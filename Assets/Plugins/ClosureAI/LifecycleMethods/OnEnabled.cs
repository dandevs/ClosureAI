#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureAI
{
    public static partial class AI
    {
        //********************************************************************************************
        // On Enabled lifecycle methods

        /// <summary>
        /// Registers a synchronous callback to execute during the <see cref="SubStatus.Enabling"/> phase.
        /// This is called exactly once when a node first activates (transitions from <see cref="Status.None"/> to <see cref="Status.Running"/>).
        /// Use this to initialize state, subscribe to events, or allocate resources.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None → <b>Enabling</b> → Entering → Running → Success/Failure → Exiting → Done</para>
        /// <para><b>Called When:</b> First tick of a node (before OnEnter)</para>
        /// <para><b>NOT Called During:</b> Re-entry via allowReEnter (OnEnter is called instead)</para>
        /// <para><b>Pair With:</b> <see cref="OnDisabled"/> for cleanup (called only during reset operations)</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute when the node is enabled.</param>
        /// <example>
        /// <code>
        /// Leaf("MyNode", () =>
        /// {
        ///     OnEnabled(() => Debug.Log("Node activated for the first time"));
        ///     OnEnter(() => Debug.Log("Node entered (can be called multiple times)"));
        ///     OnDisabled(() => Debug.Log("Node deactivated (only on reset)"));
        /// });
        /// </code>
        /// </example>
        public static void OnEnabled(Action action)
        {
            CurrentNode?.OnEnabledActions.Add((MethodLifecycleType.Sync, action));
        }

        /// <summary>
        /// Registers an asynchronous callback to execute during the <see cref="SubStatus.Enabling"/> phase.
        /// This is called exactly once when a node first activates (transitions from <see cref="Status.None"/> to <see cref="Status.Running"/>).
        /// Use this for async initialization like loading assets, waiting for services, or async setup operations.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None → <b>Enabling</b> → Entering → Running → Success/Failure → Exiting → Done</para>
        /// <para><b>Cancellation:</b> The CancellationToken is cancelled when the node is reset.</para>
        /// <para><b>Single-Tick Limitation:</b> This overload must complete within a single tick. For multi-tick async operations, use the Tick Core overload.</para>
        /// </remarks>
        /// <param name="action">The asynchronous action to execute. Receives a CancellationToken that is cancelled on reset.</param>
        /// <example>
        /// <code>
        /// OnEnabled(async ct =>
        /// {
        ///     await LoadResourcesAsync(ct);
        ///     await InitializeSystemAsync(ct);
        /// });
        /// </code>
        /// </example>
        public static void OnEnabled(Func<CancellationToken, UniTask> action)
        {
            CurrentNode?.OnEnabledActions.Add((MethodLifecycleType.Async, action));
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Enabling"/> phase.
        /// This overload allows multi-tick async operations by providing a <c>Func&lt;UniTask&gt;</c> that waits for the next tick.
        /// Use this when your enabling logic needs to span multiple frames.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Multi-Tick Support:</b> Unlike the standard async overload, this can await multiple ticks during Enabling.</para>
        /// <para><b>Cancellation:</b> The CancellationToken is cancelled when the node is reset.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and a tick function. The tick function returns a UniTask that completes on the next tick.</param>
        /// <example>
        /// <code>
        /// OnEnabled(async (ct, tick) =>
        /// {
        ///     LoadResourcesAsync(); // Start loading
        ///     await tick(); // Wait for next tick
        ///     await tick(); // Wait another tick
        ///     await tick(); // Wait one more tick
        ///     FinalizeSetup(); // Complete setup
        /// });
        /// </code>
        /// </example>
        public static void OnEnabled(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            CurrentNode?.OnEnabledActions.Add(
                (MethodLifecycleType.Async,
                 CreateAsyncTickCore(CurrentNode, action)));
        }
    }
}

#endif