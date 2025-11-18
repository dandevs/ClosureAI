#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        //********************************************************************************************
        // On Exit lifecycle methods

        /// <summary>
        /// Registers a synchronous callback to execute during the <see cref="SubStatus.Exiting"/> phase.
        /// This is called after <see cref="OnSuccess"/> or <see cref="OnFailure"/> callbacks, before the node transitions to <see cref="SubStatus.Done"/>.
        /// Use this to clean up per-execution state, log exit, or finalize the node's work.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? Running ? Success/Failure ? <b>Exiting</b> ? Done</para>
        /// <para><b>Called When:</b> After OnSuccess/OnFailure, before transitioning to Done</para>
        /// <para><b>Pair With:</b> <see cref="OnEnter"/> for setup at the beginning of each execution</para>
        /// <para><b>Note:</b> This is NOT the same as OnDisabled (which only fires during reset)</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute when the node exits.</param>
        /// <example>
        /// <code>
        /// Leaf("Attack", () =>
        /// {
        ///     OnEnter(() => weapon.BeginAttack());
        ///     OnBaseTick(() => weapon.IsAttackComplete() ? Status.Success : Status.Running);
        ///     OnExit(() => weapon.EndAttack());
        /// });
        /// </code>
        /// </example>
        public static void OnExit(Action action)
        {
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Sync, action));
        }

        /// <summary>
        /// Registers an asynchronous callback to execute during the <see cref="SubStatus.Exiting"/> phase.
        /// This is called after success/failure handling, supporting async cleanup operations.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? Running ? Success/Failure ? <b>Exiting</b> ? Done</para>
        /// <para><b>Cancellation:</b> The CancellationToken is cancelled when the node is reset during exit.</para>
        /// <para><b>Single-Tick Limitation:</b> This overload must complete within a single tick. For multi-tick cleanup, use the Tick Core overload.</para>
        /// </remarks>
        /// <param name="action">The asynchronous action to execute when exiting. Receives a CancellationToken.</param>
        /// <example>
        /// <code>
        /// OnExit(async ct =>
        /// {
        ///     await PlayExitAnimationAsync(ct);
        ///     await CleanupResourcesAsync(ct);
        /// });
        /// </code>
        /// </example>
        public static void OnExit(Func<CancellationToken, UniTask> action)
        {
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Async, action));
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Exiting"/> phase.
        /// This overload allows multi-tick exit operations for gradual or phased cleanup.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Multi-Tick Support:</b> Exit can span multiple frames, useful for animations or gradual resource release.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and a tick function for multi-tick exit operations.</param>
        /// <example>
        /// <code>
        /// OnExit(async (ct, tick) =>
        /// {
        ///     BeginExitSequence();
        ///     await tick(); // Wait a tick
        ///     ContinueExitSequence();
        ///     await tick(); // Wait another tick
        ///     FinalizeExit();
        /// });
        /// </code>
        /// </example>
        public static void OnExit(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            CurrentNode?.OnExitActions.Add(
                (MethodLifecycleType.Async,
                 CreateAsyncTickCore(CurrentNode, action)));
        }
    }
}


#endif
