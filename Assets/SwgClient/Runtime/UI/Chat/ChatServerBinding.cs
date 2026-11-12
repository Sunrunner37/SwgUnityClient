using UnityEngine;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Networking.Chat;

namespace Swg.Client.Runtime.UI.Chat
{
    public sealed class ChatServerBinding : MonoBehaviour
    {
        public ChatWindow window;
        ICommandDispatcher dispatcher;
        ClientChat chat;
        void OnEnable()
        {
            if (GameServices.Connection == null)
                return;
            chat = GameServices.Resolve<ClientChat>();
            dispatcher = GameServices.Resolve<ICommandDispatcher>();
            window.ClearHistory();
            foreach (var message in chat.History)
                window.AppendMessage(message.Channel, message.Text);
            chat.MessageReceived += window.AppendMessage;
            window.onSubmit.AddListener(Submit);
        }

        void Submit(string channel, string message)
        {
            dispatcher.Submit(channel, message, out string error);
            if (error != null)
                window.AppendMessage(channel, error);
        }

        void OnDisable()
        {
            if (chat == null)
                return;
            chat.MessageReceived -= window.AppendMessage;
            window.onSubmit.RemoveListener(Submit);
            chat = null;
            dispatcher = null;
        }
    }
}
