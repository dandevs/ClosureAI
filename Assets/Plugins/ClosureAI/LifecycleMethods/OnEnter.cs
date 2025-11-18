#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        //********************************************************************************************
        // On Enter lifecycle methods

        /// <summary>
        /// Registers a synchronous callback to execute during the <see cref="SubStatus.Entering"/> phase.
        /// This is called when a node begins execution logic, and can be called multiple times if the node re-enters via reactive invalidation.
        /// Use this to reset per-execution state, log entry, or prepare for the node's main logic.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? <b>Entering</b> ? Running ? Success/Failure ? Exiting ? Done</para>
        /// <para><b>Called When:</b> After OnEnabled on first tick, OR when re-entering via allowReEnter (reactive invalidation)</para>
        /// <para><b>Re-entry:</b> Unlike OnEnabled, this IS called during re-entry (Done ? Entering)</para>
        /// <para><b>Pair With:</b> <see cref="OnExit"/> for cleanup at the end of each execution</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute when the node enters.</param>
        /// <example>
        /// <code>
        /// Leaf("MoveTo", () =>
        /// {
        ///     OnEnabled(() => Debug.Log("First activation"));
        ///     OnEnter(() =>
        ///     {
        ///         Debug.Log("Starting movement (called each time we enter)");
        ///         startTime = Time.time;
        ///     });
        ///     OnExit(() => Debug.Log("Movement complete"));
        /// });
        /// </code>
        /// </example>
        public static void OnEnter(Action action)
        {
            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Sync, action));
        }

        /// <summary>
        /// Registers an asynchronous callback to execute during the <see cref="SubStatus.Entering"/> phase.
        /// This is called when a node begins execution logic, supporting async operations like animations or delays before main execution.
        /// Can be called multiple times if the node re-enters via reactive invalidation.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? <b>Entering</b> ? Running ? Success/Failure ? Exiting ? Done</para>
        /// <para><b>Cancellation:</b> The CancellationToken is cancelled when the node is reset.</para>
        /// <para><b>Single-Tick Limitation:</b> This overload must complete within a single tick. For multi-tick operations, use the Tick Core overload.</para>
        /// </remarks>
        /// <param name="action">The asynchronous action to execute when entering. Receives a CancellationToken.</param>
        /// <example>
        /// <code>
        /// OnEnter(async ct =>
        /// {
        ///     await PlayEnterAnimationAsync(ct);
        ///     await PrepareSystemAsync(ct);
        /// });
        /// </code>
        /// </example>
        public static void OnEnter(Func<CancellationToken, UniTask> action)
        {
            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Async, action));
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Entering"/> phase.
        /// This overload allows multi-tick entry operations, useful for complex setup that needs to span multiple frames.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Multi-Tick Support:</b> Entry can span multiple frames, useful for phased initialization or gradual setup.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and a tick function for multi-tick entry operations.</param>
        /// <example>
        /// <code>
        /// OnEnter(async (ct, tick) =>
        /// {
        ///     StartEnterSequence();
        ///     await tick(); // Wait a tick
        ///     ContinueEnterSequence();
        ///     await tick(); // Wait another tick
        ///     FinalizeEntry();
        /// });
        /// </code>
        /// </example>
        public static void OnEnter(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            CurrentNode?.OnEnterActions.Add(
                (MethodLifecycleType.Async,
                CreateAsyncTickCore(CurrentNode, action)));
        }
    }
}


#endif
