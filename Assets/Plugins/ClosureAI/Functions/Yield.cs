#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public partial class BT
    {
        // We add a YieldNode type so we can query its type for editor functionality
        [Serializable]
        public class YieldNode : CompositeNode
        {
            public YieldNode() : base("Yield") {}
        }

        /// <summary>
        /// Creates a yield node that dynamically inserts a single child node and caches it for reuse.
        /// This is a simplified version of YieldDynamic for common use cases like recursion and single-node insertion.
        /// </summary>
        /// <param name="name">The name of the yield node for debugging and visualization</param>
        /// <param name="getNode">A function that returns the node to yield. Called once and the result is cached</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A yield node that inserts and manages a single cached child node</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Evaluates the getNode function once on first tick and caches the result</item>
        /// <item>Reuses the same cached node instance for all subsequent ticks</item>
        /// <item>Returns <see cref="Status.Running"/> while the yielded node is running</item>
        /// <item>Returns the yielded node's status (Success/Failure) when it completes</item>
        /// <item>Automatically resets the yielded node when switching or exiting (safe cleanup)</item>
        /// <item>Clears the cache when the yield node re-enters (fresh evaluation)</item>
        /// </list>
        ///
        /// <para><b>Default Configuration:</b></para>
        /// <list type="bullet">
        /// <item>NodeChangeResetPolicy: Reset (gracefully resets old node when switching)</item>
        /// <item>NodeExitResetPolicy: Reset (resets child when yield exits)</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Recursive behavior trees (e.g., AcquireItem calls itself for dependencies)</item>
        /// <item>Dynamic subtree selection based on parameters (e.g., different combat styles)</item>
        /// <item>Parameterized behaviors (e.g., MoveTo(target) where target changes)</item>
        /// <item>Lazy node creation (create expensive nodes only when needed)</item>
        /// </list>
        ///
        /// <para><b>Example - Recursive Item Acquisition:</b></para>
        /// <code>
        /// public Node AcquireItem(Func&lt;string&gt; getItemID) => Selector("Acquire", () =>
        /// {
        ///     var itemID = Variable(getItemID);
        ///
        ///     Sequence("Pick Up", () =>
        ///     {
        ///         Condition(() => FindItem(itemID.Value) != null);
        ///         Do(() => PickUp(itemID.Value));
        ///     });
        ///
        ///     Sequence("Craft", () =>
        ///     {
        ///         Condition(() => CanCraft(itemID.Value));
        ///         // Recursively acquire crafting materials
        ///         YieldSimpleCached(() => AcquireItem(() => GetRequiredMaterial(itemID.Value)));
        ///         Do(() => Craft(itemID.Value));
        ///     });
        /// });
        /// // This creates arbitrarily deep recursion based on crafting dependencies
        /// </code>
        ///
        /// <para><b>Example - Fixed Initial Setup:</b></para>
        /// <code>
        /// // Good: Initial setup that doesn't change
        /// YieldSimpleCached("Patrol Route", () => CreatePatrolRoute(currentLocation));
        ///
        /// // Bad: Don't use this if combatMode can change - it will lock to the first selected tree
        /// YieldSimpleCached("Combat Style", () =>
        /// {
        ///     return combatMode switch { /* ... */ };
        /// });
        /// // Use YieldDynamic instead if you need to switch based on changing conditions
        /// </code>
        ///
        /// <para><b>Technical Details:</b></para>
        /// Internally uses YieldDynamic with simplified configuration. The node caching pattern
        /// ensures the same node instance is used throughout execution, which is important for
        /// preserving node state (variables, lifecycle callbacks, etc.).
        ///
        /// <para><b>When NOT to use:</b></para>
        /// <list type="bullet">
        /// <item>If you need the yielded node to change dynamically, use <see cref="YieldDynamic"/> instead</item>
        /// <item>If you need fine-grained control over reset policies, use <see cref="YieldDynamic"/> directly</item>
        /// </list>
        /// </remarks>
        public static YieldNode YieldSimpleCached(string name, Func<Node> getNode, Action lifecycle = null)
        {
            return YieldDynamic(name, controller =>
            {
                controller
                    .WithResetYieldedNodeOnNodeChange()
                    .WithResetYieldedNodeOnSelfExit();

                Node node = null;
                lifecycle?.Invoke();
                return _ => node ??= getNode();
            });
        }

        /// <summary>
        /// Creates a yield node that dynamically inserts a single child node and caches it for reuse.
        /// Uses "Yield Simple" as the default name.
        /// </summary>
        /// <param name="getNode">A function that returns the node to yield. Called once and the result is cached</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A yield node that inserts and manages a single cached child node</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Yield Simple" as the default node name.
        /// See <see cref="YieldSimpleCached(string, Func{Node}, Action)"/> for detailed behavior description.
        /// </remarks>
        public static YieldNode YieldSimpleCached(Func<Node> getNode, Action lifecycle = null)
        {
            return YieldSimpleCached("Yield Simple", getNode, lifecycle);
        }
    }
}

#endif
