using System;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Core.Networking.Login.Packets;
using Swg.Client.Core.Networking.Soe;

namespace Swg.Client.Core.Networking.Game
{
    /// <summary>Owns the admitted zone connection and its single send/receive pump.</summary>
    public sealed class GameConnection : IDisposable
    {
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        readonly PacketSendQueue outgoing = new PacketSendQueue();
        readonly SoeTransport transport;
        SynchronizationContext context;
        /// <summary>Raised on the owning context when the connection ends unexpectedly.</summary>
        public event Action<string> Disconnected;
        Action<byte[]> receive;
        public Admission Admission { get; }

        GameConnection(SoeTransport transport, Admission admission)
        {
            this.transport = transport;
            Admission = admission;
        }

        public void Send<T>(in T packet)
            where T : struct, IPacket => outgoing.Send(in packet);
        public void SetReceiver(Action<byte[]> receive) => Volatile.Write(ref this.receive, receive);
        /// <summary>Starts the network pump after subscribers have attached, using their main-thread context.</summary>
        public void Start(SynchronizationContext context)
        {
            if (this.context != null)
                throw new InvalidOperationException("The game connection is already started.");
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            Task.Run(Run);
        }

        internal static GameConnection Connect(ClientSettings settings, LoginResult login, Galaxy galaxy, CancellationToken cancel)
        {
            var transport = SoeTransport.Connect(string.IsNullOrWhiteSpace(settings.ZoneHost) ? galaxy.Address : settings.ZoneHost, galaxy.Port, cancel);
            try
            {
                try
                {
                    transport.SendData(new ClientIdentificationPacket(login.Token, login.AccountId, settings.ClientVersion));
                }
                finally
                {
                    Array.Clear(login.Token, 0, login.Token.Length);
                }

                DateTime deadline = DateTime.UtcNow.AddSeconds(20);

                while (DateTime.UtcNow < deadline)
                {
                    cancel.ThrowIfCancellationRequested();
                    foreach (var bytes in transport.Tick())
                    {
                        ThrowIfError(bytes);
                        if (ByteReader.At(bytes, sizeof(ushort)) != ClientPermissionsPacket.Opcode)
                            continue;
                        var permissions = ClientPermissionsPacket.ReadFrom(new ByteReader(bytes));
                        if (!permissions.CanLogin)
                            throw new InvalidOperationException("This galaxy does not permit login for this account");
                        return new GameConnection(transport, new Admission { AccountId = login.AccountId, Username = login.Username, Galaxy = galaxy, Characters = login.Characters, CanLogin = true, CanCreate = permissions.CanCreate });
                    }
                }

                throw new TimeoutException("Galaxy timed out when connecting");
            }
            catch
            {
                transport.Dispose();
                throw;
            }
        }

        void Run()
        {
            try
            {
                using (transport)
                using (outgoing)
                    while (!stop.IsCancellationRequested)
                    {
                        outgoing.Flush(transport.SendData);
                        foreach (var bytes in transport.Tick())
                        {
                            ThrowIfError(bytes);
                            Volatile.Read(ref receive)?.Invoke(bytes);
                        }
                    }
            }
            catch (Exception error)
            {
                if (!stop.IsCancellationRequested)
                {
                    string reason = error.Message;
                    context.Post(_ =>
                    {
                        // An intentional close may happen while this notification awaits delivery.
                        if (!stop.IsCancellationRequested)
                            Disconnected?.Invoke(reason);
                    }, null);
                }
            }
        }

        static void ThrowIfError(byte[] bytes)
        {
            if (ByteReader.At(bytes, sizeof(ushort)) == ErrorPacket.Opcode)
                throw ErrorPacket.ReadFrom(new ByteReader(bytes)).ToException();
        }

        public void Dispose()
        {
            stop.Cancel();
            if (context == null)
            {
                // Admission can be cancelled before ownership transfers to the client.
                outgoing.Dispose();
                transport.Dispose();
            }
        }
    }
}
