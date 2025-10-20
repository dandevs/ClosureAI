#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureAI
{
    public static partial class AI
    {
        //********************************************************************************************
        // OnEnterExitPair - Sync-Sync

        /// <summary>
        /// Registers a synchronized pair of OnEnter and OnExit callbacks with guaranteed execution semantics.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// This prevents cleanup from running when setup never happened, ensuring proper state management.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed (via entered flag)</para>
        /// <para><b>Use Case:</b> Acquire/release resource pairs, subscribe/unsubscribe, start/stop systems</para>
        /// <para><b>Thread Safety:</b> The entered flag ensures exit logic matches enter state</para>
        /// <para><b>Pattern:</b> Both callbacks are synchronous (Action)</para>
        /// </remarks>
        /// <param name="onEnter">Synchronous action executed during Entering phase.</param>
        /// <param name="onExit">Synchronous action executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// Leaf("UseResource", () =>
        /// {
        ///     OnEnterExitPair(
        ///         onEnter: () =>
        ///         {
        ///             resource = AcquireResource();
        ///             resource.Begin();
        ///         },
        ///         onExit: () =>
        ///         {
        ///             resource.End();
        ///             ReleaseResource(resource);
        ///         }
        ///     );
        /// });
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Action onEnter, Action onExit)
        {
            var entered = false;

            var enter = new Action(() =>
            {
                entered = true;
                onEnter();
            });

            var exit = new Action(() =>
            {
                if (!entered)
                    return;

                entered = false;
                onExit();
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Sync, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Sync, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Async-Async

        /// <summary>
        /// Registers a synchronized pair of async OnEnter and OnExit callbacks with guaranteed execution semantics.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// Both callbacks support async operations with CancellationToken support.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed (via entered flag)</para>
        /// <para><b>Cancellation:</b> Both callbacks receive CancellationToken for graceful cancellation</para>
        /// <para><b>Pattern:</b> Both callbacks are async (Func&lt;CancellationToken, UniTask&gt;)</para>
        /// <para><b>Single-Tick Limitation:</b> Each callback must complete within a single tick. For multi-tick, use Tick Core overloads.</para>
        /// </remarks>
        /// <param name="onEnter">Async action executed during Entering phase.</param>
        /// <param name="onExit">Async action executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: async ct =>
        ///     {
        ///         await LoadResourceAsync(ct);
        ///         await InitializeAsync(ct);
        ///     },
        ///     onExit: async ct =>
        ///     {
        ///         await ShutdownAsync(ct);
        ///         await UnloadResourceAsync(ct);
        ///     }
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Func<CancellationToken, UniTask> onEnter, Func<CancellationToken, UniTask> onExit)
        {
            var entered = false;

            var enter = new Func<CancellationToken, UniTask>(async ct =>
            {
                entered = true;
                await onEnter(ct);
            });

            var exit = new Func<CancellationToken, UniTask>(async ct =>
            {
                if (!entered)
                    return;

                entered = false;
                await onExit(ct);
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Async, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Async, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Async with Tick Core

        /// <summary>
        /// Registers a synchronized pair of async OnEnter and OnExit callbacks with Tick Core support.
        /// Both callbacks can span multiple ticks using the provided tick function.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed (via entered flag)</para>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick</para>
        /// <para><b>Multi-Tick Support:</b> Both enter and exit can span multiple frames</para>
        /// <para><b>Pattern:</b> Both callbacks use Tick Core (Func&lt;CancellationToken, Func&lt;UniTask&gt;, UniTask&gt;)</para>
        /// </remarks>
        /// <param name="onEnter">Async action with Tick Core executed during Entering phase.</param>
        /// <param name="onExit">Async action with Tick Core executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: async (ct, tick) =>
        ///     {
        ///         StartFadeIn();
        ///         await tick(); // Wait a tick
        ///         await tick(); // Wait another tick
        ///         CompleteFadeIn();
        ///     },
        ///     onExit: async (ct, tick) =>
        ///     {
        ///         StartFadeOut();
        ///         await tick();
        ///         await tick();
        ///         CompleteFadeOut();
        ///     }
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(
            Func<CancellationToken, Func<UniTask>, UniTask> onEnter,
            Func<CancellationToken, Func<UniTask>, UniTask> onExit)
        {
            var entered = false;
            var enterTickCore = CreateAsyncTickCore(CurrentNode, onEnter);
            var exitTickCore = CreateAsyncTickCore(CurrentNode, onExit);

            var enter = new Func<CancellationToken, UniTask>(async ct =>
            {
                entered = true;
                await enterTickCore(ct);
            });

            var exit = new Func<CancellationToken, UniTask>(async ct =>
            {
                if (!entered)
                    return;

                entered = false;
                await exitTickCore(ct);
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Async, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Async, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Async-Sync

        /// <summary>
        /// Registers a synchronized pair of OnEnter (async) and OnExit (sync) callbacks.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// This mixed pattern is useful when enter requires async operations but exit can be synchronous.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed</para>
        /// <para><b>Pattern:</b> OnEnter is async, OnExit is synchronous</para>
        /// <para><b>Use Case:</b> Async loading/setup with synchronous cleanup</para>
        /// </remarks>
        /// <param name="onEnter">Async action executed during Entering phase.</param>
        /// <param name="onExit">Synchronous action executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: async ct =>
        ///     {
        ///         resource = await LoadResourceAsync(ct);
        ///         await resource.InitializeAsync(ct);
        ///     },
        ///     onExit: () => resource.Dispose() // Simple synchronous cleanup
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Func<CancellationToken, UniTask> onEnter, Action onExit)
        {
            var entered = false;

            var enter = new Func<CancellationToken, UniTask>(async ct =>
            {
                entered = true;
                await onEnter(ct);
            });

            var exit = new Action(() =>
            {
                if (!entered)
                    return;

                entered = false;
                onExit();
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Async, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Sync, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Sync-Async

        /// <summary>
        /// Registers a synchronized pair of OnEnter (sync) and OnExit (async) callbacks.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// This mixed pattern is useful when enter is synchronous but exit requires async cleanup.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed</para>
        /// <para><b>Pattern:</b> OnEnter is synchronous, OnExit is async</para>
        /// <para><b>Use Case:</b> Quick synchronous setup with async cleanup/saving</para>
        /// </remarks>
        /// <param name="onEnter">Synchronous action executed during Entering phase.</param>
        /// <param name="onExit">Async action executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: () => state = CreateState(), // Simple synchronous setup
        ///     onExit: async ct =>
        ///     {
        ///         await SaveStateAsync(state, ct);
        ///         await CleanupAsync(ct);
        ///     }
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Action onEnter, Func<CancellationToken, UniTask> onExit)
        {
            var entered = false;

            var enter = new Action(() =>
            {
                entered = true;
                onEnter();
            });

            var exit = new Func<CancellationToken, UniTask>(async ct =>
            {
                if (!entered)
                    return;

                entered = false;
                await onExit(ct);
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Sync, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Async, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Async with Tick Core (onEnter) and Sync (onExit)

        /// <summary>
        /// Registers a synchronized pair of OnEnter (async with Tick Core) and OnExit (sync) callbacks.
        /// The enter callback can span multiple ticks while exit is synchronous.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed</para>
        /// <para><b>Pattern:</b> OnEnter uses Tick Core, OnExit is synchronous</para>
        /// <para><b>Use Case:</b> Multi-tick setup/initialization with simple synchronous cleanup</para>
        /// </remarks>
        /// <param name="onEnter">Async action with Tick Core executed during Entering phase.</param>
        /// <param name="onExit">Synchronous action executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: async (ct, tick) =>
        ///     {
        ///         // Multi-frame fade-in
        ///         for (int i = 0; i &lt; 10; i++)
        ///         {
        ///             alpha += 0.1f;
        ///             await tick();
        ///         }
        ///     },
        ///     onExit: () => alpha = 0f // Instant reset
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Func<CancellationToken, Func<UniTask>, UniTask> onEnter, Action onExit)
        {
            var entered = false;
            var enterTickCore = CreateAsyncTickCore(CurrentNode, onEnter);

            var enter = new Func<CancellationToken, UniTask>(async ct =>
            {
                entered = true;
                await enterTickCore(ct);
            });

            var exit = new Action(() =>
            {
                if (!entered)
                    return;

                entered = false;
                onExit();
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Async, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Sync, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Sync (onEnter) and Async with Tick Core (onExit)

        /// <summary>
        /// Registers a synchronized pair of OnEnter (sync) and OnExit (async with Tick Core) callbacks.
        /// The enter callback is synchronous while exit can span multiple ticks.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed</para>
        /// <para><b>Pattern:</b> OnEnter is synchronous, OnExit uses Tick Core</para>
        /// <para><b>Use Case:</b> Instant setup with gradual/animated cleanup</para>
        /// </remarks>
        /// <param name="onEnter">Synchronous action executed during Entering phase.</param>
        /// <param name="onExit">Async action with Tick Core executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: () => effect.Activate(), // Instant activation
        ///     onExit: async (ct, tick) =>
        ///     {
        ///         // Gradual fade-out over multiple frames
        ///         for (int i = 0; i &lt; 10; i++)
        ///         {
        ///             effect.intensity -= 0.1f;
        ///             await tick();
        ///         }
        ///         effect.Deactivate();
        ///     }
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Action onEnter, Func<CancellationToken, Func<UniTask>, UniTask> onExit)
        {
            var entered = false;
            var exitTickCore = CreateAsyncTickCore(CurrentNode, onExit);

            var enter = new Action(() =>
            {
                entered = true;
                onEnter();
            });

            var exit = new Func<CancellationToken, UniTask>(async ct =>
            {
                if (!entered)
                    return;

                entered = false;
                await exitTickCore(ct);
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Sync, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Async, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Async with Tick Core (onEnter) and Async (onExit)

        /// <summary>
        /// Registers a synchronized pair of OnEnter (async with Tick Core) and OnExit (async) callbacks.
        /// The enter callback can span multiple ticks while exit is single-tick async.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed</para>
        /// <para><b>Pattern:</b> OnEnter uses Tick Core, OnExit is single-tick async</para>
        /// <para><b>Use Case:</b> Multi-frame setup with single-frame async cleanup</para>
        /// </remarks>
        /// <param name="onEnter">Async action with Tick Core executed during Entering phase.</param>
        /// <param name="onExit">Async action executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: async (ct, tick) =>
        ///     {
        ///         // Multi-frame initialization
        ///         await LoadPartialDataAsync(ct);
        ///         await tick();
        ///         await LoadRestOfDataAsync(ct);
        ///     },
        ///     onExit: async ct => await SaveAndUnloadAsync(ct) // Single-frame cleanup
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Func<CancellationToken, Func<UniTask>, UniTask> onEnter, Func<CancellationToken, UniTask> onExit)
        {
            var entered = false;
            var enterTickCore = CreateAsyncTickCore(CurrentNode, onEnter);

            var enter = new Func<CancellationToken, UniTask>(async ct =>
            {
                entered = true;
                await enterTickCore(ct);
            });

            var exit = new Func<CancellationToken, UniTask>(async ct =>
            {
                if (!entered)
                    return;

                entered = false;
                await onExit(ct);
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Async, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Async, exit));
        }

        //********************************************************************************************
        // OnEnterExitPair - Async (onEnter) and Async with Tick Core (onExit)

        /// <summary>
        /// Registers a synchronized pair of OnEnter (async) and OnExit (async with Tick Core) callbacks.
        /// The enter callback is single-tick async while exit can span multiple ticks.
        /// The exit callback is only invoked if the enter callback successfully executed.
        /// </summary>
        /// <remarks>
        /// <para><b>Guarantee:</b> OnExit only runs if OnEnter completed</para>
        /// <para><b>Pattern:</b> OnEnter is single-tick async, OnExit uses Tick Core</para>
        /// <para><b>Use Case:</b> Quick async setup with gradual multi-frame cleanup</para>
        /// </remarks>
        /// <param name="onEnter">Async action executed during Entering phase.</param>
        /// <param name="onExit">Async action with Tick Core executed during Exiting phase, only if onEnter succeeded.</param>
        /// <example>
        /// <code>
        /// OnEnterExitPair(
        ///     onEnter: async ct => await QuickSetupAsync(ct), // Single-frame setup
        ///     onExit: async (ct, tick) =>
        ///     {
        ///         // Gradual multi-frame cleanup
        ///         await StartCleanupAsync(ct);
        ///         await tick();
        ///         await ContinueCleanupAsync(ct);
        ///         await tick();
        ///         await FinalizeCleanupAsync(ct);
        ///     }
        /// );
        /// </code>
        /// </example>
        public static void OnEnterExitPair(Func<CancellationToken, UniTask> onEnter, Func<CancellationToken, Func<UniTask>, UniTask> onExit)
        {
            var entered = false;
            var asyncTickCore = CreateAsyncTickCore(CurrentNode, onExit);

            var enter = new Func<CancellationToken, UniTask>(async ct =>
            {
                entered = true;
                await onEnter(ct);
            });

            var exit = new Func<CancellationToken, UniTask>(async ct =>
            {
                if (!entered)
                    return;

                entered = false;
                await asyncTickCore(ct);
            });

            CurrentNode?.OnEnterActions.Add((MethodLifecycleType.Async, enter));
            CurrentNode?.OnExitActions.Add((MethodLifecycleType.Async, exit));
        }
    }
}

#endif