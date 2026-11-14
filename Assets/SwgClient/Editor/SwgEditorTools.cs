using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Audio;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Audio;
using Swg.Client.Core.Characters.Creation;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.CharacterCreation;

namespace Swg.Client.Editor
{
    [InitializeOnLoad]
    public static class SwgEditorTools
    {
        const string SmokeKey = "Swg.Client.Smoke";
        static double deadline;
        static int frames;
        static string screenshot;
        static double lastDiagnostic;
        static EditorWindow gameView;
        static string Root => Directory.GetParent(Application.dataPath).FullName;

        static SwgEditorTools()
        {
            if (Application.isBatchMode && SessionState.GetBool(SmokeKey, false))
            {
                deadline = EditorApplication.timeSinceStartup + 90;
                EditorApplication.update += SmokeUpdate;
            }
        }

        [MenuItem("SWG/Play Client")]
        public static void Play() => ClientPlayMode.PlayClient();
        [MenuItem("SWG/Validate Original Assets")]
        public static void Validate()
        {
            try
            {
                var s = ClientSettings.From(Path.Combine(Root, "client.ini"));
                using (var loader = new TreAssetLoader(s.TreDirectory, s.TreArchivePriorities))
                {
                    var visuals = AssetDatabase.FindAssets("t:TreVisual", new[] { SwgProjectPaths.SwgAssets }).Select(AssetDatabase.GUIDToAssetPath).Select(path => AssetDatabase.LoadAssetAtPath<TreVisual>(path)).ToArray();
                    if (visuals.Length == 0)
                        throw new InvalidOperationException("No SWG visual asset definitions found.");
                    foreach (var visual in visuals)
                    {
                        try
                        {
                            var texture = visual.Load(loader);
                            if (texture.Width <= 0 || texture.Height <= 0 || texture.Pixels.Length != texture.Width * texture.Height * 4)
                                throw new InvalidDataException("Invalid rendered texture dimensions.");
                        }
                        catch (Exception e)
                        {
                            throw new InvalidDataException("SWG visual asset '" + AssetDatabase.GetAssetPath(visual) + "': " + e.Message, e);
                        }
                    }

                    var creation = new CharacterCreationData(loader);
                    foreach (var species in creation.Species)
                    {
                        SkeletalAppearanceLoader.Load(loader, species.Male);
                        SkeletalAppearanceLoader.Load(loader, species.Female);
                    }

                    var audio = MusicLoader.Load(loader, "sound/music_main_title.snd");
                    Debug.Log("SWG_VALIDATION_OK: " + loader.ArchiveCount + " archives; " + creation.Species.Count + " species; " + visuals.Length + " visual asset definitions; original DDS/UI/models/audio decoded in memory; title " + audio.Duration + " seconds.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
        }

        [MenuItem("SWG/Build Windows Client")]
        public static void BuildWindows()
        {
            string path = Path.Combine(Root, "Build", "SwgEmuClient.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("Add a scene to the build profile first.");
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("SWG build failed: " + report.summary.result);
            Debug.Log("SWG_BUILD_OK: " + path);
        }

        public static void SmokeTest()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("SmokeTest is batch-only. Use Preview Current Screen for interactive work.");
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SwgProjectPaths.Scenes + "/" + ScreenId.CharacterCreation + ".unity");
            SessionState.SetBool(SmokeKey, true);
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= SmokeUpdate;
            EditorApplication.update += SmokeUpdate;
            gameView = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            gameView.position = new Rect(50, 50, 1280, 840);
            gameView.Show();
            EditorApplication.isPlaying = true;
        }

        static void SmokeUpdate()
        {
            if (!Application.isBatchMode || !SessionState.GetBool(SmokeKey, false))
            {
                EditorApplication.update -= SmokeUpdate;
                return;
            }

            if (EditorApplication.timeSinceStartup > deadline)
            {
                SessionState.SetBool(SmokeKey, false);
                Debug.LogError("SWG smoke test timed out");
                EditorApplication.Exit(1);
                return;
            }

            gameView?.Repaint();
            var screen = UnityEngine.Object.FindAnyObjectByType<ClientScreen>();
            if (EditorApplication.timeSinceStartup - lastDiagnostic > 5)
            {
                lastDiagnostic = EditorApplication.timeSinceStartup;
                Debug.Log("SWG_SMOKE_WAIT: play=" + EditorApplication.isPlaying + ", app=" + (screen == null ? "missing" : screen.GetType().Name + ", ready=" + screen.IsReady));
            }

            if (!EditorApplication.isPlaying)
                return;
            if (screen == null)
                return; // The smoke test requires an authored frontend scene; never synthesize a replacement.
            if (!screen.IsReady)
                return;
            if (!(screen is CreationScreen))
                return;
            if (frames == 0)
            {
                screenshot = Path.Combine(Root, "Build", "QA", "creation.png");
                Directory.CreateDirectory(Path.GetDirectoryName(screenshot));
            }

            frames++;
            if (frames == 20)
                ScreenCapture.CaptureScreenshot(screenshot);
            if (frames > 40 && File.Exists(screenshot) && new FileInfo(screenshot).Length > 1000)
            {
                SessionState.SetBool(SmokeKey, false);
                Debug.Log("SWG_SMOKE_OK: native character scene and UI rendered to " + screenshot);
                EditorApplication.update -= SmokeUpdate;
                EditorApplication.Exit(0);
            }
        }
    }

    public sealed class CopyClientSettings : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            string target = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "client.ini");
            if (!File.Exists(target))
                File.Copy(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"), target);
        }
    }
}
