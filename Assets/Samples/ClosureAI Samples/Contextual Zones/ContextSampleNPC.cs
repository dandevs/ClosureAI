#if UNITASK_INSTALLED
using System;
using ClosureAI.Samples.Shared;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI.Samples.ContextAI
{
    [SelectionBase]
    public class ContextSampleNPC : MonoBehaviour
    {
        public Node AI;
        public Pawn Pawn;
        public Pawn Player;

        public Node CustomAI { get; set; }

        private void Awake() => AI = Reactive * SequenceAlways("Contextual NPC AI", () =>
        {
            D.ConditionLatch(() => !Node.IsInvalid(CustomAI));
            SequenceAlways("Use Custom Node", () =>
            {
                _ = Reactive * Sequence(() =>
                {
                    Condition(() => !Node.IsInvalid(CustomAI));
                    YieldDynamic(controller =>
                    {
                        controller
                            .WithResetYieldedNodeOnNodeChange()
                            .WithResetYieldedNodeOnSelfExit();

                        return _ => CustomAI;
                    });
                });

                Sequence(() =>
                {
                    WaitUntil("Looking At Player", () => Pawn.LookAtXZ(Player.transform.position));
                    Jump(); // Act surprised
                });
            });

            Pawn.MoveTo(() => (Player.transform.position, 2.5f, 3.5f));

            //-------------------------------------------------------------------------------------

            var playerSpinDuration = UsePipe(
                UseEveryTick(() => Player.transform.forward),
                v => UseThrottle(0.1f, v), // Sample every 0.1 seconds
                v => UseRollingBuffer(2, true, v),
                v => UseTimePredicateElapsed(v, elapsed =>
                {
                    var (prev, current) = (v.Value[0], v.Value[1]);
                    var angle = Vector3.Angle(prev, current);
                    return angle > 10f;
                }));

            D.Condition("Spin With Player", () => playerSpinDuration.Value >= 0.5f);
            Spin(() => float.MaxValue);

            //-------------------------------------------------------------------------------------

            Wait(0.1f);
            JustRunning();
        });

        public Node Spin(Func<float> getDuration, Action lifecycle = null) => Leaf("Spin!", () =>
        {
            var enterForward = Variable(() => Pawn.transform.forward);
            var startTime = Variable(() => Time.timeAsDouble);
            // AlwaysInvalidate();

            // OnExit(async (ct, tick) =>
            // {
            //     while (!Pawn.LookAtXZ(Pawn.transform.position + enterForward.Value))
            //         await tick();
            // });

            OnBaseTick(() => // Spin!
            {
                Pawn.transform.rotation *= Quaternion.Euler(0f, 720f * Time.deltaTime, 0f);
                return Time.timeAsDouble - startTime.Value < getDuration() ? Status.Running : Status.Success;
            });

            lifecycle?.Invoke();
        });

        public Node Jump(Func<float> getJumpHeight, Action lifecycle = null) => Leaf("Jump!", () =>
        {
            OnExit(async (ct, tick) =>
            {
                var elapsed = 0f;
                var originalLocalPos = Pawn.Model.transform.localPosition;

                // In the event we have a cancellation, we force return to its original position
                try
                {
                    var duration = Mathf.Sqrt(getJumpHeight() / 2f) * 0.75f;
                    var t = 0f;

                    while (t < 1f)
                    {
                        elapsed += Time.deltaTime;
                        t = Mathf.Clamp01(elapsed / duration);
                        Pawn.Model.transform.localPosition = originalLocalPos + Vector3.up * Mathf.Sin(t * Mathf.PI) * getJumpHeight();
                        await tick();
                    }
                }
                finally
                {
                    Pawn.Model.transform.localPosition = originalLocalPos;
                }
            });

            lifecycle?.Invoke();
        });

        public Node Jump() => Jump(static () => 0.6f);

        public Node JumpTo(Func<Vector3> getTargetPosition, Action lifecycle = null) => Leaf("Jump To", () =>
        {
            lifecycle?.Invoke();

            OnExit(async (ct, tick) =>
            {
                var startPosition = Pawn.transform.position;
                var targetPosition = getTargetPosition();
                var lookLocation = new Vector3(targetPosition.x, Pawn.transform.position.y, targetPosition.z);

                // Look at target first
                while (!Pawn.LookAtXZ(lookLocation))
                    await tick();

                var elapsed = 0f;
                var originalLocalPos = Pawn.Model.transform.localPosition;
                var duration = 1f;
                // var jumpHeight = 0.6f;
                var jumpHeight = Mathf.Abs(targetPosition.y - startPosition.y) + 0.7f;

                try
                {
                    var t = 0f;

                    while (t < 1f)
                    {
                        elapsed += Time.deltaTime;
                        t = Mathf.Clamp01(elapsed / duration);

                        // Linear interpolation for XZ position
                        var xzPosition = Vector3.Lerp(startPosition, targetPosition, t);
                        Pawn.transform.position = xzPosition;

                        // Sine wave for Y offset (jump arc)
                        Pawn.Model.transform.localPosition = originalLocalPos + Vector3.up * Mathf.Sin(t * Mathf.PI) * jumpHeight;
                        await tick();
                    }
                }
                finally
                {
                    // Ensure we end at the exact target position
                    Pawn.transform.position = targetPosition;
                    Pawn.Model.transform.localPosition = originalLocalPos;
                    Pawn.Agent.Warp(targetPosition);
                }
            });
        });

        private void Update() => AI.Tick();
        private void OnDestroy() => AI.ResetImmediately();
    }
}
#endif
