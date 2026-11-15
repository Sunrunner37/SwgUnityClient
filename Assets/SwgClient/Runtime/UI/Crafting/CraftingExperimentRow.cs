using System;
using Swg.Client.Runtime.UI.Containers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Swg.Client.Runtime.UI.Crafting
{
    public sealed class CraftingExperimentRow : MonoBehaviour
    {
        public TMP_Text label;
        public TMP_Text percentage;
        public Button[] bubbles;
        public Image[] fills;
        public CapacityBar progress;
        public CapacityBar potential;
        public bool showAllocations = true;
        public Image rowBackground;
        public Color rowColor;
        public Color alternateRowColor;
        public void Bind(Action<int> allocate)
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                int count = i + 1;
                bubbles[i].onClick.AddListener(() => allocate(count));
            }
        }
        public void Show(string name, float value, float maximum, int allocation, bool alternate = false)
        {
            if (rowBackground != null) rowBackground.color = alternate ? alternateRowColor : rowColor;
            label.text = name;
            percentage.text = Mathf.RoundToInt(value * 100) + "%";
            progress.Fill = value;
            if (potential != null) potential.Fill = maximum;
            for (int i = 0; i < bubbles.Length; i++)
            {
                bubbles[i].gameObject.SetActive(showAllocations && i < Mathf.CeilToInt(maximum * 10));
                fills[i].color = i < allocation ? new Color32(255, 225, 0, 255) : new Color32(76, 66, 158, 255);
            }
        }
    }
}
