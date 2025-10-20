using System;
using UnityEngine;

namespace ClosureAI.Samples
{
    public class SoundSource : MonoBehaviour
    {
        public static event Action<SoundSource> OnSoundBegan = delegate {};

        public AudioSource AudioSource;

        public void Play()
        {
            AudioSource.Play();
            OnSoundBegan(this);
        }
    }
}
