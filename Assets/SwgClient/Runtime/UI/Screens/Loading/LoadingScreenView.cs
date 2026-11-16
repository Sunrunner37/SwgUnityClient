using TMPro;
using Swg.Client.Core.Assets.Loading;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Screens.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [Tooltip("Original DTII loading content table; {planet} comes from CmdStartScene.")]
        public string contentTable = "datatables/loading/space_{planet}.iff";
        public string fallbackTable = "datatables/loading/general.iff";
        public TMP_Text status;
        public TMP_Text planet;
        public TMP_Text title;
        public TMP_Text description;
        public TMP_Text percent;
        public RawImage picture;
        public RawImage planetImage;
        public RawImage flagImage;
        public Slider progress;
        public LoadingRadialProgress radial;
        public Button cancel;
        public void Show(LoadingScreenData content, TreRuntimeAssets assets)
        {
            if (planet != null)
                planet.text = content.Planet;
            title.text = content.Title.ToUpperInvariant();
            description.text = content.Description;
            picture.texture = assets.Texture(content.Picture);
            if (planetImage != null)
                planetImage.gameObject.SetActive(content.PlanetImage != null);
            if (flagImage != null)
                flagImage.gameObject.SetActive(content.FlagImage != null);
            if (planetImage != null)
                planetImage.texture = content.PlanetImage == null ? null : assets.Texture(content.PlanetImage);
            if (flagImage != null)
                flagImage.texture = content.FlagImage == null ? null : assets.Texture(content.FlagImage);
        }

        void LateUpdate()
        {
            float value = progress.value;
            if (radial != null)
                radial.SetValue(value);
            if (percent != null)
                percent.text = Mathf.FloorToInt(value * 100).ToString();
        }
    }
}
