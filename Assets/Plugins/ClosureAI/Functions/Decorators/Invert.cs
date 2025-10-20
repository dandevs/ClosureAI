#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that inverts the child node's status, converting Success to Failure and Failure to Success.
            /// This provides logical negation for behavior tree conditions and actions.
            /// </summary>
            /// <returns>A decorator node that inverts Success/Failure status</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Ticks the child node with allowReEnter=true</item>
            /// <item>Returns <see cref="Status.Failure"/> when the child returns Success</item>
            /// <item>Returns <see cref="Status.Success"/> when the child returns Failure</item>
            /// <item>Returns <see cref="Status.Running"/> while the child is running</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Logical NOT operations on conditions (e.g., "if NOT hasTarget")</item>
            /// <item>Inverting success/failure semantics of existing nodes</item>
            /// <item>Creating negative conditionals without writing inverse logic</item>
            /// <item>Triggering behaviors when something is absent rather than present</item>
            /// </list>
            ///
            /// <para><b>Example - Negative Condition:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     D.Invert();
            ///     Condition("Has Target", () => target != null);
            ///     // Now succeeds when target IS null (inverted)
            ///
            ///     Do("Search for Target", () => SearchForTarget());
            /// });
            /// // Only searches when we DON'T have a target
            /// </code>
            ///
            /// <para><b>Example - Fail-if-Success Pattern:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     Sequence("Avoid If Enemy Present", () =>
            ///     {
            ///         D.Invert();
            ///         Condition(() => enemyNearby);
            ///         // Succeeds when NO enemy nearby
            ///
            ///         MoveTo(() => targetPosition);
            ///     });
            ///
            ///     Sequence("Hide If Enemy Present", () =>
            ///     {
            ///         Condition(() => enemyNearby);
            ///         Hide();
            ///     });
            /// });
            /// // Moves to target if safe, hides if enemy nearby
            /// </code>
            ///
            /// <para><b>Example - Wait Until NOT Condition:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     StartAnimation();
            ///
            ///     D.Invert();
            ///     WaitUntil(() => animator.IsPlaying("MyAnimation"));
            ///     // Now succeeds when animation is NOT playing (i.e., finished)
            ///
            ///     OnAnimationComplete();
            /// });
            /// </code>
            ///
            /// <para><b>Comparison with Similar Decorators:</b></para>
            /// <list type="bullet">
            /// <item><b>Invert:</b> Success → Failure, Failure → Success (flips status)</item>
            /// <item><b>AlwaysSucceed:</b> Success → Success, Failure → Success (forces success)</item>
            /// <item><b>AlwaysFail:</b> Success → Failure, Failure → Failure (forces failure)</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Invert();
            /// Condition(() => someCondition);
            /// </code>
            /// </remarks>
            public static DecoratorNode Invert() => Decorator("Invert", () =>
            {
                var node = (DecoratorNode)CurrentNode;

                OnExit(_ => ExitNode(node.Child));
                OnInvalidCheck(() => node.Child.IsInvalid());

                OnBaseTick(() =>
                {
                   if (node.Child.Tick(out var status, true))
                        return status == Status.Success ? Status.Failure : Status.Success;
                   else
                       return Status.Running;
                });
            });
        }
    }
}

#endif