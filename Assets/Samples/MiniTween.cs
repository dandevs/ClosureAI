using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureAI.Samples
{
    public static class MiniTween
    {
        public const Easing DEFAULT_EASING = Easing.OutQuad;

        public static float Ease(Easing easing, float elapsed, float duration)
        {
            var t = Mathf.Clamp01(elapsed / duration);

            return easing switch
            {
                Easing.Linear => t,
                Easing.InQuad => t * t,
                Easing.OutQuad => 1f - (1f - t) * (1f - t),
                Easing.InBounce => 1f - OutBounceCore(1f - t),
                Easing.OutBounce => OutBounceCore(t),
                Easing.InElastic => InElasticCore(t),
                Easing.OutElastic => OutElasticCore(t),
                _ => t
            };
        }

        //*********************************************************************************************************

        public enum Easing
        {
            Linear,
            InQuad, OutQuad,
            InBounce, OutBounce,
            InElastic, OutElastic,
        }

        //*********************************************************************************************************

        private static float OutBounceCore(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            return t switch
            {
                < 1f / d1 => n1 * t * t,
                < 2f / d1 => n1 * (t -= 1.5f / d1) * t + 0.75f,
                < 2.5f / d1 => n1 * (t -= 2.25f / d1) * t + 0.9375f,
                _ => n1 * (t -= 2.625f / d1) * t + 0.984375f
            };
        }

        private static float InElasticCore(float t)
        {
            return 1f - OutElasticCore(1f - t);
        }

        private static float OutElasticCore(float t)
        {
            const float c4 = 0.45f;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - c4 / 4f) * (2f * Mathf.PI) / c4) + 1;
        }

        //*********************************************************************************************************

        public static UniTask Ease(Transform transform, Vector3 end, float duration, CancellationToken ct = default, Easing easing = DEFAULT_EASING)
        {
            var p = (transform, start: transform.position, end);

            return Ease(p, duration, ct, static (p, t) =>
            {
                p.transform.position = Vector3.LerpUnclamped(p.start, p.end, t);
            },
            easing);
        }

        public static UniTask Ease(Transform start, Transform end, float duration, CancellationToken ct = default, Easing easing = DEFAULT_EASING)
        {
            var p = (transform: start, start: start.position, end);

            return Ease(p, duration, ct, static (p, t) =>
            {
                p.transform.position = Vector3.LerpUnclamped(p.start, p.end.position, t);
            },
            easing);
        }

        public static async UniTask Ease<T>(T binder, float duration, CancellationToken ct, Action<T, float> action, Easing easing = DEFAULT_EASING)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                await UniTask.NextFrame(ct);
                elapsed += Time.deltaTime;
                action(binder, Ease(easing, elapsed, duration));
            }
        }
    }
}
