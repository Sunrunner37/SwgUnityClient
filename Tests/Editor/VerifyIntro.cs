using System;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Runtime.UI.Screens.Intro;
using Swg.Client.Runtime.UI.Screens.Login;
using UnityEngine;
using UnityEngine.Playables;

namespace Swg.Client.Tests.Editor
{
    public static class VerifyIntro
    {
        public static async Task<string> Run()
        {
            var screen = UnityEngine.Object.FindAnyObjectByType<IntroScreen>();
            if (screen == null || !screen.IsReady)
                throw new InvalidOperationException("Start the Intro screen preview and wait for Ready.");
            var director = screen.sequence;
            director.Pause();
            director.timeUpdateMode = DirectorUpdateMode.Manual;
            var crawl = screen.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Original opening crawl");
            var camera = screen.GetComponentsInChildren<Camera>(true).Single(c => c.name == "SWG Intro Camera");
            var mesh = crawl.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            Texture texture = null;
            foreach (var time in new[]
            {
                15.0,
                18.0,
                35.0,
                75.0,
                79.0
            }

            )
            {
                director.time = time;
                director.Evaluate();
                var block = new MaterialPropertyBlock();
                crawl.GetComponent<Renderer>().GetPropertyBlock(block);
                var current = block.GetTexture("_BaseMap");
                if (current == null || (texture != null && current != texture))
                    throw new Exception("Timeline lost the native crawl texture at " + time);
                texture = current;
                if (time == 15 && camera.WorldToViewportPoint(crawl.TransformPoint(Vector3.zero)).y >= 0)
                    throw new Exception("Crawl appears inside the viewport on its first frame.");
                if (!mesh.vertices.SequenceEqual(vertices))
                    throw new Exception("Crawl animation changed shared mesh geometry.");
                if (time == 79 && Mathf.Abs(block.GetFloat("_Opacity") - .2f) > .02f)
                    throw new Exception("Crawl fade does not match its Timeline clip.");
            }

            director.time = 89.8;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.Play();
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (UnityEngine.Object.FindAnyObjectByType<LoginScreen>()?.IsReady != true)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Intro did not finish at the Login screen.");
                await Task.Delay(25);
            }

            return "INTRO_OK: offscreen entry, native texture, static geometry, fade and completion";
        }
    }
}
