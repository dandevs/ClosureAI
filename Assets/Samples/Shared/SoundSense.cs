#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureBT.Samples
{
    public class SoundSense : MonoBehaviour
    {
        public float Range = 12f;

        public SoundSource LastHeardSoundSource { get; private set; }
        public float SoundHeardStrength { get; private set; }

        private void OnEnable() => SoundSource.OnSoundBegan += SoundSourceListener;
        private void OnDisable() => SoundSource.OnSoundBegan -= SoundSourceListener;

        public event Action<SoundSense> OnSoundHeard = delegate {};

        private void SoundHeard(SoundSource soundSource)
        {
            var strength = Mathf.Clamp01(1f - Vector3.Distance(transform.position, soundSource.transform.position) / Range);
            strength = strength * soundSource.AudioSource.volume;
            LastHeardSoundSource = soundSource;
            SoundHeardStrength = strength;

            OnSoundHeard(this);
        }

        private void SoundSourceListener(SoundSource soundSource)
        {
            var distance = Vector3.Distance(transform.position, soundSource.transform.position);

            if (distance <= Range)
                SoundHeard(soundSource);
        }
    }
}
#endif
