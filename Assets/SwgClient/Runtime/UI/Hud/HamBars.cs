using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Hud
{
    /// <summary>Renders primary HAM with proportional maxima, colored depletion, and wounds from the right.</summary>
    public sealed class HamBars : MonoBehaviour
    {
        public Image[] currentBars;
        public Image[] capacityBars;
        public Image[] woundBars;

        public void Show(HamPool ham)
        {
            UpdateBar(0, ham.Health, ham.LargestMaximum);
            UpdateBar(1, ham.Action, ham.LargestMaximum);
            UpdateBar(2, ham.Mind, ham.LargestMaximum);
        }

        void UpdateBar(int index, HamElement value, int largestMaximum)
        {
            var row = (RectTransform)currentBars[index].transform.parent;
            row.anchorMax = new Vector2(value.RelativeWidth(largestMaximum), 1);
            capacityBars[index].fillAmount = value.CapacityFraction;
            woundBars[index].fillAmount = value.WoundFraction;
            currentBars[index].fillAmount = value.CurrentFraction;
        }
    }
}
