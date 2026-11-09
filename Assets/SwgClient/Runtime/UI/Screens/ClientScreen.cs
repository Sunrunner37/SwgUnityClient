using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Runtime.UI.Screens
{
    public abstract class ClientScreen : MonoBehaviour
    {
        [FormerlySerializedAs("loadingPanel")]
        public GameObject errorPanel;
        [FormerlySerializedAs("loadingText")]
        public Text errorText;
        public CanvasGroup content;
        [Range(0, 1)]
        public float musicVolume = .65f;
        [Tooltip("Virtual TRE path to an MP3 or sound template. Shared tracks continue between screens.")]
        public string musicPath;
        public bool loopMusic = true;
        protected ClientRuntime Client
        {
            get; private set;
        }
        public bool IsReady
        {
            get; private set;
        }
        public string Phase { get; private set; } = "Initializing";

        protected IEnumerator Start()
        {
            errorPanel.SetActive(false);
            content.alpha = 0;
            content.interactable = false;
            content.blocksRaycasts = false;
            try
            {
                Client = ClientRuntime.Ensure();
            }
            catch (Exception e)
            {
                Fail(e);
                yield break;
            }

            if (RedirectOnStart())
                yield break;
            Phase = "Mounting original assets";
            var ready = Client.Assets.Ready;
            while (!ready.IsCompleted)
                yield return null;
            if (ready.IsFaulted)
            {
                Fail(ready.Exception.GetBaseException());
                yield break;
            }

            Task preparation;
            try
            {
                foreach (var binding in GetComponentsInChildren<TreTextureBinding>(true))
                    binding.Apply(Client.Assets.LoadVisual);
                content.alpha = 1;
                OnAssetsBound();
                Phase = "Preparing screen";
                preparation = PrepareAsync();
            }
            catch (Exception e)
            {
                Fail(e);
                yield break;
            }

            while (!preparation.IsCompleted)
                yield return null;
            if (preparation.IsFaulted)
            {
                Fail(preparation.Exception.GetBaseException());
                yield break;
            }

            try
            {
                Client.Music.Volume = musicVolume;
                content.interactable = true;
                content.blocksRaycasts = true;
                OnReady();
                IsReady = true;
                Phase = "Ready";
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        protected virtual bool RedirectOnStart() => false;
        protected virtual void OnAssetsBound()
        {
        }

        protected abstract Task PrepareAsync();
        protected abstract void OnReady();
        void Fail(Exception error)
        {
            Phase = error.Message;
            content.alpha = 1;
            errorPanel.SetActive(true);
            errorPanel.transform.SetAsLastSibling();
            errorText.text = "Unable to load screen\n" + error.Message;
            Debug.LogException(error);
        }
    }
}
