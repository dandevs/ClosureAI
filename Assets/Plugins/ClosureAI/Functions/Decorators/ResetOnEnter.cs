#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that resets its child node on both entry and exit, ensuring the child always starts fresh.
            /// This forces the child to begin from its initial state rather than resuming from where it left off.
            /// </summary>
            /// <returns>A decorator node that resets its child on entry and exit</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Calls ResetNode on the child during OnEnter (forces fresh start)</item>
            /// <item>Calls ResetNode on the child during OnExit (cleans up state)</item>
            /// <item>If child is Done, attempts ResetGracefully() before ticking</item>
            /// <item>Returns <see cref="Status.Running"/> for one tick if ResetGracefully() fails, then ticks child next frame</item>
            /// <item>Returns child's status when child completes (Success or Failure)</item>
            /// <item>Returns <see cref="Status.Running"/> while child is running</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Forcing fresh initialization of subtrees that maintain state</item>
            /// <item>Resetting timers, counters, or variables in child nodes</item>
            /// <item>Ensuring consistent behavior by clearing previous execution state</item>
            /// <item>Creating repeatable behaviors that always start from scratch</item>
            /// <item>Cleaning up child state on both entry and exit</item>
            /// <item>Debugging by forcing clean state transitions</item>
            /// </list>
            ///
            /// <para><b>Example - Reset Loop Counter:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     Condition(() => skipRepeats);
            ///
            ///     D.Reset();
            ///     D.RepeatCount(3);
            ///     DoTask();
            ///     // RepeatCount counter resets on entry, always repeats exactly 3 times
            ///     // Counter is also reset on exit, ready for next execution
            /// });
            /// </code>
            ///
            /// <para><b>Example - Fresh Timer Each Time:</b></para>
            /// <code>
            /// D.Reset();
            /// Sequence(() =>
            /// {
            ///     var elapsed = Variable(() => 0f);
            ///     OnTick(() => elapsed.Value += Time.deltaTime);
            ///
            ///     Wait(5f);
            ///     Do(() => PerformAction());
            ///     // elapsed variable resets to 0 each time this sequence is entered
            /// });
            /// </code>
            ///
            /// <para><b>Example - Repeating with Fresh State:</b></para>
            /// <code>
            /// D.Repeat();
            /// D.Reset();
            /// Sequence(() =>
            /// {
            ///     // This sequence starts completely fresh on each repeat iteration
            ///     InitializeState();
            ///     ExecuteTask();
            ///     CleanupState();
            /// });
            /// </code>
            ///
            /// <para><b>When Reset Occurs:</b></para>
            /// <list type="bullet">
            /// <item><b>OnEnter:</b> Resets child before first tick (fresh start)</item>
            /// <item><b>OnExit:</b> Resets child when decorator exits (cleanup)</item>
            /// <item><b>During tick (if child.Done):</b> Attempts ResetGracefully() before ticking</item>
            /// <item>Reset triggers full disable/enable cycle on the child</item>
            /// <item>All child variables are re-initialized to their initial values</item>
            /// </list>
            ///
            /// <para><b>Graceful Reset Logic:</b></para>
            /// OnBaseTick includes special handling for completed children:
            /// <code>
            /// if (node.Child.Done && !node.Child.ResetGracefully())
            ///     return Status.Running; // Wait one tick for reset to complete
            /// </code>
            /// This ensures the child has time to fully reset before being ticked again,
            /// preventing issues with nodes that need cleanup time.
            ///
            /// <para><b>Difference from Re-entry:</b></para>
            /// <list type="bullet">
            /// <item><b>Re-entry (allowReEnter=true):</b> Skips OnEnabled, just calls OnEnter. Variables keep values.</item>
            /// <item><b>Reset decorator:</b> Full reset cycle. Calls OnDisabled then OnEnabled. Variables re-initialize.</item>
            /// <item><b>Reset on entry AND exit:</b> Ensures clean state transitions in both directions</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Reset();
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode ResetOnEnter() => Decorator("Reset", () =>
            {
                var node = (DecoratorNode)CurrentNode;

                OnInvalidCheck(() => node.Child.IsInvalid());
                OnExit(_ => ResetNode(node.Child));
                OnEnter(_ => ResetNode(node.Child));

                OnBaseTick(() =>
                {
                    if (node.Child.Done && !node.Child.ResetGracefully())
                        return Status.Running;

                    return node.Child.Tick(out var status) ? status : Status.Running;
                });
            });
        }
    }
}

#endif
