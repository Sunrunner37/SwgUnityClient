using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.UI;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.UI.Theming;
using UnityEngine;
using TMPro;

namespace Swg.Client.Runtime.UI.Combat
{
    /// <summary>Shows the player's combat queue; prefab layout owns its frame, buttons, scrolling, and row sizes.</summary>
    public sealed class CombatQueueWindow : MonoBehaviour, IUiPrefabSource
    {
        public GameObject panel;
        public Transform content;
        public CombatQueueRow rowPrefab;
        public UiTheme theme;
        public TMP_Text targetName;
        public IEnumerable<GameObject> UiPrefabs { get { yield return rowPrefab.gameObject; } }
        readonly Dictionary<uint, CombatQueueRow> rows = new Dictionary<uint, CombatQueueRow>();
        WorldState state;
        ClientCombatQueue queue;
        CommandCatalog catalog;
        CommandVisuals visuals;
        SceneObjectNameResolver names;
        SwgId playerSwgId;
        bool showingRecovery;
        bool ready;
        void Awake()
        {
            state = GameServices.Resolve<WorldState>();
            queue = GameServices.Resolve<ClientCombatQueue>();
            catalog = GameServices.Resolve<CommandCatalog>();
            visuals = GameServices.Resolve<CommandVisuals>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }
        void OnEnable()
        {
            queue.Changed += Refresh;
            theme.Changed += Refresh;
            state.SceneStarted += BindPlayer;
            BindPlayer();
        }
        IEnumerator Start()
        {
            if (!visuals.Loading.IsCompleted)
                yield return new WaitUntil(() => visuals.Loading.IsCompleted);
            if (visuals.Loading.IsFaulted)
            {
                Debug.LogException(visuals.Loading.Exception.GetBaseException(), this);
                yield break;
            }
            ready = true;
            OnPlayerChanged(SceneObjectChange.State);
            Refresh();
        }
        void OnDisable()
        {
            queue.Changed -= Refresh;
            theme.Changed -= Refresh;
            state.SceneStarted -= BindPlayer;
            state.SceneObjects.Unsubscribe(playerSwgId, OnPlayerChanged);
        }
        void BindPlayer()
        {
            state.SceneObjects.Unsubscribe(playerSwgId, OnPlayerChanged);
            playerSwgId = state.PlayerSwgId;
            state.SceneObjects.Subscribe(playerSwgId, OnPlayerChanged);
            OnPlayerChanged(SceneObjectChange.State);
        }
        void OnPlayerChanged(SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.State | SceneObjectChange.Target | SceneObjectChange.Replaced)) == 0)
                return;
            bool combat = ready && state.SceneObjects.TryGet<CreatureSceneObject>(playerSwgId, out var player) && player.IsInCombat;
            panel.SetActive(combat);
            if (combat)
                Refresh();
        }
        public void Clear() => queue.Clear();
        public void Peace() => queue.Peace();
        void Refresh()
        {
            if (!ready)
                return;
            double now = Time.realtimeSinceStartupAsDouble;
            var entries = queue.GetEntries(now).ToArray();
            var keep = new HashSet<uint>(entries.Select(entry => entry.ActionCounter));
            foreach (var counter in rows.Keys.Where(counter => !keep.Contains(counter)).ToArray())
            {
                Destroy(rows[counter].gameObject);
                rows.Remove(counter);
            }
            int index = 0;
            foreach (var entry in entries)
            {
                if (!rows.TryGetValue(entry.ActionCounter, out var row))
                {
                    row = Instantiate(rowPrefab, content);
                    rows.Add(entry.ActionCounter, row);
                }
                row.transform.SetSiblingIndex(index++);
                var command = catalog.FindByCrc(entry.Command.CommandCrc, entry.Command.Arguments);
                row.actionName.text = command.Label;
                row.actionName.color = entry.Executing ? theme.highlight : theme.text;
                row.icon.sprite = visuals.GetIcon(command);
                row.icon.enabled = row.icon.sprite != null;
                row.icon.color = row.actionName.color;
                row.opacity.alpha = entry.Alpha;
            }
            var targetSwgId = entries.FirstOrDefault().Command.TargetSwgId;
            if (targetSwgId.IsNone && state.SceneObjects.TryGet<CreatureSceneObject>(playerSwgId, out var player))
                targetSwgId = player.TargetSwgId;
            targetName.text = state.SceneObjects.TryGet(targetSwgId, out var target) ? names.Resolve(target) : "";
            targetName.color = theme.text;
            showingRecovery = queue.TryGetRecovery(now, out _);
        }
        void LateUpdate()
        {
            if (!ready || !panel.activeSelf)
                return;
            if (queue.TryGetRecovery(Time.realtimeSinceStartupAsDouble, out var recovery))
            {
                if (rows.TryGetValue(recovery.ActionCounter, out var row))
                    row.opacity.alpha = recovery.Alpha;
            }
            else if (showingRecovery)
                Refresh();
        }
    }
}
