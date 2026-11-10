using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Editor
{
    [InitializeOnLoad]
    public static class TreEditorAssets
    {
        static TreAssetLoader loader;
        static readonly Dictionary<TreVisual, TextureData> visuals = new Dictionary<TreVisual, TextureData>();
        static readonly Dictionary<TextureData, Texture2D> textures = new Dictionary<TextureData, Texture2D>();
        static readonly Dictionary<string, TextureData> decoded = new Dictionary<string, TextureData>();
        public static TreAssetLoader Loader
        {
            get
            {
                if (loader == null)
                {
                    var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
                    loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
                }

                return loader;
            }
        }

        public static Texture2D Load(TreVisual asset)
        {
            if (!visuals.TryGetValue(asset, out var source))
            {
                source = asset.Load(Loader);
                visuals.Add(asset, source);
            }

            return Convert(source);
        }

        public static void Invalidate(TreVisual asset)
        {
            if (!visuals.TryGetValue(asset, out var source))
                return;
            visuals.Remove(asset);
            if (textures.TryGetValue(source, out var texture))
            {
                textures.Remove(source);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static TreEditorAssets()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
            EditorApplication.quitting += Close;
            EditorApplication.projectChanged += QueueRefresh;
            Undo.undoRedoPerformed += () =>
            {
                Close();
                Refresh();
            };
            EditorSceneManager.sceneOpened += (_, __) =>
            {
                Refresh();
                QueueRefresh();
            };
            PrefabStage.prefabStageOpened += _ => QueueRefresh();
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    QueueRefresh();
            };
            EditorApplication.delayCall += QueueRefresh;
        }

        static int refreshFrames;
        static void QueueRefresh()
        {
            refreshFrames = 3;
            EditorApplication.update -= RefreshPending;
            EditorApplication.update += RefreshPending;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        static void RefreshPending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || --refreshFrames < 0)
            {
                EditorApplication.update -= RefreshPending;
                return;
            }

            Refresh();
            Canvas.ForceUpdateCanvases();
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.GetType().Name == "GameView")
                    window.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        [MenuItem("SWG/Refresh TRE Previews")]
        public static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            try
            {
                foreach (var binding in Resources.FindObjectsOfTypeAll<TreTextureBinding>())
                    if (!EditorUtility.IsPersistent(binding) && binding.gameObject.scene.IsValid())
                        binding.Apply(Load);
                // Prefab/scene reloads can reset CanvasRenderer geometry without changing
                // the RawImage's managed texture reference. Rebuild it explicitly.
                foreach (var graphic in Resources.FindObjectsOfTypeAll<Graphic>())
                    if (!EditorUtility.IsPersistent(graphic) && graphic.gameObject.scene.IsValid())
                        graphic.SetAllDirty();
                Canvas.ForceUpdateCanvases();
                SceneView.RepaintAll();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SWG TRE preview: " + e.Message);
            }
        }

        public static Texture2D Texture(string path)
        {
            if (!decoded.TryGetValue(path, out var source))
            {
                source = TextureData.Load(Loader, path);
                decoded.Add(path, source);
            }

            return Convert(source);
        }

        public static Texture2D Convert(TextureData source)
        {
            if (textures.TryGetValue(source, out var tex))
                return tex;
            tex = new Texture2D(source.Width, source.Height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "TRE preview",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new byte[source.Pixels.Length];
            for (int y = 0; y < source.Height; y++)
                Buffer.BlockCopy(source.Pixels, y * source.Width * 4, pixels, (source.Height - 1 - y) * source.Width * 4, source.Width * 4);
            tex.LoadRawTextureData(pixels);
            tex.Apply(false, true);
            textures.Add(source, tex);
            return tex;
        }

        public static void Close()
        {
            foreach (var tex in textures.Values)
                if (tex != null)
                    UnityEngine.Object.DestroyImmediate(tex);
            textures.Clear();
            decoded.Clear();
            visuals.Clear();
            loader?.Dispose();
            loader = null;
        }
    }

    [CustomEditor(typeof(TreTextureBinding))]
    public sealed class TreTextureBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Assign an SWG Texture, Panel, or Button asset. Create these using Assets > Create > SWG; edit SWG paths and composition on the asset. RectTransform and UI tint remain editable here.", MessageType.Info);
            var selected = ((TreTextureBinding)target).asset;
            if (selected != null && GUILayout.Button("Select asset definition"))
                Selection.activeObject = selected;
            if (changed || GUILayout.Button("Load / refresh preview"))
            {
                try
                {
                    var binding = (TreTextureBinding)target;
                    if (binding.enabled)
                        binding.Apply(TreEditorAssets.Load);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

    [CustomEditor(typeof(TreVisual), true)]
    public sealed class TreVisualAssetEditor : UnityEditor.Editor
    {
        string error;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            var asset = (TreVisual)target;
            if (changed || GUILayout.Button("Reload from SWG archives"))
            {
                TreEditorAssets.Invalidate(asset);
                TreEditorAssets.Refresh();
                error = null;
            }

            try
            {
                var texture = TreEditorAssets.Load(asset);
                EditorGUILayout.LabelField(texture.width + " � " + texture.height + " � archive pixels remain external");
                EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(64, 220), texture, null, ScaleMode.ScaleToFit);
                error = null;
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            if (error != null)
                EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    public sealed class TreAssetBrowser : EditorWindow
    {
        KeyValuePair<string, string>[] catalog = Array.Empty<KeyValuePair<string, string>>();
        KeyValuePair<string, string>[] matches = Array.Empty<KeyValuePair<string, string>>();
        string search = "texture/";
        string selected = "";
        string source = "";
        string error = "";
        string details = "";
        Vector2 scroll;
        Texture2D preview;
        [MenuItem("SWG/TRE Asset Browser")]
        public static void Open()
        {
            GetWindow<TreAssetBrowser>("SWG TRE Assets");
        }

        void OnEnable()
        {
            Mount();
        }

        void Mount()
        {
            try
            {
                catalog = TreEditorAssets.Loader.Catalog().OrderBy(p => p.Key).ToArray();
                Filter();
                error = "";
            }
            catch (Exception e)
            {
                error = e.Message;
            }
        }

        void Filter()
        {
            matches = catalog.Where(p => p.Key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            scroll = Vector2.zero;
        }

        void Select(KeyValuePair<string, string> entry)
        {
            selected = entry.Key;
            source = entry.Value;
            preview = null;
            details = "";
            error = "";
            try
            {
                if (selected.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                    preview = TreEditorAssets.Texture(selected);
                else
                {
                    var bytes = TreEditorAssets.Loader.Read(selected);
                    details = bytes.Length.ToString("N0") + " bytes\n";
                    if (selected.EndsWith(".txt") || selected.EndsWith(".cfg") || selected.EndsWith(".inc"))
                        details += System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 16000));
                    else
                        details += BitConverter.ToString(bytes, 0, Math.Min(bytes.Length, 256)).Replace("-", " ");
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Original asset archives", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Mounted using [Assets] in client.ini. DDS previews load in memory; Assign adds a virtual path to the selected UI object. No original assets are copied into Assets/.", MessageType.None);
            if (GUILayout.Button("Reload archives"))
            {
                TreEditorAssets.Close();
                Mount();
                TreEditorAssets.Refresh();
            }

            EditorGUI.BeginChangeCheck();
            search = EditorGUILayout.TextField("Search virtual paths", search);
            if (EditorGUI.EndChangeCheck())
                Filter();
            EditorGUILayout.LabelField(matches.Length.ToString("N0") + " matches / " + catalog.Length.ToString("N0") + " virtual assets");
            // Draw only visible rows even when the catalog contains hundreds of thousands of names.
            float height = Mathf.Clamp(position.height * .35f, 100, 300);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(height));
            int first = Math.Max(0, (int)(scroll.y / 20));
            int last = Math.Min(matches.Length, first + (int)(height / 20) + 3);
            GUILayout.Space(first * 20);
            for (int i = first; i < last; i++)
                if (GUILayout.Button(matches[i].Key, EditorStyles.miniButton, GUILayout.Height(18)))
                    Select(matches[i]);
            GUILayout.Space((matches.Length - last) * 20);
            GUILayout.EndScrollView();
            EditorGUILayout.SelectableLabel(selected, EditorStyles.boldLabel, GUILayout.Height(20));
            EditorGUILayout.SelectableLabel(source, EditorStyles.miniLabel, GUILayout.Height(20));
            if (!string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox(error, MessageType.Error);
            if (preview != null)
            {
                EditorGUILayout.LabelField(preview.width + " � " + preview.height);
                var rect = GUILayoutUtility.GetRect(64, Mathf.Max(64, position.height - height - 255));
                EditorGUI.DrawPreviewTexture(rect, preview, null, ScaleMode.ScaleToFit);
                using (new EditorGUI.DisabledScope(Selection.activeGameObject == null || Selection.activeGameObject.GetComponent<RawImage>() == null))
                    if (GUILayout.Button("Assign DDS to selected RawImage"))
                    {
                        var go = Selection.activeGameObject;
                        var binding = go.GetComponent<TreTextureBinding>() ?? Undo.AddComponent<TreTextureBinding>(go);
                        string folder = SwgProjectPaths.SwgAssets;
                        Directory.CreateDirectory(folder);
                        AssetDatabase.Refresh();
                        var definition = CreateInstance<TreTexture>();
                        definition.swgPath = selected;
                        AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + Path.GetFileNameWithoutExtension(selected) + ".asset"));
                        Undo.RegisterCreatedObjectUndo(definition, "Create SWG texture asset");
                        Undo.RecordObject(binding, "Assign TRE texture");
                        binding.asset = definition;
                        binding.Apply(TreEditorAssets.Load);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
                        EditorUtility.SetDirty(binding);
                    }
            }
            else
                EditorGUILayout.TextArea(details, GUILayout.ExpandHeight(true));
            if (selected.Length > 0 && GUILayout.Button("Copy virtual path"))
                EditorGUIUtility.systemCopyBuffer = selected;
        }
    }
}
