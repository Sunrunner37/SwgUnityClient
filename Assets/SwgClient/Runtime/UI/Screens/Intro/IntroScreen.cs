using Swg.Client.Runtime.Assets.Audio;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Swg.Client.Runtime.UI.Screens.Intro
{
    public sealed class IntroScreen : ClientScreen
    {
        public PlayableDirector sequence;
        public CanvasGroup content;
        [Range(0, 1)]
        public float musicVolume = .65f;
        public string musicPath;
        bool isClosing;
        ClientMusic music;
        void Awake()
        {
            content.alpha = 0;
            content.interactable = false;
            content.blocksRaycasts = false;
        }

        IEnumerator Start()
        {
            System.Threading.Tasks.Task<AudioClip> task;
            try
            {
                if (GameServices.Resolve<ClientSettings>().SkipIntro)
                {
                    Skip();
                    yield break;
                }

                sequence.time = 0;
                sequence.Evaluate();
                Debug.Log("IntroScreen: Loading intro music", this);
                music = GameServices.Resolve<ClientMusic>();
                task = GameServices.Resolve<AudioClipLoader>().LoadAsync(musicPath);
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception.GetBaseException(), this);
                yield break;
            }

            if (task.IsCanceled)
                yield break;
            if (isClosing)
                yield break;
            yield return null;
            try
            {
                TreTextureBinding.ThrowIfFailed(content.transform);
                content.alpha = 1;
                content.interactable = true;
                content.blocksRaycasts = true;
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                yield break;
            }

            try
            {
                sequence.stopped += OnSequenceStopped;
                music.Volume = musicVolume;
                music.Play(task.Result, true);
                sequence.time = 0;
                sequence.Play();
                IsReady = true;
                Debug.Log("IntroScreen: Playing intro", this);
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
            }
        }

        public void Skip()
        {
            if (isClosing)
                return;
            isClosing = true;
            ScreenChanger.Open(ScreenId.Login);
        }

        void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Skip();
        }

        void OnSequenceStopped(PlayableDirector director)
        {
            if (isActiveAndEnabled)
                Skip();
        }

        void OnDestroy()
        {
            if (sequence != null)
                sequence.stopped -= OnSequenceStopped;
        }
    }
}
