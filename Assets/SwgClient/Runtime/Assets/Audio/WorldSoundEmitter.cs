using System.Collections;
using Swg.Client.Core.Assets.Audio;
using Swg.Client.Runtime.World;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Audio
{
    /// <summary>Plays a native ambient sound using shared clips. The musical note is a Scene-view gizmo only.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class WorldSoundEmitter : MonoBehaviour
    {
        SoundAppearance sound;
        AudioSource source;
        Coroutine playback;
        [SerializeField] string soundTemplatePath;
        public void Initialize(SoundAppearance appearance)
        {
            sound = appearance;
            soundTemplatePath = appearance.Path;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            BeginPlayback();
        }

        void OnEnable() => BeginPlayback();

        void BeginPlayback()
        {
            if (source != null && isActiveAndEnabled && playback == null)
                playback = StartCoroutine(Play());
        }

        bool IsAudible(Transform listener) => sound.Settings.MinimumDistance <= 0 ||
            (listener.position - transform.position).sqrMagnitude < sound.Settings.MaximumDistance * sound.Settings.MaximumDistance;

        IEnumerator Play()
        {
            var settings = sound.Settings;
            var clips = GameServices.Resolve<AudioClipLoader>();
            var listener = GameServices.Resolve<WorldRenderingContext>().WorldCamera.transform;
            float Pick(Vector2 range) => Random.Range(range.x, range.y);
            source.spatialBlend = settings.Spatial ? 1 : 0;
            source.dopplerLevel = 0;
            if (settings.MinimumDistance > 0)
            {
                source.minDistance = settings.MinimumDistance;
                source.maxDistance = settings.MaximumDistance;
            }
            // Apply native linear distance attenuation ourselves, also for unpanned SD2D loops.
            source.rolloffMode = AudioRolloffMode.Custom;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0, 1, 1));
            var tick = new WaitForSecondsRealtime(.1f);
            float delay = Pick(settings.StartDelay);
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            int count = settings.Infinite ? int.MaxValue : Mathf.Max(1, Random.Range(settings.LoopCountMin, settings.LoopCountMax + 1));
            int sample = 0;
            for (int play = 0; play < count; play++)
            {
                while (!IsAudible(listener)) yield return tick;
                int index = settings.PlayOrder == 0 ? sample++ % settings.Samples.Length : Random.Range(0, settings.Samples.Length);
                var task = clips.LoadAsync(settings.Samples[index]);
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.IsFaulted)
                {
                    Debug.LogWarning("Ambient sound " + soundTemplatePath + ": " + task.Exception.GetBaseException().Message, this);
                    yield break;
                }
                if (task.IsCanceled) yield break;
                while (!IsAudible(listener)) yield return tick;
                source.clip = task.Result;
                source.pitch = Mathf.Pow(2, Pick(settings.Pitch) / 12);
                source.loop = settings.Infinite && settings.Samples.Length == 1 && settings.LoopDelay.y == 0;
                float volume = Pick(settings.Volume);
                float fadeIn = Pick(settings.FadeIn);
                float fadeOut = Pick(settings.FadeOut);
                double started = Time.realtimeSinceStartupAsDouble;
                source.volume = 0;
                source.Play();
                while (source.isPlaying)
                {
                    if (!IsAudible(listener))
                    {
                        source.Pause();
                        while (!IsAudible(listener)) yield return tick;
                        source.UnPause();
                    }
                    float attenuation = settings.MinimumDistance <= 0 ? 1 : Mathf.Clamp01((settings.MaximumDistance - Vector3.Distance(listener.position, transform.position)) / (settings.MaximumDistance - settings.MinimumDistance));
                    float fade = fadeIn <= 0 ? 1 : Mathf.Clamp01((float)(Time.realtimeSinceStartupAsDouble - started) / fadeIn);
                    if (!source.loop && fadeOut > 0) fade = Mathf.Min(fade, Mathf.Clamp01((source.clip.length - source.time) / (source.pitch * fadeOut)));
                    source.volume = volume * attenuation * fade;
                    yield return tick;
                }
                delay = Pick(settings.LoopDelay);
                if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            }
        }

        void OnDisable()
        {
            if (playback != null) StopCoroutine(playback);
            playback = null;
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (Camera.current == null || Camera.current.cameraType != CameraType.SceneView) return;
            UnityEditor.Handles.Label(transform.position, "♫", NoteStyle);
#endif
        }
#if UNITY_EDITOR
        static GUIStyle noteStyle;
        static GUIStyle NoteStyle => noteStyle ??= new GUIStyle(UnityEditor.EditorStyles.boldLabel)
        {
            fontSize = 32,
            normal = { textColor = new Color(.85f, .3f, 1) }
        };
#endif
    }
}
