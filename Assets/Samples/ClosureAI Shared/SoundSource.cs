#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureBT.Samples
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
#endif
