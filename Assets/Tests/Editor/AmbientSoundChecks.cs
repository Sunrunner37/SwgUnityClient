using System.Collections;
using Autofac;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Audio;
using Swg.Client.Runtime.World;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Audio;
using Swg.Client.Core.Assets.Formats.Wav;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class AmbientSoundChecks
    {
        [Test]
        public void StarportEmitterLoadsNativeLoopInsteadOfToolMarker()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "object/soundobject/shared_soundobject_starport_interior.iff");
            Assert.That(appearance.HasContent, Is.True, "Streaming must retain audio-only objects.");
            Assert.That(appearance.Parts, Is.Empty);
            Assert.That(appearance.Particles, Is.Empty, "The musical-note particle is a tool marker.");
            var sound = appearance.Sounds.Single();
            Assert.That(sound.Path, Is.EqualTo("sound/amb_starport_int_lp.snd"));
            Assert.That(sound.Settings.Infinite, Is.True);
            Assert.That(sound.Settings.FadeIn, Is.EqualTo(new Vector2(1.5f,1.5f)));
            Assert.That(sound.Settings.Volume.x, Is.EqualTo(.63f).Within(.001f));
            Assert.That(sound.Settings.MinimumDistance, Is.EqualTo(2));
            Assert.That(sound.Settings.MaximumDistance, Is.EqualTo(8));
            var pcm = MusicLoader.Load(assets.Loader, sound.Settings.Samples.Single());
            Assert.That(pcm.Duration, Is.EqualTo(14).Within(.01));
            Assert.That(pcm.Channels, Is.EqualTo(2));
            Assert.That(pcm.Rate, Is.EqualTo(22050));
            Assert.That(pcm.Samples.Any(sample => Math.Abs(sample) > .01f), Is.True);
        }

        [TestCase("starport_announcer")]
        [TestCase("cantina_medium")]
        [TestCase("city_crowd_small")]
        public void OtherAmbientEmittersResolvePlayableSamples(string name)
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "object/soundobject/shared_soundobject_" + name + ".iff");
            foreach (var sound in appearance.Sounds)
            {
                Assert.That(sound.Settings.Samples, Is.Not.Empty);
                Assert.That(MusicLoader.Load(assets.Loader, sound.Settings.Samples[0]).Duration, Is.GreaterThan(0));
            }
            Assert.That(appearance.Sounds, Is.Not.Empty);
            Assert.That(appearance.Particles, Is.Empty);
        }

        [UnityTest]
        public IEnumerator PlaybackFollowsAudibilityAndEnabledLifetime()
        {
            yield return new EnterPlayMode();
            var initial = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Ambient sound lifecycle test"));
            yield return SceneManager.UnloadSceneAsync(initial);
            var cameraObject = new GameObject("Sound listener test");
            var context = new WorldRenderingContext { WorldCamera = cameraObject.AddComponent<Camera>() };
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            using var clips = new AudioClipLoader(assets);
            using var services = ManagedObjectTestScope.Use(new SceneObjectRegistry(), builder =>
            {
                builder.RegisterInstance(context);
                builder.RegisterInstance(clips).ExternallyOwned();
            });
            var root = new GameObject("Ambient playback test");
            try
            {
                var sound = SoundAppearance.Load(assets.Loader, "object/soundobject/shared_soundobject_starport_interior.iff", Matrix4x4.identity).Single();
                sound.Settings.StartDelay = Vector2.zero;
                sound.Settings.FadeIn = Vector2.zero;
                cameraObject.transform.position = Vector3.right * 1000;
                var emitter = root.AddComponent<WorldSoundEmitter>();
                emitter.Initialize(sound);
                var source = root.GetComponent<AudioSource>();
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(source.clip, Is.Null, "Out-of-range emitters must not load clips.");
                var cache = (System.Collections.IDictionary)typeof(AudioClipLoader).GetField("clips", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(clips);
                var pending = (System.Collections.IDictionary)typeof(AudioClipLoader).GetField("pending", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(clips);
                Assert.That(cache.Count + pending.Count, Is.Zero, "No decode should start before audibility.");
                emitter.enabled = false;
                cameraObject.transform.position = Vector3.zero;
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(cache.Count + pending.Count, Is.Zero, "Disabling must stop the waiting coroutine.");
                emitter.enabled = true;
                float deadline = Time.realtimeSinceStartup + 10;
                while (source.clip == null && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(source.isPlaying, Is.True);
                cameraObject.transform.position = Vector3.right * 1000;
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(source.isPlaying, Is.False, "Out-of-range playback should pause.");
                cameraObject.transform.position = Vector3.zero;
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(source.isPlaying, Is.True, "Audible playback should resume.");
                emitter.enabled = false;
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(source.isPlaying, Is.False);
                Assert.That(source.clip, Is.Null);
                emitter.enabled = true;
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(source.isPlaying, Is.True, "Re-enabling the component restarts playback.");
                root.SetActive(false);
                yield return null;
                root.SetActive(true);
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(source.isPlaying, Is.True, "Reactivating the object restarts playback.");
                emitter.enabled = false;
                sound.Settings.MinimumDistance = 0;
                cameraObject.transform.position = Vector3.right * 1000;
                emitter.enabled = true;
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(source.isPlaying, Is.True, "Unattenuated sounds must play at any distance.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTearDown]
        public IEnumerator ExitPlaybackTest()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [Test]
        public void WaveDecoderSkipsPaddedMetadataAndPreservesSignedSamples()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            void Tag(string name) => writer.Write(Encoding.ASCII.GetBytes(name));
            Tag("RIFF"); writer.Write(0); Tag("WAVE");
            Tag("JUNK"); writer.Write(1); writer.Write((byte)7); writer.Write((byte)0);
            Tag("fmt "); writer.Write(16); writer.Write((ushort)1); writer.Write((ushort)1); writer.Write(8000); writer.Write(16000); writer.Write((ushort)2); writer.Write((ushort)16);
            Tag("data"); writer.Write(6); writer.Write(short.MinValue); writer.Write((short)0); writer.Write(short.MaxValue);
            var bytes = stream.ToArray(); BitConverter.GetBytes(bytes.Length - 8).CopyTo(bytes,4);
            var pcm = WavFile.Decode(bytes);
            Assert.That(pcm.Rate, Is.EqualTo(8000));
            Assert.That(pcm.Samples, Is.EqualTo(new[]{-1f,0,32767/32768f}));
        }
    }
}
