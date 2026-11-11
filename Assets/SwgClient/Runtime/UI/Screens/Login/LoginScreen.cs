using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;
using System;
using System.Collections;
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
        public CanvasGroup content;
        void Awake()
        {
            content.alpha = 0;
            content.interactable = false;
            content.blocksRaycasts = false;
        }

        TreRuntimeAssets assets;
        IEnumerator Start()
        {
            assets = GameServices.Resolve<TreRuntimeAssets>();
            try
            {
                server.text = "Server: " + GameServices.Resolve<ClientSettings>().LoginEndpoint;
                username.text = "";
                status.text = "";
                back.onClick.AddListener(Back);
                next.onClick.AddListener(Submit);
                SetInteractable(true);
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

            yield return new WaitUntil(() => assets.Ready.IsCompleted);
            if (assets.Ready.IsFaulted)
            {
                Debug.LogException(assets.Ready.Exception.GetBaseException(), this);
                yield break;
            }

            yield return null;
            try
            {
                TreTextureBinding.ThrowIfFailed(content.transform);
                content.alpha = 1;
                content.interactable = true;
                content.blocksRaycasts = true;
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

            username.ActivateInputField();
            IsReady = true;
            Debug.Log("LoginScreen: Ready", this);
        }

        public void Submit()
        {
            if (!IsReady || connecting != null)
                return;
            if (string.IsNullOrWhiteSpace(username.text) || string.IsNullOrEmpty(password.text))
            {
                status.text = "Enter your username and password.";
                return;
            }

            connecting = new LoginConnection(GameServices.Resolve<ClientSettings>(), username.text, password.text);
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

            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void Update()
        {
            if (!IsReady)
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
                        GameServices.AcceptConnection(connecting.TakeGameConnection());
                        connecting.Dispose();
                        connecting = null;
                        ScreenChanger.Open(ScreenId.CharacterList);
                        return;
                    }

                    status.text = message.Status;
                }

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.escapeKey.wasPressedThisFrame)
                Back();
            else if (keyboard.tabKey.wasPressedThisFrame && connecting == null)
            {
                var target = username.isFocused ? password : username;
                if (target.IsActive() && target.IsInteractable())
                {
                    target.Select();
                    target.ActivateInputField();
                }
            }
            else if (keyboard.enterKey.wasPressedThisFrame)
                Submit();
        }

        void OnDestroy() => connecting?.Dispose();
    }
}
