using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Swg.Client.Runtime.UI.Inventory;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.UI;
using Swg.Client.Runtime.UI.Windows;
using Swg.Client.Runtime.UI.Theming;

namespace Swg.Client.Tests.Editor
{
    public sealed class UiPresentationChecks
    {
        [Test]
        public void SharedBodyFontPersistsMatchingGlyphsAtlasAndMaterial()
        {
            const string path = "Assets/SwgAssets/UI/FrontendFont.asset";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            font.ReadFontAssetDefinition();
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(font.material.mainTexture == font.atlasTextures[0], Is.True);
            Assert.That(font.material.GetFloat(ShaderUtilities.ID_TextureWidth), Is.EqualTo(font.atlasWidth));
            Assert.That(font.material.GetFloat(ShaderUtilities.ID_TextureHeight), Is.EqualTo(font.atlasHeight));
            foreach (char character in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
            {
                var glyph = font.characterLookupTable[character].glyph;
                var texture = font.atlasTextures[glyph.atlasIndex];
                var rect = glyph.glyphRect;
                Assert.That(rect.x + rect.width, Is.LessThanOrEqualTo(texture.width));
                Assert.That(rect.y + rect.height, Is.LessThanOrEqualTo(texture.height));
                Assert.That(texture.GetPixels(rect.x, rect.y, rect.width, rect.height).Max(pixel => pixel.a), Is.GreaterThan(.5f), character.ToString());
            }
            Assert.That(font.fallbackFontAssetTable.Any(fallback => fallback.atlasPopulationMode == AtlasPopulationMode.Dynamic), Is.True);
        }
        [Test]
        public void InventoryPresentationUsesWhiteTextAndSharedNativeScrollbarControls()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Inventory/Inventory.prefab");
            var inventory = root.GetComponent<InventoryWindow>();
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab").GetComponent<Canvas>();
            Assert.That(hud.updateRectTransformForStandalone, Is.EqualTo(StandaloneRenderResize.Disabled));
            var preloader = hud.GetComponent<UiAssetPreloader>();
            Assert.That(preloader, Is.Not.Null);
            var templates = preloader.GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<IUiPrefabSource>().SelectMany(source => source.UiPrefabs);
            Assert.That(templates, Does.Contain(inventory.itemPrefab.gameObject));
            Assert.That(inventory.details.theme.attributeText, Is.EqualTo((Color)new Color32(192, 255, 0, 255)));
            Assert.That(inventory.details.text.color, Is.EqualTo(Color.white));
            Assert.That(inventory.details.text.richText, Is.True);
            Assert.That(root.transform.Find("Dark background"), Is.Null);
            Assert.That(inventory.characterPreview.previewCamera.backgroundColor.a, Is.Zero);
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            {
                var skin = scroll.verticalScrollbar.GetComponent<WindowScrollbar>();
                Assert.That(skin, Is.Not.Null);
                Assert.That(skin.up.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(skin.down.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(skin.transform.Find("Rail"), Is.Not.Null);
            }
            var item = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Containers/ContainerItem.prefab").GetComponent<ContainerItem>();
            Assert.That(item.equipped.GetComponent<Outline>(), Is.Not.Null);
            Assert.That(item.itemName.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void WindowsUseSharedHeaderWithTheirOwnTitleDragTargetAndCloseAction()
        {
            foreach (string name in new[] { "Inventory/Inventory", "Interactions/ExamineWindow", "Commands/CommandBrowser" })
            {
                var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/" + name + ".prefab");
                try
                {
                    var header = root.transform.Find("WindowHeader");
                    Assert.That(header, Is.Not.Null, name);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(header.gameObject), Is.EqualTo("Assets/Prefabs/UI/Windows/WindowHeader.prefab"));
                    Assert.That(header.GetComponentInChildren<WindowHandle>().window, Is.EqualTo(root.GetComponent<UiWindow>()));
                    var close = header.GetComponentInChildren<Button>();
                    Assert.That(close.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                    Assert.That(((Component)close.onClick.GetPersistentTarget(0)).gameObject, Is.EqualTo(root));
                    Assert.That(close.onClick.GetPersistentMethodName(0), Is.EqualTo("Close"));
                    Assert.That(root.GetComponent<UiThemeBinding>().children, Does.Contain(header.GetComponent<UiThemeBinding>()));
                    var title = header.GetComponentInChildren<TMP_Text>();
                    Assert.That(title.text, Is.Not.EqualTo("WINDOW"));
                    var inventory = root.GetComponent<InventoryWindow>();
                    if (inventory != null)
                        Assert.That(inventory.title, Is.EqualTo(title));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        [Test]
        public void ThemeChangesUpdateNestedHeaderEffectsAndPanelMaterialsWithoutMutatingSourcePalette()
        {
            var original = AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/SwgAssets/UI/DefaultTheme.asset");
            var alternate = Object.Instantiate(original);
            var material = new Material(original.itemPanel);
            material.SetColor("_OuterColor", Color.red);
            material.SetColor("_BandColor", Color.magenta);
            alternate.itemPanel = material;
            alternate.headerDivider = Color.yellow;
            alternate.closeGlow = Color.green;
            var before = original.itemPanel.GetColor("_OuterColor");
            var root = new GameObject("Window", typeof(RectTransform), typeof(UiThemeBinding));
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(Shadow), typeof(UiThemeBinding));
            try
            {
                panel.transform.SetParent(root.transform, false);
                header.transform.SetParent(root.transform, false);
                var parent = root.GetComponent<UiThemeBinding>();
                var child = header.GetComponent<UiThemeBinding>();
                parent.theme = original;
                parent.children.Add(child);
                parent.surfaces.Add(new UiThemeBinding.SurfaceBinding { graphic = panel.GetComponent<Image>(), surface = UiThemeBinding.Surface.Item });
                child.colors.Add(new UiThemeBinding.Binding { graphic = header.GetComponent<Image>(), role = UiThemeBinding.Role.HeaderDivider });
                child.effects.Add(new UiThemeBinding.EffectBinding { effect = header.GetComponent<Shadow>(), role = UiThemeBinding.Role.CloseGlow });
                parent.Apply();
                Assert.That(panel.GetComponent<Image>().material, Is.EqualTo(original.itemPanel));
                parent.theme = alternate;
                parent.Apply();
                alternate.headerDivider = Color.cyan;
                alternate.NotifyChanged();
                Assert.That(child.theme, Is.EqualTo(alternate));
                Assert.That(header.GetComponent<Image>().color, Is.EqualTo(Color.cyan));
                Assert.That(header.GetComponent<Shadow>().effectColor, Is.EqualTo(Color.green));
                Assert.That(panel.GetComponent<Image>().material, Is.EqualTo(material));
                Assert.That(panel.GetComponent<Image>().material.GetColor("_BandColor"), Is.EqualTo(Color.magenta));
                original.NotifyChanged();
                Assert.That(child.theme, Is.EqualTo(alternate));
                Assert.That(original.itemPanel.GetColor("_OuterColor"), Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(alternate);
            }
        }

        [Test]
        public void InventoryPrefabSourceUsesTheCurrentWidgetReference()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Inventory/Inventory.prefab");
            try
            {
                var window = root.GetComponent<InventoryWindow>();
                var source = (IUiPrefabSource)window;
                Assert.That(source.UiPrefabs.Single(), Is.EqualTo(window.itemPrefab.gameObject));
                var replacement = new GameObject("Replacement item", typeof(RectTransform), typeof(ContainerItem));
                replacement.transform.SetParent(root.transform, false);
                window.itemPrefab = replacement.GetComponent<ContainerItem>();
                Assert.That(source.UiPrefabs.Single(), Is.EqualTo(replacement));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
