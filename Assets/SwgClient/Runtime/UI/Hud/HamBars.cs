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
        Color[] capacityColors;
        void Awake() { capacityColors = System.Array.ConvertAll(capacityBars, bar => bar.color); }

        public void Show(HamPool ham)
        {
            UpdateBar(0, ham.Health, ham.LargestMaximum);
            UpdateBar(1, ham.Action, ham.LargestMaximum);
            UpdateBar(2, ham.Mind, ham.LargestMaximum);
        }

        public void ShowCondition(float fraction)
        {
            for (int i = 0; i < currentBars.Length; i++) currentBars[i].transform.parent.gameObject.SetActive(i == 0);
            var row = (RectTransform)currentBars[0].transform.parent;
            row.anchorMax = Vector2.one;
            currentBars[0].fillAmount = fraction;
            capacityBars[0].fillAmount = 1;
            capacityBars[0].color = new Color(.7f, .7f, .7f, 1);
            woundBars[0].fillAmount = 0;
        }

        void UpdateBar(int index, HamElement value, int largestMaximum)
        {
            currentBars[index].transform.parent.gameObject.SetActive(true);
            var row = (RectTransform)currentBars[index].transform.parent;
            row.anchorMax = new Vector2(value.RelativeWidth(largestMaximum), 1);
            capacityBars[index].fillAmount = value.CapacityFraction;
            if (capacityColors != null) capacityBars[index].color = capacityColors[index];
            woundBars[index].fillAmount = value.WoundFraction;
            currentBars[index].fillAmount = value.CurrentFraction;
        }
    }
}
