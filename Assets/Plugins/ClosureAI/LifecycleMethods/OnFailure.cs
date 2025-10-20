#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureAI
{
    public static partial class AI
    {
        //********************************************************************************************
        // On Failure lifecycle methods

        /// <summary>
        /// Registers a synchronous callback to execute during the <see cref="SubStatus.Failing"/> phase.
        /// This is called when <see cref="OnBaseTick"/> returns <see cref="Status.Failure"/>, before <see cref="OnExit"/>.
        /// Use this to handle failure-specific logic, error logging, or recovery attempts.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? Running ? <b>Failing</b> ? Exiting ? Done</para>
        /// <para><b>Called When:</b> OnBaseTick returns Status.Failure</para>
        /// <para><b>Order:</b> OnFailure ? OnExit ? Done</para>
        /// <para><b>Note:</b> Not called if the node returns Success or Running</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute on failure.</param>
        /// <example>
        /// <code>
        /// Leaf("TryOpenDoor", () =>
        /// {
        ///     OnBaseTick(() => door.IsLocked ? Status.Failure : Status.Success);
        ///     OnSuccess(() => Debug.Log("Door opened"));
        ///     OnFailure(() => Debug.Log("Door is locked!"));
        /// });
        /// </code>
        /// </example>
        public static void OnFailure(Action action)
        {
            CurrentNode?.OnFailureActions.Add((MethodLifecycleType.Sync, action));
        }

        /// <summary>
        /// Registers an asynchronous callback to execute during the <see cref="SubStatus.Failing"/> phase.
        /// This is called when <see cref="OnBaseTick"/> returns <see cref="Status.Failure"/>, supporting async failure handling.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? Running ? <b>Failing</b> ? Exiting ? Done</para>
        /// <para><b>Cancellation:</b> The CancellationToken is cancelled when the node is reset.</para>
        /// <para><b>Single-Tick Limitation:</b> This overload must complete within a single tick. For multi-tick operations, use the Tick Core overload.</para>
        /// </remarks>
        /// <param name="action">The asynchronous action to execute on failure. Receives a CancellationToken.</param>
        /// <example>
        /// <code>
        /// OnFailure(async ct =>
        /// {
        ///     await LogFailureAsync(ct);
        ///     await NotifySystemAsync(ct);
        /// });
        /// </code>
        /// </example>
        public static void OnFailure(Func<CancellationToken, UniTask> action)
        {
            CurrentNode?.OnFailureActions.Add((MethodLifecycleType.Async, action));
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Failing"/> phase.
        /// This overload allows multi-tick failure handling operations.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Multi-Tick Support:</b> Failure handling can span multiple frames, useful for recovery animations or complex error handling.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and a tick function for multi-tick failure operations.</param>
        /// <example>
        /// <code>
        /// OnFailure(async (ct, tick) =>
        /// {
        ///     PlayFailureAnimation();
        ///     await tick(); // Wait a tick for animation
        ///     await tick(); // Wait another tick
        ///     AttemptRecovery();
        /// });
        /// </code>
        /// </example>
        public static void OnFailure(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            CurrentNode?.OnFailureActions.Add(
                (MethodLifecycleType.Async,
                CreateAsyncTickCore(CurrentNode, action)));
        }
    }
}


#endif