#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        //********************************************************************************************
        // On Success lifecycle methods

        /// <summary>
        /// Registers a synchronous callback to execute during the <see cref="SubStatus.Succeeding"/> phase.
        /// This is called when <see cref="OnBaseTick"/> returns <see cref="Status.Success"/>, before <see cref="OnExit"/>.
        /// Use this to handle success-specific logic, rewards, or state updates.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? Running ? <b>Succeeding</b> ? Exiting ? Done</para>
        /// <para><b>Called When:</b> OnBaseTick returns Status.Success</para>
        /// <para><b>Order:</b> OnSuccess ? OnExit ? Done</para>
        /// <para><b>Note:</b> Not called if the node returns Failure or Running</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute on success.</param>
        /// <example>
        /// <code>
        /// Leaf("CollectItem", () =>
        /// {
        ///     OnBaseTick(() => TryCollectItem() ? Status.Success : Status.Failure);
        ///     OnSuccess(() => Debug.Log("Item collected successfully!"));
        ///     OnFailure(() => Debug.Log("Failed to collect item"));
        /// });
        /// </code>
        /// </example>
        public static void OnSuccess(Action action)
        {
            CurrentNode?.OnSuccessActions.Add((MethodLifecycleType.Sync, action));
        }

        /// <summary>
        /// Registers an asynchronous callback to execute during the <see cref="SubStatus.Succeeding"/> phase.
        /// This is called when <see cref="OnBaseTick"/> returns <see cref="Status.Success"/>, supporting async success handling.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? Running ? <b>Succeeding</b> ? Exiting ? Done</para>
        /// <para><b>Cancellation:</b> The CancellationToken is cancelled when the node is reset.</para>
        /// <para><b>Single-Tick Limitation:</b> This overload must complete within a single tick. For multi-tick operations, use the Tick Core overload.</para>
        /// </remarks>
        /// <param name="action">The asynchronous action to execute on success. Receives a CancellationToken.</param>
        /// <example>
        /// <code>
        /// OnSuccess(async ct =>
        /// {
        ///     await SaveProgressAsync(ct);
        ///     await TriggerRewardAsync(ct);
        /// });
        /// </code>
        /// </example>
        public static void OnSuccess(Func<CancellationToken, UniTask> action)
        {
            CurrentNode?.OnSuccessActions.Add((MethodLifecycleType.Async, action));
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Succeeding"/> phase.
        /// This overload allows multi-tick success handling operations.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Multi-Tick Support:</b> Success handling can span multiple frames, useful for animations or complex state updates.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and a tick function for multi-tick success operations.</param>
        /// <example>
        /// <code>
        /// OnSuccess(async (ct, tick) =>
        /// {
        ///     PlaySuccessAnimation();
        ///     await tick(); // Wait a tick for animation
        ///     await tick(); // Wait another tick
        ///     FinalizeSuccess();
        /// });
        /// </code>
        /// </example>
        public static void OnSuccess(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            CurrentNode?.OnSuccessActions.Add(
                (MethodLifecycleType.Async,
                 CreateAsyncTickCore(CurrentNode, action)));
        }
    }
}


#endif
