using System;
using System.Collections;
using System.Collections.Generic;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Hud
{
    /// <summary>Renders server-owned buffs and their remaining durations using native TRE icons.</summary>
    public sealed class BuffHud : MonoBehaviour, IUiPrefabSource
    {
        [Serializable]
        public struct Definition
        {
            public string name;
            public string caption;
            public TreVisual icon;
        }
        public GameObject panel;
        public Transform content;
        public BuffIcon iconPrefab;
        public UiTooltip tooltip;
        public TreVisual fallbackIcon;
        public Definition[] definitions;
        public IEnumerable<GameObject> UiPrefabs { get { yield return iconPrefab.gameObject; } }
        readonly Dictionary<uint, BuffIcon> icons = new Dictionary<uint, BuffIcon>();
        readonly List<uint> removed = new List<uint>();
        bool closed;
        public void Close() { closed = true; tooltip.Hide(); panel.SetActive(false); }
        CurrentPlayer player;
        TreRuntimeAssets assets;
        void Awake() { player = GameServices.Resolve<CurrentPlayer>(); assets = GameServices.Resolve<TreRuntimeAssets>(); }
        void OnEnable() { player.BuffsChanged += Refresh; Refresh(); }
        void OnDisable() { player.BuffsChanged -= Refresh; tooltip.Hide(); }
        IEnumerator Start()
        {
            yield return new WaitUntil(() => assets.Ready.IsCompleted);
            if (assets.Ready.IsFaulted) { Debug.LogException(assets.Ready.Exception.GetBaseException(), this); yield break; }
            Refresh();
        }
        void Refresh()
        {
            if (!assets.Ready.IsCompletedSuccessfully) return;
            closed = false;
            removed.Clear();
            foreach (var entry in icons) if (!player.Buffs.ContainsKey(entry.Key)) removed.Add(entry.Key);
            foreach (uint crc in removed) { Destroy(icons[crc].gameObject); icons.Remove(crc); }
            foreach (var buff in player.Buffs)
            {
                if (!icons.TryGetValue(buff.Key, out var icon))
                {
                    icon = Instantiate(iconPrefab, content);
                    icons.Add(buff.Key, icon);
                }
                var definition = Array.Find(definitions, value => ObjectTemplateNames.TemplateCrc(value.name) == buff.Key);
                icon.Bind(definition.caption ?? ("Effect " + buff.Key.ToString("X8")), buff.Value,
                    assets.LoadVisual(definition.icon != null ? definition.icon : fallbackIcon), tooltip);
            }
            UpdateVisibility();
        }
        void LateUpdate() => UpdateVisibility();
        void UpdateVisibility()
        {
            bool any = false;
            double now = Time.realtimeSinceStartupAsDouble;
            foreach (var entry in icons)
            {
                bool active = entry.Value.ExpiresAt > now;
                entry.Value.gameObject.SetActive(active);
                any |= active;
            }
            panel.SetActive(any && !closed);
        }
    }
}
