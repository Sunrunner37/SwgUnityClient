using Autofac;
using Swg.Client.Core.Networking.Game.Packets.Session;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Soe;
using Swg.Client.Core.Networking.Login.Packets;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Core.Networking.Login
{
    public sealed class LoginConnection : IDisposable
    {
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        readonly ConcurrentQueue<WorldSelection> selections = new ConcurrentQueue<WorldSelection>();
        int sceneReadyRequested;
        readonly struct WorldSelection
        {
            public readonly ulong CharacterSwgId;
            public readonly ILifetimeScope Services;
            public WorldSelection(ulong characterSwgId, ILifetimeScope services)
            {
                CharacterSwgId = characterSwgId;
                Services = services;
            }
        }

        public void SelectCharacter(ulong characterSwgId, ILifetimeScope services)
        {
            if (characterSwgId == 0 || services == null || stop.IsCancellationRequested)
                throw new InvalidOperationException("No active connection for character selection.");
            Interlocked.Exchange(ref sceneReadyRequested, 0);
            selections.Enqueue(new WorldSelection(characterSwgId, services));
        }

        public void ConfirmSceneReady() => Interlocked.Exchange(ref sceneReadyRequested, 1);
        public readonly ConcurrentQueue<ClientEvent> Events = new ConcurrentQueue<ClientEvent>();
        public LoginConnection(ClientSettings settings, string username, string password)
        {
            Task.Run(() =>
            {
                try
                {
                    Run(settings, username, password, stop.Token);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    if (!stop.IsCancellationRequested)
                        Events.Enqueue(new ClientEvent { Failed = true, Status = e.Message });
                }
            });
        }

        void Progress(string s) => Events.Enqueue(new ClientEvent { Status = s });
        void Run(ClientSettings config, string user, string password, CancellationToken cancel)
        {
            var credentials = new LoginCredentialsPacket(user, password, config.ClientVersion);
            user = null;
            password = null;
            Progress("Connecting to login server...");
            using (var login = SoeTransport.Connect(config.LoginHost, config.LoginPort, cancel))
            {
                Progress("Authenticating account...");
                login.SendData(credentials);
                var loginPacketCollector = new LoginPacketCollector();
                DateTime deadline = DateTime.UtcNow.AddSeconds(20);
                while (!loginPacketCollector.Finished)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (DateTime.UtcNow > deadline)
                        throw new TimeoutException("Login timed out waiting for account and galaxy data");
                    foreach (var b in login.Tick())
                        loginPacketCollector.Accept(b);
                }

                var result = loginPacketCollector.Result;
                try
                {
                    Galaxy galaxy = result.Galaxies.FirstOrDefault(g => g.Status == LoginClusterStatusPacket.OnlineStatus && (config.GalaxyId == 0 || g.Id == config.GalaxyId));
                    if (galaxy == null)
                        throw new InvalidOperationException("No available galaxy. Check server status or GalaxyId in client.ini.");
                    Progress("Connecting to " + galaxy.Name + "...");
                    using (var zone = SoeTransport.Connect(string.IsNullOrWhiteSpace(config.ZoneHost) ? galaxy.Address : config.ZoneHost, galaxy.Port, cancel))
                    {
                        try
                        {
                            zone.SendData(new ClientIdentificationPacket(result.Token, result.AccountId, config.ClientVersion));
                        }
                        finally
                        {
                            Array.Clear(result.Token, 0, result.Token.Length);
                        }

                        Progress("Waiting for galaxy permissions...");
                        deadline = DateTime.UtcNow.AddSeconds(20);
                        bool admitted = false;
                        bool creationAllowed = false;
                        while (!admitted)
                        {
                            cancel.ThrowIfCancellationRequested();
                            if (DateTime.UtcNow > deadline)
                                throw new TimeoutException("Galaxy timed out before granting client permissions");
                            login.Tick();
                            foreach (var b in zone.Tick())
                            {
                                uint opcode = ByteReader.At(b, sizeof(ushort));
                                if (opcode == ErrorPacket.Opcode)
                                    throw ErrorPacket.ReadFrom(new ByteReader(b)).ToException();
                                if (opcode == ClientPermissionsPacket.Opcode)
                                {
                                    var permissions = ClientPermissionsPacket.ReadFrom(new ByteReader(b));
                                    if (!permissions.CanLogin)
                                        throw new InvalidOperationException("This galaxy does not permit login for this account");
                                    creationAllowed = permissions.CanCreate;
                                    admitted = true;
                                }
                            }
                        }

                        Events.Enqueue(new ClientEvent { Admission = new Admission { AccountId = result.AccountId, Galaxy = galaxy, Characters = result.Characters, CanLogin = true, CanCreate = creationAllowed }, Status = "Connected to " + galaxy.Name });
                        GamePacketRouter gamePackets = null;
                        while (!cancel.IsCancellationRequested)
                        {
                            while (selections.TryDequeue(out var selection))
                            {
                                gamePackets = new GamePacketRouter(selection.Services);
                                Progress("Loading selected character...");
                                zone.SendData(new SelectCharacterPacket(selection.CharacterSwgId));
                            }

                            if (gamePackets != null && Interlocked.Exchange(ref sceneReadyRequested, 0) != 0)
                                zone.SendData(new CmdSceneReadyPacket());
                            foreach (var b in zone.Tick())
                            {
                                if (ByteReader.At(b, sizeof(ushort)) == ErrorPacket.Opcode)
                                    throw ErrorPacket.ReadFrom(new ByteReader(b)).ToException();
                                gamePackets?.Accept(b);
                            }
                        }
                    }
                }
                finally
                {
                    Array.Clear(result.Token, 0, result.Token.Length);
                }
            }
        }

        public void Dispose()
        {
            stop.Cancel();
        }
    }
}
