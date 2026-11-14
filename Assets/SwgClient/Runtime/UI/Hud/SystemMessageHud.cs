using System.Collections;
using Swg.Client.Runtime.Networking.Chat;
using TMPro;
using UnityEngine;
namespace Swg.Client.Runtime.UI.Hud
{
    /// <summary>Displays server screen-and-chat messages at the center of the HUD; chat-only messages stay in chat.</summary>
    public sealed class SystemMessageHud : MonoBehaviour
    {
        public TMP_Text label;
        [Min(.1f)] public float duration = 5;
        [Min(.1f)] public float fadeDuration = .5f;
        ClientChat source;
        Coroutine displaying;
        void Awake()
        {
            source = GameServices.Resolve<ClientChat>();
            label.enabled = false;
        }
        void OnEnable() => source.ScreenMessageReceived += Show;
        void OnDisable()
        {
            if (source != null) source.ScreenMessageReceived -= Show;
            if (displaying != null) StopCoroutine(displaying);
            displaying = null;
            label.enabled = false;
        }
        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (displaying != null) StopCoroutine(displaying);
            transform.SetAsLastSibling();
            displaying = StartCoroutine(Display(message));
        }
        IEnumerator Display(string message)
        {
            label.text = message;
            label.alpha = 1;
            label.enabled = true;
            yield return new WaitForSecondsRealtime(duration);
            for (float age = 0; age < fadeDuration; age += Time.unscaledDeltaTime)
            {
                label.alpha = 1 - age / fadeDuration;
                yield return null;
            }
            label.enabled = false;
            displaying = null;
        }
    }
}
