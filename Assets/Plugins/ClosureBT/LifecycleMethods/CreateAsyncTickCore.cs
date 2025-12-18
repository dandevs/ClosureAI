#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        //********************************************************************************************
        // CreateAsyncTickCore helper methods

        /// <summary>
        /// Creates an async wrapper that enables multi-tick async operations within lifecycle callbacks.
        /// This is an internal helper used by lifecycle methods that support the Tick Core pattern.
        /// The Tick Core pattern provides a <c>Func&lt;UniTask&gt;</c> that, when awaited, completes on the next tick.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> Allows async callbacks to span multiple ticks by awaiting the tick function</para>
        /// <para><b>How It Works:</b></para>
        /// <list type="number">
        /// <item>Creates a completion source (TCS) when tick() is called</item>
        /// <item>Stores the TCS with the current tick count</item>
        /// <item>On next tick, completes all TCS from previous ticks</item>
        /// <item>This allows the async operation to resume on the next frame</item>
        /// </list>
        /// <para><b>Cancellation:</b> All pending TCS are cancelled when the CancellationToken fires</para>
        /// <para><b>Cleanup:</b> All TCS are cleared after the callback completes or cancels</para>
        /// <para><b>Internal Use:</b> This is used by OnEnabled, OnDisabled, OnEnter, OnExit, OnSuccess, OnFailure with Tick Core overloads</para>
        /// </remarks>
        /// <param name="node">The node this async operation belongs to (for OnAnyTick handler registration).</param>
        /// <param name="action">The async action receiving a CancellationToken and tick function.</param>
        /// <returns>A function that executes the async action with tick support when called.</returns>
        internal static Func<CancellationToken, UniTask> CreateAsyncTickCore(Node node, Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            var tcsList = new List<(int tickCreated, AutoResetUniTaskCompletionSource tcs)>();
            var ticksElapsed = 0;

            Func<UniTask> createTCS = () =>
            {
                var tcs = AutoResetUniTaskCompletionSource.Create();
                tcsList.Add((ticksElapsed, tcs));
                return tcs.Task;
            };

            var cancelAllTCS = new Action(() =>
            {
                for (var i = 0; i < tcsList.Count; i++)
                    tcsList[i].tcs.TrySetCanceled();

                tcsList.Clear();
            });

            var onAnyTick = new Action<Node>(_ =>
            {
                for (var i = 0; i < tcsList.Count; i++)
                {
                    var (tick, tcs) = tcsList[0];

                    if (ticksElapsed != tick)
                    {
                        tcsList.RemoveAt(i--);
                        tcs.TrySetResult();
                    }
                }

                ticksElapsed++;
            });

            return async ct =>
            {
                ticksElapsed = 0;
                var ctRegistration = default(CancellationTokenRegistration);

                try
                {
                    node.OnAnyTicks.Add(onAnyTick);
                    ctRegistration = ct.RegisterWithoutCaptureExecutionContext(cancelAllTCS);
                    await action(ct, createTCS);
                }
                catch (Exception exception)
                {
                    throw new NodeException(node, exception);
                }
                finally
                {
                    node.OnAnyTicks.Remove(onAnyTick);
                    ctRegistration.Dispose();
                    cancelAllTCS();
                }
            };
        }

        /// <summary>
        /// Creates an async wrapper that enables multi-tick async operations within OnBaseTick callbacks.
        /// This is a specialized version for OnBaseTick that returns <see cref="Status"/> instead of void.
        /// The Tick Core pattern provides a <c>Func&lt;UniTask&gt;</c> that, when awaited, completes on the next tick.
        /// </summary>
        /// <remarks>
        /// <para><b>Tick Core Pattern:</b> Allows OnBaseTick to span multiple ticks while returning Status.Running between ticks</para>
        /// <para><b>How It Works:</b></para>
        /// <list type="number">
        /// <item>First call starts the async operation via Run() (fire-and-forget)</item>
        /// <item>While the operation is running, subsequent ticks return Status.Running</item>
        /// <item>When tick() is awaited, creates a TCS that completes on the next tick</item>
        /// <item>When the async operation completes with a final status, that status is returned</item>
        /// <item>The wrapper then resets for potential re-entry</item>
        /// </list>
        /// <para><b>State Machine:</b></para>
        /// <list type="bullet">
        /// <item><b>started = false:</b> First tick - starts async operation, returns Running</item>
        /// <item><b>started = true, status = Running:</b> Async operation in progress, returns Running</item>
        /// <item><b>started = true, status = Success/Failure:</b> Async completed, returns final status and resets</item>
        /// </list>
        /// <para><b>Cancellation:</b> All pending TCS are cancelled and state is reset when CancellationToken fires</para>
        /// <para><b>Internal Use:</b> This is used by OnBaseTick with the Tick Core overload</para>
        /// </remarks>
        /// <param name="node">The node this async operation belongs to (for cancellation token access).</param>
        /// <param name="action">The async action receiving a CancellationToken and tick function, returning Status.</param>
        /// <returns>A synchronous function that manages the async operation and returns Status each tick.</returns>
        /// <example>
        /// <code>
        /// // Example of how this is used internally by OnBaseTick:
        /// OnBaseTick(async (ct, tick) =>
        /// {
        ///     Debug.Log("Tick 1");
        ///     await tick(); // Returns Running this frame, continues next frame
        ///     Debug.Log("Tick 2");
        ///     await tick(); // Returns Running this frame, continues next frame
        ///     Debug.Log("Tick 3");
        ///     return Status.Success; // Returns Success this frame
        /// });
        /// </code>
        /// </example>
        internal static Func<Status> CreateAsyncTickCore(Node node, Func<CancellationToken, Func<UniTask>, UniTask<Status>> action)
        {
            var _completed = Variable(onEnter: static () => false);
            var started = false;
            var status = Status.Running;
            var tcsList = new List<(int tickCreated, AutoResetUniTaskCompletionSource tcs)>();
            var ticksElapsed = 0;
            var cancelled = false;
            var ctRegistration = default(CancellationTokenRegistration);
            var onAnyTick = new Action<Node>(static _ => {});

            Func<UniTask> createTCS = () =>
            {
                var tcs = AutoResetUniTaskCompletionSource.Create();
                tcsList.Add((ticksElapsed, tcs));
                return tcs.Task;
            };

            var cancelAllTCS = new Action(() =>
            {
                node.OnAnyTicks.Remove(onAnyTick);

                for (var i = 0; i < tcsList.Count; i++)
                    tcsList[i].tcs.TrySetCanceled();

                tcsList.Clear();
                cancelled = true;
                started = false;
                ticksElapsed = 0;
                ctRegistration.Dispose();
            });

            async UniTaskVoid Run()
            {
                try
                {
                    status = await action(node.GetCancellationToken(), createTCS);
                }
                catch (OperationCanceledException)
                {
                    if (!node.Resetting)
                        throw;
                }
                catch (Exception exception)
                {
                    throw new NodeException(node, exception);
                }
            }

            return () =>
            {
                if (_completed.Value)
                    return Status.Success;

                if (!started)
                {
                    started = true;
                    cancelled = false;
                    ticksElapsed = 0;
                    status = Status.Running;
                    node.OnAnyTicks.Add(onAnyTick);
                    ctRegistration = node.GetCancellationToken().RegisterWithoutCaptureExecutionContext(cancelAllTCS);
                    Run().Forget();
                }

                if (cancelled)
                {
                    Debug.LogWarning("This shouldn't ever run!");
                    return Status.Failure;
                }

                for (var i = 0; i < tcsList.Count; i++)
                {
                    var (tick, tcs) = tcsList[i];

                    if (ticksElapsed != tick)
                    {
                        tcsList.RemoveAt(i--);
                        tcs.TrySetResult();
                    }
                }

                if (status != Status.Running)
                {
                    started = false;
                    cancelled = false;
                    _completed.SetValueSilently(true);
                    ctRegistration.Dispose();
                    node.OnAnyTicks.Remove(onAnyTick);
                    return status;
                }

                ticksElapsed++;
                return Status.Running;
            };
        }
    }
}

#endif
