using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using TMPro;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Inventory
{
    public sealed class DestroyItemWindow : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text prompt;
        ClientObjectInteractions interactions;
        WorldState state;
        SceneObjectNameResolver names;
        SwgId candidate;

        void Awake()
        {
            interactions = GameServices.Resolve<ClientObjectInteractions>();
            state = GameServices.Resolve<WorldState>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }

        void OnEnable() => interactions.DestroyRequested += Open;
        void OnDisable()
        {
            interactions.DestroyRequested -= Open;
            Cancel();
        }

        void Open(SwgId swgId)
        {
            if (!state.SceneObjects.TryGet(swgId, out var sceneObject)) return;
            candidate = swgId;
            prompt.text = "Really destroy " + names.Resolve(sceneObject) + "?";
            panel.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Cancel()
        {
            candidate = default;
            panel.SetActive(false);
        }

        public void Confirm()
        {
            var swgId = candidate;
            Cancel();
            if (!swgId.IsNone) interactions.Destroy(swgId);
        }
    }
}
