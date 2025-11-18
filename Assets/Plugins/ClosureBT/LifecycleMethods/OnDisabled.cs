#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        //********************************************************************************************
        // On Disabled lifecycle methods

        /// <summary>
        /// Registers a synchronous callback to execute during the <see cref="SubStatus.Disabling"/> phase.
        /// <b>CRITICAL:</b> This is NOT called during normal tick flow - only during reset operations (<see cref="Node.ResetImmediately"/> or <see cref="Node.ResetGracefully"/>).
        /// Use this to clean up resources, unsubscribe from events, or deallocate state initialized in <see cref="OnEnabled"/>.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> [Any State] → <b>Disabling</b> → None (only during reset)</para>
        /// <para><b>Called When:</b> ResetImmediately() or ResetGracefully() is invoked</para>
        /// <para><b>NOT Called When:</b> Node completes normally (Success/Failure → Done)</para>
        /// <para><b>Pair With:</b> <see cref="OnEnabled"/> for initialization</para>
        /// <para><b>Important:</b> OnDisabled is the EXCEPTION to normal lifecycle - it's reset-only!</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute when the node is disabled during reset.</param>
        /// <example>
        /// <code>
        /// Leaf("MyNode", () =>
        /// {
        ///     OnEnabled(() =>
        ///     {
        ///         eventBus.Subscribe(OnEvent);
        ///         resource = AllocateResource();
        ///     });
        ///
        ///     OnDisabled(() =>
        ///     {
        ///         eventBus.Unsubscribe(OnEvent);
        ///         resource.Dispose();
        ///     });
        /// });
        /// </code>
        /// </example>
        public static void OnDisabled(Action action)
        {
            CurrentNode?.OnDisabledActions.Add((MethodLifecycleType.Sync, action));
        }

        /// <summary>
        /// Registers an asynchronous callback to execute during the <see cref="SubStatus.Disabling"/> phase.
        /// <b>CRITICAL:</b> This is NOT called during normal tick flow - only during reset operations.
        /// Use this for async cleanup like saving state, releasing async resources, or graceful shutdown.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> [Any State] → <b>Disabling</b> → None (only during reset)</para>
        /// <para><b>Cancellation:</b> The CancellationToken is cancelled on force-reset. During ResetGracefully, it remains active to allow cleanup to complete.</para>
        /// <para><b>Single-Tick Limitation:</b> This overload must complete within a single tick. For multi-tick cleanup, use the Tick Core overload.</para>
        /// </remarks>
        /// <param name="action">The asynchronous action to execute during reset. Receives a CancellationToken.</param>
        /// <example>
        /// <code>
        /// OnDisabled(async ct =>
        /// {
        ///     await SaveStateAsync(ct);
        ///     await ReleaseResourcesAsync(ct);
        /// });
        /// </code>
        /// </example>
        public static void OnDisabled(Func<CancellationToken, UniTask> action)
        {
            CurrentNode?.OnDisabledActions.Add((MethodLifecycleType.Async, action));
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Disabling"/> phase.
        /// This overload allows multi-tick cleanup operations during reset.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Multi-Tick Support:</b> Cleanup can span multiple frames, useful for gradual resource release or phased shutdown.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and a tick function for multi-tick cleanup operations.</param>
        /// <example>
        /// <code>
        /// OnDisabled(async (ct, tick) =>
        /// {
        ///     StartGracefulShutdown();
        ///     await tick(); // Wait a tick
        ///     await tick(); // Wait another tick
        ///     FinalizeCleanup();
        /// });
        /// </code>
        /// </example>
        public static void OnDisabled(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            CurrentNode?.OnDisabledActions.Add(
                (MethodLifecycleType.Async,
                 CreateAsyncTickCore(CurrentNode, action)));
        }
    }
}

#endif
