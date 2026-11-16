using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Hud
{
    /// <summary>Shared HAM and posture artwork; each HUD supplies its creature and controls its own layout.</summary>
    public sealed class CreatureStatusDisplay : MonoBehaviour
    {
        public HamBars ham;
        public GameObject posture;
        [Tooltip("Native posture icons in CreaturePosture enum order.")]
        public GameObject[] postureIcons;

        public void Show(CreatureSceneObject creature)
        {
            bool available = creature != null;
            ham.gameObject.SetActive(available);
            posture.SetActive(available);
            if (!available) return;
            ham.Show(creature.Ham);
            for (int i = 0; i < postureIcons.Length; i++)
                postureIcons[i].SetActive(i == (int)creature.Posture);
        }
    }
}
