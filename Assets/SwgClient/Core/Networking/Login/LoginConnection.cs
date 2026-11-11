using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Login.Packets;
using Swg.Client.Core.Networking.Soe;

namespace Swg.Client.Core.Networking.Login
{
    /// <summary>Authenticates the account and hands an admitted zone connection to its caller.</summary>
    public sealed class LoginConnection : IDisposable
    {
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        readonly object gate = new object ();
        GameConnection gameConnection;
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
                catch (Exception error)
                {
                    if (!stop.IsCancellationRequested)
                        Events.Enqueue(new ClientEvent { Failed = true, Status = error.Message });
                }
            });
        }

        public GameConnection TakeGameConnection()
        {
            lock (gate)
            {
                var connection = gameConnection ?? throw new InvalidOperationException("Galaxy admission is not complete.");
                gameConnection = null;
                return connection;
            }
        }

        void Progress(string status) => Events.Enqueue(new ClientEvent { Status = status });
        void Run(ClientSettings settings, string username, string password, CancellationToken cancel)
        {
            Progress("Connecting to login server...");
            using (var login = SoeTransport.Connect(settings.LoginHost, settings.LoginPort, cancel))
            {
                Progress("Authenticating account...");
                var credentials = new LoginCredentialsPacket(username, password, settings.ClientVersion);
                login.SendData(credentials);
                var collector = new LoginPacketCollector();
                DateTime deadline = DateTime.UtcNow.AddSeconds(20);
                while (!collector.Finished)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (DateTime.UtcNow > deadline)
                        throw new TimeoutException("Login timed out waiting for account and galaxy data");
                    foreach (var bytes in login.Tick())
                        collector.Accept(bytes);
                }

                var result = collector.Result;
                var galaxy = result.Galaxies.FirstOrDefault(g => g.Status == LoginClusterStatusPacket.OnlineStatus && (settings.GalaxyId == 0 || g.Id == settings.GalaxyId));
                if (galaxy == null)
                    throw new InvalidOperationException("No available galaxy. Check server status or GalaxyId in client.ini.");
                Progress("Connecting to " + galaxy.Name + "...");
                var connection = GameConnection.Connect(settings, result, galaxy, cancel);
                lock (gate)
                {
                    if (stop.IsCancellationRequested)
                    {
                        connection.Dispose();
                        return;
                    }

                    gameConnection = connection;
                    Events.Enqueue(new ClientEvent { Admission = connection.Admission, Status = "Connected to " + galaxy.Name });
                }
            }
        }

        public void Dispose()
        {
            stop.Cancel();
            lock (gate)
            {
                gameConnection?.Dispose();
                gameConnection = null;
            }
        }
    }
}
