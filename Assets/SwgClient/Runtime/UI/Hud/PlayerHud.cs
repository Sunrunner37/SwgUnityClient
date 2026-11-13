using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Localization;
using TMPro;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Hud
{
    /// <summary>Displays the current player's replicated name and HAM, independently of target selection.</summary>
    public sealed class PlayerHud : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text playerName;
        public HamBars ham;
        [Tooltip("Native posture icons in CreaturePosture enum order.")]
        public GameObject[] postureIcons;
        WorldState state;
        SceneObjectNameResolver names;
        CreatureSceneObject displayedPlayer;
        string displayedName;

        void Awake()
        {
            state = GameServices.Resolve<WorldState>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }

        void LateUpdate()
        {
            bool available = state.SceneObjects.TryGet<CreatureSceneObject>(state.PlayerSwgId, out var player);
            panel.SetActive(available);
            if (!available)
                return;
            if (displayedPlayer != player || displayedName != player.Name)
            {
                displayedPlayer = player;
                displayedName = player.Name;
                playerName.text = names.Resolve(player);
            }
            ham.Show(player.Ham);
            for (int i = 0; i < postureIcons.Length; i++)
                postureIcons[i].SetActive(i == (int)player.Posture);
        }
    }
}
