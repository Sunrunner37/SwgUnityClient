using System;
using System.Linq;
using UnityEngine;
using Swg.Client.Core;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Login;

namespace Swg.Client.Runtime.Networking
{
    public sealed class ClientConnection : MonoBehaviour
    {
        LoginConnection connection;
        public void SelectCharacter(SwgId swgId)
        {
            if (connection == null || Admission == null)
                throw new InvalidOperationException("Log in before selecting a character.");
            var character = Admission.Characters.FirstOrDefault(value => value.SwgId == swgId && value.Galaxy == Admission.Galaxy.Id);
            if (character == null)
                throw new InvalidOperationException("The selected character does not belong to this admitted galaxy.");
            GameServices.EndGame();
            GameServices.BeginGame();
            connection.SelectCharacter((ulong)swgId, GameServices.GameScope);
        }

        public void ConfirmSceneReady()
        {
            if (connection == null || GameServices.GameScope == null)
                throw new InvalidOperationException("No selected world session.");
            connection.ConfirmSceneReady();
        }

        public Admission Admission { get; private set; }
        public string Username { get; set; }
        public string FailureReason { get; private set; }

        public void Accept(LoginConnection accepted, Admission admission)
        {
            if (accepted == null || admission == null || !admission.CanLogin || admission.Characters == null || admission.Galaxy == null)
                throw new InvalidOperationException("The server has not granted login permission.");
            Disconnect();
            connection = accepted;
            Admission = admission;
        }

        void Update()
        {
            if (connection == null)
                return;
            while (connection.Events.TryDequeue(out var message))
            {
                if (!message.Failed)
                    continue;
                Disconnect(message.Status);
                break;
            }
        }

        public string ConsumeFailure()
        {
            var message = FailureReason;
            FailureReason = null;
            return message;
        }

        public void Disconnect(string reason = null)
        {
            connection?.Dispose();
            connection = null;
            Admission = null;
            GameServices.EndGame();
            FailureReason = reason;
        }

        void OnDestroy() => connection?.Dispose();
    }
}
