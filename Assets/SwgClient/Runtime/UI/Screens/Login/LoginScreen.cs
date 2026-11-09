using System.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Swg.Client.Core.Networking.Login;

namespace Swg.Client.Runtime.UI.Screens.Login
{
    public sealed class LoginScreen : ClientScreen
    {
        public InputField username;
        public InputField password;
        public Text status;
        public Text server;
        public Button back;
        public Button next;
        LoginConnection connecting;
        protected override Task PrepareAsync() => Task.CompletedTask;
        protected override void OnReady()
        {
            Client.Music.Request(Client.Assets, musicPath, loopMusic);
            server.text = "Server: " + Client.Settings.LoginEndpoint;
            username.text = Client.Session.Username ?? "";
            status.text = Client.Session.ConsumeFailure() ?? "";
            back.onClick.AddListener(Back);
            next.onClick.AddListener(Submit);
            SetInteractable(true);
            username.ActivateInputField();
        }

        public void Submit()
        {
            if (!IsReady || connecting != null || ClientNavigation.IsChanging)
                return;
            if (string.IsNullOrWhiteSpace(username.text) || string.IsNullOrEmpty(password.text))
            {
                status.text = "Enter your username and password.";
                return;
            }

            Client.Session.Username = username.text;
            connecting = new LoginConnection(Client.Settings, username.text, password.text);
            password.text = "";
            SetInteractable(false);
            status.text = "Connecting to login server...";
        }

        void SetInteractable(bool enabled)
        {
            username.interactable = password.interactable = next.interactable = enabled;
        }

        public void Back()
        {
            if (connecting != null)
            {
                connecting.Dispose();
                connecting = null;
                status.text = "Connection cancelled.";
                SetInteractable(true);
                return;
            }

            ClientNavigation.Quit();
        }

        void Update()
        {
            if (!IsReady || ClientNavigation.IsChanging)
                return;
            if (connecting != null)
                while (connecting.Events.TryDequeue(out var message))
                {
                    if (message.Failed)
                    {
                        connecting.Dispose();
                        connecting = null;
                        SetInteractable(true);
                        status.text = message.Status;
                        break;
                    }

                    if (message.Admission != null)
                    {
                        Client.Session.Accept(connecting, message.Admission);
                        connecting = null;
                        ClientNavigation.Open(ScreenId.CharacterList);
                        return;
                    }

                    status.text = message.Status;
                }

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.escapeKey.wasPressedThisFrame)
                Back();
            else if (keyboard.enterKey.wasPressedThisFrame)
                Submit();
        }

        void OnDestroy() => connecting?.Dispose();
    }
}
