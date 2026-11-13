using System;
using TMPro;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Theming
{
    [CreateAssetMenu(menuName = "SWG/UI/Theme")]
    public sealed class UiTheme : ScriptableObject
    {
        public TMP_FontAsset font;
        [Min(8)]
        public float fontSize = 12;
        public Color accent = new Color32(0, 214, 251, 255);
        public Color text = new Color32(150, 244, 252, 255);
        public Color surface = new Color32(0, 35, 45, 225);
        public Color input = new Color32(0, 45, 57, 230);
        public Color tab = new Color32(0, 47, 60, 255);
        public Color onAccent = new Color32(0, 45, 55, 255);
        public Color highlight = new Color32(150, 245, 0, 255);
        public Color attributeText = new Color32(192, 255, 0, 255);
        public Color bodyText = Color.white;
        public Color headerTitle = new Color32(0, 30, 33, 255);
        public Color headerStrip = new Color(0, .8f, .9f, .12f);
        public Color headerDivider = new Color(0, .75f, .85f, .8f);
        public Color closeIcon = new Color32(0, 62, 79, 255);
        public Color closeGlow = new Color(0, .9f, 1, .7f);
        public Color capacityFill = new Color32(102, 255, 0, 255);
        [Tooltip("Panel materials own their gradient and animated-band colors. Each theme can supply its own palette.")]
        public Material itemPanel;
        public Material characterPanel;
        public Material currencyPanel;
        public event Action Changed;
        public void NotifyChanged() => Changed?.Invoke();
        void OnValidate() => NotifyChanged();
    }
}
