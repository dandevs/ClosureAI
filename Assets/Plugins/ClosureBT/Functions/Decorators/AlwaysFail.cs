#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that always returns Failure, regardless of the child node's actual status.
            /// The child node executes normally, but its Success status is converted to Failure.
            /// </summary>
            /// <returns>A decorator node that forces Failure status</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Ticks the child node with allowReEnter=true</item>
            /// <item>Always returns <see cref="Status.Failure"/> when the child completes, ignoring child's actual status</item>
            /// <item>Returns <see cref="Status.Running"/> while the child is running</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Forcing failure in sequences for testing purposes</item>
            /// <item>Creating inverse success conditions (use child to test condition, but fail regardless)</item>
            /// <item>Debugging behavior trees by forcing specific paths</item>
            /// <item>Ensuring a selector continues trying alternatives even if one "succeeds"</item>
            /// </list>
            ///
            /// <para><b>Example - Testing Fallback Paths:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     D.AlwaysFail();
            ///     Sequence("Primary Method", () =>
            ///     {
            ///         // This will run but always fail, forcing selector to try next option
            ///         AttemptPrimaryMethod();
            ///     });
            ///
            ///     Sequence("Fallback Method", () =>
            ///     {
            ///         // This becomes the actual execution path
            ///         AttemptFallbackMethod();
            ///     });
            /// });
            /// </code>
            ///
            /// <para><b>Example - Conditional Execution without Success:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     D.AlwaysFail();
            ///     Do("Log Attempt", () => Debug.Log("Attempted action"));
            ///     // Logs the message but doesn't affect sequence (fails either way)
            ///
            ///     OtherAction(); // This won't run because AlwaysFail caused sequence to fail
            /// });
            /// </code>
            ///
            /// <para><b>Difference from Invert:</b></para>
            /// <list type="bullet">
            /// <item>AlwaysFail always returns Failure (Success → Failure, Failure → Failure)</item>
            /// <item>Invert flips the status (Success → Failure, Failure → Success)</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.AlwaysFail();
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode AlwaysFail() => Decorator("Always Failure", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                OnExit(_ => ExitNode(node.Child));
                OnInvalidCheck(() => node.Child.IsInvalid());
                OnBaseTick(() => node.Child.Tick(out _, true) ? Status.Failure : Status.Running);
            });
        }
    }
}

#endif
