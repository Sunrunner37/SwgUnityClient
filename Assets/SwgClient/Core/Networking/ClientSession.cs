using System;
using Swg.Client.Core.Networking.Login;

namespace Swg.Client.Core.Networking
{
    public sealed class ClientSession : IDisposable
    {
        LoginConnection connection;
        public Admission Admission
        {
            get; private set;
        }
        public string Username
        {
            get; set;
        }
        public string FailureReason
        {
            get; private set;
        }

        public void Accept(LoginConnection accepted, Admission admission)
        {
            if (accepted == null || admission == null || !admission.CanLogin || admission.Characters == null || admission.Galaxy == null)
                throw new InvalidOperationException("The server has not granted login permission.");
            Disconnect();
            connection = accepted;
            Admission = admission;
        }

        public void Poll()
        {
            if (connection == null)
                return;
            while (connection.Events.TryDequeue(out var message))
            {
                if (!message.Failed)
                    continue;
                Disconnect();
                FailureReason = message.Status;
                break;
            }
        }

        public string ConsumeFailure()
        {
            var message = FailureReason;
            FailureReason = null;
            return message;
        }

        public void Disconnect()
        {
            connection?.Dispose();
            connection = null;
            Admission = null;
            FailureReason = null;
        }

        public void Dispose() => Disconnect();
    }
}
