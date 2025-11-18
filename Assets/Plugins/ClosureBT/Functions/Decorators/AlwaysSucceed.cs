#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that always returns Success, regardless of the child node's actual status.
            /// The child node executes normally, but its Failure status is converted to Success.
            /// </summary>
            /// <returns>A decorator node that forces Success status</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Ticks the child node with allowReEnter=true</item>
            /// <item>Always returns <see cref="Status.Success"/> when the child completes, ignoring child's actual status</item>
            /// <item>Returns <see cref="Status.Running"/> while the child is running</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Optional tasks in sequences that shouldn't cause the sequence to fail</item>
            /// <item>"Best effort" behaviors where failure is acceptable</item>
            /// <item>Ensuring a sequence continues even if one step fails</item>
            /// <item>Fire-and-forget actions where the outcome doesn't matter</item>
            /// </list>
            ///
            /// <para><b>Example - Optional Task:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     Do("Required Action", () => PerformRequiredAction());
            ///
            ///     D.AlwaysSucceed();
            ///     Do("Optional Action", () => TryOptionalAction());
            ///     // Even if TryOptionalAction fails, the sequence continues
            ///
            ///     Do("Finish", () => CompleteSequence());
            /// });
            /// </code>
            ///
            /// <para><b>Example - Best Effort Cleanup:</b></para>
            /// <code>
            /// SequenceAlways(() =>
            /// {
            ///     D.AlwaysSucceed();
            ///     Do("Cleanup Resources", () => CleanupResources());
            ///     // Cleanup might fail but we don't care
            ///
            ///     D.AlwaysSucceed();
            ///     Do("Log Completion", () => LogCompletion());
            ///     // Logging might fail but we don't care
            ///
            ///     Do("Final Step", () => FinalStep());
            /// });
            /// </code>
            ///
            /// <para><b>Example - Guaranteed Sequence Success:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     TryPrimaryMethod();
            ///
            ///     D.AlwaysSucceed();
            ///     Sequence(() =>
            ///     {
            ///         // This fallback always "succeeds" even if actions fail
            ///         LogError();
            ///         UseDefaultBehavior();
            ///     });
            /// });
            /// // Selector is guaranteed to succeed because fallback always succeeds
            /// </code>
            ///
            /// <para><b>Difference from Invert:</b></para>
            /// <list type="bullet">
            /// <item>AlwaysSucceed always returns Success (Success → Success, Failure → Success)</item>
            /// <item>Invert flips the status (Success → Failure, Failure → Success)</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.AlwaysSucceed();
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode AlwaysSucceed() => Decorator("Always Succeed", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                OnExit(_ => ExitNode(node.Child));
                OnInvalidCheck(() => node.Child.IsInvalid());
                OnBaseTick(() => node.Child.Tick(out _, true) ? Status.Success : Status.Running);
            });
        }
    }
}

#endif
