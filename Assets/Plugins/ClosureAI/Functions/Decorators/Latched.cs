#if UNITASK_INSTALLED
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that blocks invalidation signals from propagating upward from its child.
            /// This prevents parent reactive nodes from detecting changes in the child's conditions.
            /// </summary>
            /// <returns>A decorator node that blocks child invalidation from propagating to parents</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Ticks the child node and returns its status directly</item>
            /// <item>Returns <see cref="Status.Success"/> when the child succeeds</item>
            /// <item>Returns <see cref="Status.Failure"/> when the child fails</item>
            /// <item>Returns <see cref="Status.Running"/> while the child is running</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Does NOT implement OnInvalidCheck (blocks child invalidation from propagating up)</item>
            /// <item>WILL exit if earlier nodes in a reactive composite invalidate (doesn't prevent external invalidation)</item>
            /// </list>
            ///
            /// <para><b>Key Difference from Other Decorators:</b></para>
            /// Most decorators implement OnInvalidCheck to propagate child invalidation.
            /// Latched does NOT, which means:
            /// <list type="bullet">
            /// <item>The child's invalidation signals are blocked from reaching parent nodes</item>
            /// <item>Parent reactive nodes won't detect changes inside the Latched subtree</item>
            /// <item>However, Latched itself CAN still be exited if earlier siblings invalidate</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Preventing child condition changes from triggering parent re-evaluation</item>
            /// <item>Isolating volatile subtrees from reactive parents</item>
            /// <item>Reducing unnecessary invalidation checks for performance</item>
            /// <item>Debugging reactive behavior by selectively blocking invalidation propagation</item>
            /// </list>
            ///
            /// <para><b>Example - Block Child Invalidation:</b></para>
            /// <code>
            /// Reactive * Sequence(() =>
            /// {
            ///     Condition(() => enemyInRange); // If this invalidates, whole sequence exits
            ///
            ///     D.Latched();
            ///     Sequence("Attack Sequence", () =>
            ///     {
            ///         Condition(() => hasAmmo); // Changes to hasAmmo won't propagate past Latched
            ///         Attack();
            ///         // The parent Sequence won't see invalidation from inside this Latched subtree
            ///         // BUT if enemyInRange becomes false, Latched will still exit (earlier node invalidated)
            ///     });
            /// });
            /// </code>
            ///
            /// <para><b>Example - Isolate Volatile Conditions:</b></para>
            /// <code>
            /// Reactive * Selector(() =>
            /// {
            ///     Condition(() => hasHighPriorityTarget);
            ///
            ///     D.Latched();
            ///     Sequence("Task with Volatile Conditions", () =>
            ///     {
            ///         Condition(() => randomFluctuatingValue > 0.5f); // Won't trigger parent re-evaluation
            ///         PerformTask();
            ///         // Parent won't see invalidation from randomFluctuatingValue changes
            ///     });
            /// });
            /// </code>
            ///
            /// <para><b>Reactive Behavior Impact:</b></para>
            /// <list type="bullet">
            /// <item><b>Blocks upward:</b> Child invalidation does NOT propagate to parent</item>
            /// <item><b>Does NOT block downward:</b> Earlier sibling invalidation WILL still exit this node</item>
            /// <item>Use case: Prevent specific subtree conditions from triggering parent re-evaluation</item>
            /// <item>Not a "commitment" - external invalidation still works normally</item>
            /// </list>
            ///
            /// <para><b>Technical Note:</b></para>
            /// Unlike other decorators which implement:
            /// <code>OnInvalidCheck(() => node.Child.IsInvalid());</code>
            /// Latched has NO OnInvalidCheck, so IsInvalid() on this decorator will return false
            /// even if the child is invalid.
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Latched();
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode Latched() => Decorator("Latched", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                OnExit(_ => ExitNode(node.Child));
                OnBaseTick(() => node.Child.Tick(out var status) ? status : Status.Running);
            });
        }
    }
}

#endif