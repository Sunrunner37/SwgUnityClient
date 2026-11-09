using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Swg.Client.Runtime.UI.Screens.Intro
{
    public sealed class IntroScreen : ClientScreen
    {
        public PlayableDirector sequence;
        AudioClip music;
        bool playing;
        protected override bool RedirectOnStart()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(ClientNavigation.PreviewSessionKey, false))
                return false;
#endif
            if (!Client.Settings.SkipIntro)
                return false;
            ClientNavigation.Open(ScreenId.Login);
            return true;
        }

        protected override void OnAssetsBound()
        {
            sequence.time = 0;
            sequence.Evaluate();
        }

        protected override async Task PrepareAsync()
        {
            music = await Client.Music.LoadAsync(Client.Assets, musicPath);
        }

        protected override void OnReady()
        {
            sequence.stopped += OnSequenceStopped;
            Client.Music.Play(music, loopMusic);
            music = null;
            playing = true;
            sequence.time = 0;
            sequence.Play();
        }

        public void Skip()
        {
            playing = false;
            ClientNavigation.Open(ScreenId.Login);
        }

        void Update()
        {
            if (!ClientNavigation.IsChanging && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Skip();
        }

        void OnSequenceStopped(PlayableDirector director)
        {
            if (playing && isActiveAndEnabled && !ClientNavigation.IsChanging)
                Skip();
        }

        void OnDestroy()
        {
            playing = false;
            if (sequence != null)
                sequence.stopped -= OnSequenceStopped;
        }
    }
}
