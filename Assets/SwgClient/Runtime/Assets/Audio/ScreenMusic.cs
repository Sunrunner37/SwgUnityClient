using System;
using UnityEngine;

namespace Swg.Client.Runtime.Assets.Audio
{
    /// <summary>Optional scene music; asset loading does not block the screen's UI.</summary>
    public sealed class ScreenMusic : MonoBehaviour
    {
        [Range(0, 1)]
        public float volume = .65f;
        public string musicPath;
        public bool loop = true;
        void Start()
        {
            try
            {
                var music = GameServices.Resolve<ClientMusic>();
                music.Volume = volume;
                music.Request(musicPath, loop);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Screen music unavailable: " + error.Message, this);
            }
        }
    }
}
