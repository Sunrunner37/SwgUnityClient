using System.Collections.Generic;
using Swg.Client.Core;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Interactions;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Inventory
{
    /// <summary>Opens independent inventory views only after Core3 grants access to a container.</summary>
    public sealed class ContainerWindows : MonoBehaviour, IUiPrefabSource
    {
        public InventoryWindow windowPrefab;
        public RadialContextMenu contextMenu;
        readonly Dictionary<SwgId, InventoryWindow> windows = new Dictionary<SwgId, InventoryWindow>();
        ClientInventory inventory;
        public IEnumerable<GameObject> UiPrefabs { get { yield return windowPrefab.gameObject; } }
        void Awake() => inventory = GameServices.Resolve<ClientInventory>();
        void OnEnable() => inventory.ContainerOpened += Open;
        void OnDisable() => inventory.ContainerOpened -= Open;
        void Open(SwgId swgId)
        {
            if (!windows.TryGetValue(swgId, out var window) || window == null)
            {
                window = Instantiate(windowPrefab, transform);
                window.contextMenu = contextMenu;
                window.previews.stagePosition += Vector3.right * (1000 * (windows.Count + 1));
                windows[swgId] = window;
            }
            window.OpenContainer(swgId);
            window.previews.stage.position = window.previews.stagePosition;
        }
    }
}

