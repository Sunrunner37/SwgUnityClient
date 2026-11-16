using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI.Theming;
using TMPro;
using UnityEngine;
namespace Swg.Client.Runtime.UI.Testing
{
    /// <summary>Loads the actual ground HUD palettes from ui_styles.inc; changes only this test scene's theme instances.</summary>
    public sealed class UiThemeSelector : MonoBehaviour
    {
        public TMP_Dropdown dropdown;
        public UiTheme defaultTheme;
        readonly List<UiTheme> themes = new List<UiTheme>();
        readonly List<Material> materials = new List<Material>();
        UiTheme original;
        IEnumerator Start()
        {
            original = Instantiate(defaultTheme);
            themes.Add(original);
            var task = GameServices.Resolve<TreRuntimeAssets>().ReadAsync(loader => UiNode.Parse(loader.Text("ui/ui_styles.inc")).Find("palette").Children.ToArray());
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) { Debug.LogException(task.Exception.GetBaseException(), this); yield break; }
            foreach (var palette in task.Result)
            {
                var theme = Instantiate(defaultTheme);
                theme.name = palette.Get("Name");
                Color Get(string key, Color fallback) => ColorUtility.TryParseHtmlString(palette.Get(key), out var color) ? color : fallback;
                theme.accent = Get("default", theme.accent);
                theme.text = Get("textdefault", theme.text);
                theme.commandIcon = Get("icondefault", theme.commandIcon);
                theme.surface = Get("back2", theme.surface);
                theme.input = Get("back3", theme.input);
                theme.tab = Get("back1", theme.tab);
                theme.onAccent = Get("textHover", theme.onAccent);
                theme.rowDivider = Get("listline", theme.rowDivider);
                theme.skill = Get("skilldefault", theme.skill);
                theme.skillExperience = Get("exp", theme.skillExperience);
                theme.learnedSkill = Get("skillAcquiredDefault", theme.learnedSkill);
                theme.highlight = Get("selected", theme.highlight);
                theme.headerBar = Get("default", theme.headerBar);
                theme.headerTitle = Get("textHover", theme.headerTitle);
                theme.headerStrip = new Color(theme.accent.r, theme.accent.g, theme.accent.b, .12f);
                theme.headerDivider = theme.accent;
                theme.closeIcon = Get("closeNormal", theme.closeIcon);
                theme.closeGlow = Get("closeDarkNormal", theme.closeGlow);
                Material Surface(Material source)
                {
                    if (source == null) return null;
                    var result = new Material(source); materials.Add(result);
                    result.SetColor("_OuterColor", Get("back3", theme.surface));
                    result.SetColor("_MiddleColor", Get("back2", theme.surface));
                    result.SetColor("_InnerColor", Get("back1", theme.surface));
                    result.SetColor("_BandColor", Get("line1", theme.accent));
                    return result;
                }
                theme.itemPanel = Surface(theme.itemPanel);
                theme.characterPanel = Surface(theme.characterPanel);
                theme.currencyPanel = Surface(theme.currencyPanel);
                themes.Add(theme);
            }
            dropdown.ClearOptions();
            dropdown.AddOptions(themes.Select(theme => theme == original ? "Default blue" : theme.name).ToList());
            dropdown.onValueChanged.AddListener(Select);
            Select(0);
        }
        void Select(int index)
        {
            // The same palette asset is referenced by prefab templates and their future instances.
            // Change its runtime values, then restore them on teardown; no asset is saved or marked dirty.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(themes[index]), defaultTheme);
            defaultTheme.NotifyChanged();
            foreach (var binding in FindObjectsByType<UiThemeBinding>(FindObjectsInactive.Include))
                if (binding.gameObject.scene == gameObject.scene) binding.Apply();
        }
        void OnDestroy()
        {
            dropdown.onValueChanged.RemoveListener(Select);
            if (original != null)
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(original), defaultTheme);
                defaultTheme.NotifyChanged();
            }
            foreach (var material in materials) Destroy(material);
            foreach (var theme in themes) Destroy(theme);
        }
    }
}
