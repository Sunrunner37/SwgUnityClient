using System;
using System.Collections.Generic;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Trade;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Interactions;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Group
{
    /// <summary>Prefab-backed group roster, shared HAM rendering, nameplates, and member context actions.</summary>
    public sealed class GroupHud : MonoBehaviour, IUiPrefabSource
    {
        public enum MemberAction { Trade, Duel, Examine, ImageDesign, Kick, Follow, Teach }
        [Serializable]
        public struct ActionBinding
        {
            public MemberAction action;
            public Button button;
        }

        public Transform members;
        public GroupMemberHud memberPrefab;
        public GameObject menuOverlay;
        public RectTransform menu;
        public ActionBinding[] menuActions;
        public ExamineWindow examine;
        public Camera worldCamera;
        public IEnumerable<GameObject> UiPrefabs { get { yield return memberPrefab.gameObject; } }
        readonly Dictionary<SwgId, GroupMemberHud> rows = new Dictionary<SwgId, GroupMemberHud>();
        readonly List<SwgId> removed = new List<SwgId>();
        CurrentPlayer player;
        WorldState state;
        ClientTargeting targeting;
        ClientCommands commands;
        Action<TradeRequestPacket> trade;
        SwgId menuTarget;
        void Awake()
        {
            player = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            targeting = GameServices.Resolve<ClientTargeting>();
            commands = GameServices.Resolve<ClientCommands>();
            trade = GameServices.Resolve<Action<TradeRequestPacket>>();
            foreach (var binding in menuActions)
            {
                var action = binding.action;
                binding.button.onClick.AddListener(() => Activate(action));
            }
            CloseMenu();
        }
        void LateUpdate()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            var group = player.Group;
            removed.Clear();
            foreach (var row in rows)
                if (group == null || !group.Contains(row.Key) || row.Key == player.SwgId) removed.Add(row.Key);
            foreach (var id in removed) { Destroy(rows[id].gameObject); rows.Remove(id); }
            if (group == null) { CloseMenu(); return; }
            ManagedSceneObject.TryGetObjectFrom(player.SwgId, out var playerBody);
            int order = 0;
            foreach (var member in group.Members)
            {
                if (member.SwgId == player.SwgId) continue;
                if (!rows.TryGetValue(member.SwgId, out var row))
                {
                    row = Instantiate(memberPrefab, members);
                    row.gameObject.SetActive(true);
                    row.Bind(this, member.SwgId);
                    rows.Add(member.SwgId, row);
                }
                row.transform.SetSiblingIndex(order++);
                state.SceneObjects.TryGet<CreatureSceneObject>(member.SwgId, out var creature);
                ManagedSceneObject.TryGetObjectFrom(member.SwgId, out var body);
                row.Show(member, group.LeaderSwgId == member.SwgId, creature, body, playerBody == null ? null : playerBody.transform, worldCamera);
            }
            if (menuOverlay.activeSelf && (!group.Contains(menuTarget) || Keyboard.current?.escapeKey.wasPressedThisFrame == true)) CloseMenu();
        }
        public void Select(SwgId swgId) => targeting.Select(swgId);
        public void OpenMenu(SwgId swgId, Vector2 screenPosition)
        {
            if (player.Group == null || !player.Group.Contains(swgId) || swgId == player.SwgId) return;
            menuTarget = swgId;
            menuOverlay.SetActive(true);
            menuOverlay.transform.SetAsLastSibling();
            var area = (RectTransform)menu.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPosition, null, out var point);
            point.x = Mathf.Clamp(point.x, area.rect.xMin, area.rect.xMax - menu.rect.width);
            point.y = Mathf.Clamp(point.y, area.rect.yMin + menu.rect.height, area.rect.yMax);
            menu.anchoredPosition = point;
            bool nearby = ManagedSceneObject.TryGetObjectFrom(swgId, out _);
            foreach (var binding in menuActions)
                binding.button.interactable = binding.action switch
                {
                    MemberAction.Kick => player.Group.LeaderSwgId == player.SwgId,
                    MemberAction.ImageDesign => nearby && player.Creature.Skills.Contains("social_entertainer_novice"),
                    _ => nearby
                };
        }
        public void CloseMenu() => menuOverlay.SetActive(false);
        public void Activate(MemberAction action)
        {
            var target = menuTarget;
            CloseMenu();
            if (player.Group == null || !player.Group.Contains(target)) return;
            switch (action)
            {
                case MemberAction.Trade: trade(new TradeRequestPacket(player.SwgId, target)); break;
                case MemberAction.Duel: commands.Enqueue("duel", "", target); break;
                case MemberAction.Examine: examine.Open(target); break;
                case MemberAction.ImageDesign: commands.Enqueue("imagedesign", "", target); break;
                case MemberAction.Kick: if (player.Group.LeaderSwgId == player.SwgId) commands.Enqueue("dismissGroupMember", "", target); break;
                case MemberAction.Follow:
                    if (ManagedSceneObject.TryGetObjectFrom(player.SwgId, out var body) && body.TryGetComponent<PlayerMovement>(out var movement)) movement.Follow(target);
                    break;
                case MemberAction.Teach: commands.Enqueue("teach", "", target); break;
            }
        }
    }
}
