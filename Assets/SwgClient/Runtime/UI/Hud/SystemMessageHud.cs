using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        [Min(1)] public int maxMessages = 4;
        readonly List<(string Text, float Expires)> messages = new List<(string, float)>();
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
            messages.Clear();
            label.enabled = false;
        }
        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            float now = Time.unscaledTime;
            // Keep one extra fading line while new messages push the older lines upward.
            if (messages.Count >= maxMessages + 1) messages.RemoveAt(0);
            for (int i = 0; i <= messages.Count - maxMessages; i++)
                messages[i] = (messages[i].Text, Mathf.Min(messages[i].Expires, now + fadeDuration));
            messages.Add((message, now + duration + fadeDuration));
            transform.SetAsLastSibling();
            if (displaying == null) displaying = StartCoroutine(Display());
        }
        IEnumerator Display()
        {
            var text = new StringBuilder();
            label.alpha = 1;
            label.enabled = true;
            while (messages.Count > 0)
            {
                float now = Time.unscaledTime;
                messages.RemoveAll(message => message.Expires <= now);
                text.Clear();
                foreach (var message in messages)
                {
                    byte alpha = (byte)Mathf.RoundToInt(255 * Mathf.Clamp01((message.Expires - now) / fadeDuration));
                    if (text.Length > 0) text.AppendLine();
                    text.Append("<alpha=#").Append(alpha.ToString("X2")).Append('>').Append(message.Text);
                }
                label.text = text.ToString();
                yield return null;
            }
            label.enabled = false;
            displaying = null;
        }
    }
}