using Swg.Client.Core.Characters;
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
        public CreatureStatusDisplay statusDisplay;
        CurrentPlayer currentPlayer;
        SceneObjectNameResolver names;
        CreatureSceneObject displayedPlayer;
        string displayedName;

        void Awake()
        {
            currentPlayer = GameServices.Resolve<CurrentPlayer>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }

        void LateUpdate()
        {
            var player = currentPlayer.Creature;
            bool available = player != null;
            panel.SetActive(available);
            if (!available)
                return;
            if (displayedPlayer != player || displayedName != player.Name)
            {
                displayedPlayer = player;
                displayedName = player.Name;
                playerName.text = names.Resolve(player);
            }
            statusDisplay.Show(player);
        }
    }
}
