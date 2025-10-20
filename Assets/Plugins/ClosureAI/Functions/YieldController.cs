#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public partial class AI
    {
        /// <summary>
        /// Delegate for yield setup functions that configure a YieldController and return a node selection function.
        /// </summary>
        /// <param name="controller">The YieldController to configure with policies and settings</param>
        /// <returns>A function that takes a YieldController and returns the node to yield each tick</returns>
        public delegate Func<YieldController, Node> YieldSetupFunc(YieldController controller);

        /// <summary>
        /// Configuration controller for YieldDynamic nodes, providing fine-grained control over
        /// node switching behavior, reset policies, completion handling, and timing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// YieldController is used to configure how a YieldDynamic node behaves when:
        /// <list type="bullet">
        /// <item>Switching between different child nodes</item>
        /// <item>A child node completes (succeeds or fails)</item>
        /// <item>The yield node itself exits</item>
        /// <item>State changes occur during execution</item>
        /// </list>
        /// </para>
        ///
        /// <para><b>Configuration Properties:</b></para>
        /// <list type="bullet">
        /// <item><b>NodeChangeResetPolicy:</b> Controls reset behavior when switching nodes (None or Reset)</item>
        /// <item><b>NodeExitResetPolicy:</b> Controls reset behavior when yield node exits (None or Reset)</item>
        /// <item><b>NodeCompletedPolicy:</b> Controls behavior when child completes (Return status or Reset and loop)</item>
        /// <item><b>ConsumeTickOnStateChange:</b> Controls whether state changes happen immediately or wait for next tick</item>
        /// </list>
        ///
        /// <para><b>Fluent API:</b></para>
        /// All configuration methods return the controller instance, allowing method chaining:
        /// <code>
        /// controller
        ///     .WithResetYieldedNodeOnNodeChange()
        ///     .WithResetYieldedNodeOnSelfExit()
        ///     .WithLooping()
        ///     .WithConsumeTickOnStateChange(false);
        /// </code>
        /// </remarks>
        public class YieldController
        {
            private readonly Func<Node> _getCurrentNode;

            /// <summary>
            /// Gets the currently yielded node (may be null).
            /// </summary>
            public Node CurrentNode => _getCurrentNode();

            private event Action<Node, Status> _onNodeCompleted = delegate {};

            /// <summary>
            /// Policy controlling what happens when switching between different nodes.
            /// None: Abrupt switch without resetting. Reset: Gracefully resets old node before switching.
            /// </summary>
            public YieldResetPolicy NodeChangeResetPolicy = YieldResetPolicy.None;

            /// <summary>
            /// Policy controlling what happens when the yield node exits.
            /// None: Doesn't reset yielded node. Reset: Gracefully resets yielded node.
            /// </summary>
            public YieldResetPolicy NodeExitResetPolicy = YieldResetPolicy.None;

            /// <summary>
            /// Policy controlling what happens when a yielded node completes.
            /// Return: Returns the child's status. Reset: Resets child and continues running (looping).
            /// </summary>
            public NodeCompletedPolicy NodeCompletedPolicy = NodeCompletedPolicy.Return;

            /// <summary>
            /// Controls whether state changes consume a tick or happen immediately.
            /// True: State changes wait until next tick. False: State changes happen immediately in same tick.
            /// </summary>
            public bool ConsumeTickOnStateChange = true;

            /// <summary>
            /// Initializes a new YieldController with a function to get the current node.
            /// </summary>
            /// <param name="getCurrentNode">Function that returns the currently yielded node</param>
            public YieldController(Func<Node> getCurrentNode)
            {
                _getCurrentNode = getCurrentNode;
            }

            /// <summary>
            /// Configures what happens when switching between different nodes.
            /// </summary>
            /// <param name="policy">The reset policy to apply when nodes change</param>
            /// <returns>This controller instance for method chaining</returns>
            /// <remarks>
            /// <para><b>YieldResetPolicy.None:</b></para>
            /// The old node is not reset when switching. This creates an abrupt transition and may
            /// leave the old node in an inconsistent state. Use when switching is rare or cleanup isn't needed.
            ///
            /// <para><b>YieldResetPolicy.Reset:</b></para>
            /// The old node is gracefully reset before switching to the new node. This ensures proper cleanup
            /// via OnExit and OnDisabled callbacks. Recommended for most use cases.
            /// </remarks>
            public YieldController OnNodeChange(YieldResetPolicy policy)
            {
                NodeChangeResetPolicy = policy;
                return this;
            }

            /// <summary>
            /// Configures what happens when the yield node exits.
            /// </summary>
            /// <param name="policy">The reset policy to apply when the yield exits</param>
            /// <returns>This controller instance for method chaining</returns>
            /// <remarks>
            /// <para><b>YieldResetPolicy.None:</b></para>
            /// The yielded node is not reset when the yield exits. The node remains in its current state.
            /// Use when you want to preserve node state across yield re-entries.
            ///
            /// <para><b>YieldResetPolicy.Reset:</b></para>
            /// The yielded node is gracefully reset when the yield exits. This ensures proper cleanup.
            /// Recommended for most use cases to prevent state leakage.
            /// </remarks>
            public YieldController OnSelfExit(YieldResetPolicy policy)
            {
                NodeExitResetPolicy = policy;
                return this;
            }

            /// <summary>
            /// Configures what happens when a child node completes (succeeds or fails).
            /// </summary>
            /// <param name="policy">The completion policy to apply</param>
            /// <returns>This controller instance for method chaining</returns>
            /// <remarks>
            /// <para><b>NodeCompletedPolicy.Return:</b></para>
            /// When the yielded node completes, return its status (Success or Failure) and the yield node completes.
            /// This is the default behavior and appropriate for most single-execution scenarios.
            ///
            /// <para><b>NodeCompletedPolicy.Reset:</b></para>
            /// When the yielded node completes, automatically reset it and continue running. This creates
            /// looping behavior where the node runs indefinitely. The yield node always returns Running.
            /// Use for patrol loops, idle animations, and other repeating behaviors.
            /// </remarks>
            public YieldController OnSelfCompleted(NodeCompletedPolicy policy)
            {
                NodeCompletedPolicy = policy;
                return this;
            }

            /// <summary>
            /// Fluent configuration method to enable/disable graceful reset of yielded nodes when switching.
            /// This is a convenience wrapper around OnNodeChange(YieldResetPolicy).
            /// </summary>
            /// <param name="enabled">True to reset nodes on change, false to switch abruptly</param>
            /// <returns>This controller instance for method chaining</returns>
            /// <remarks>
            /// <para><b>When enabled (default for YieldSimpleCached):</b></para>
            /// Old nodes are gracefully reset before switching to new nodes, ensuring proper cleanup.
            ///
            /// <para><b>When disabled:</b></para>
            /// Nodes switch immediately without reset. May leave old nodes in inconsistent state.
            /// </remarks>
            public YieldController WithResetYieldedNodeOnNodeChange(bool enabled = true)
            {
                NodeChangeResetPolicy = enabled ? YieldResetPolicy.Reset : YieldResetPolicy.None;
                return this;
            }

            /// <summary>
            /// Fluent configuration method to enable/disable graceful reset of yielded node when yield exits.
            /// This is a convenience wrapper around OnSelfExit(YieldResetPolicy).
            /// </summary>
            /// <param name="enabled">True to reset node on exit, false to leave it as-is</param>
            /// <returns>This controller instance for method chaining</returns>
            /// <remarks>
            /// <para><b>When enabled (default for YieldSimpleCached):</b></para>
            /// The yielded node is gracefully reset when the yield exits, ensuring proper cleanup.
            ///
            /// <para><b>When disabled:</b></para>
            /// The yielded node maintains its state when the yield exits.
            /// </remarks>
            public YieldController WithResetYieldedNodeOnSelfExit(bool enabled = true)
            {
                NodeExitResetPolicy = enabled ? YieldResetPolicy.Reset : YieldResetPolicy.None;
                return this;
            }

            /// <summary>
            /// Fluent configuration method to enable/disable looping behavior (auto-reset on completion).
            /// This is a convenience wrapper around OnSelfCompleted(NodeCompletedPolicy).
            /// </summary>
            /// <param name="enabled">True to loop (reset and continue), false to return status and complete</param>
            /// <returns>This controller instance for method chaining</returns>
            /// <remarks>
            /// <para><b>When enabled:</b></para>
            /// <list type="bullet">
            /// <item>When the yielded node completes, it is automatically reset and continues running</item>
            /// <item>The yield node always returns Running (never completes)</item>
            /// <item>Creates infinite loop behavior - useful for patrols, idle animations, etc.</item>
            /// </list>
            ///
            /// <para><b>When disabled (default):</b></para>
            /// <list type="bullet">
            /// <item>When the yielded node completes, return its status (Success/Failure)</item>
            /// <item>The yield node completes with the child's status</item>
            /// <item>Standard one-time execution behavior</item>
            /// </list>
            ///
            /// <para><b>Example:</b></para>
            /// <code>
            /// YieldDynamic(controller =>
            /// {
            ///     controller.WithLooping(); // Enable infinite looping
            ///     return _ => PatrolBehavior();
            /// });
            /// // PatrolBehavior runs, completes, resets, and runs again forever
            /// </code>
            /// </remarks>
            public YieldController WithLooping(bool enabled = true)
            {
                NodeCompletedPolicy = enabled ? NodeCompletedPolicy.Reset : NodeCompletedPolicy.Return;
                return this;
            }

            /// <summary>
            /// Fluent configuration method to control whether state changes consume a tick or happen immediately.
            /// </summary>
            /// <param name="enabled">True to consume tick on state change (wait until next tick), false for immediate changes</param>
            /// <returns>This controller instance for method chaining</returns>
            /// <remarks>
            /// <para><b>When enabled (true, default):</b></para>
            /// <list type="bullet">
            /// <item>State changes (node switches, resets, completions) wait until the next tick</item>
            /// <item>More predictable behavior - one state change per tick</item>
            /// <item>Prevents potential infinite loops within a single tick</item>
            /// <item>Recommended for most use cases</item>
            /// </list>
            ///
            /// <para><b>When disabled (false):</b></para>
            /// <list type="bullet">
            /// <item>State changes happen immediately within the same tick</item>
            /// <item>Multiple state changes can occur in a single tick</item>
            /// <item>More responsive but potentially less predictable</item>
            /// <item>Use when you need immediate reaction to state changes</item>
            /// </list>
            ///
            /// <para><b>Example - Immediate State Changes:</b></para>
            /// <code>
            /// YieldDynamic(controller =>
            /// {
            ///     controller.WithConsumeTickOnStateChange(false); // Immediate switching
            ///
            ///     return _ =>
            ///     {
            ///         if (criticalCondition) return EmergencyBehavior();
            ///         return NormalBehavior();
            ///     };
            /// });
            /// // Switches to EmergencyBehavior immediately when criticalCondition becomes true
            /// </code>
            /// </remarks>
            public YieldController WithConsumeTickOnStateChange(bool enabled = true)
            {
                ConsumeTickOnStateChange = enabled;
                return this;
            }
        }

        //*************************************************************************************************

        /// <summary>
        /// Defines policies for resetting yielded nodes in various scenarios.
        /// </summary>
        public enum YieldResetPolicy
        {
            /// <summary>
            /// Do not reset the node. The node maintains its current state.
            /// </summary>
            None,

            /// <summary>
            /// Gracefully reset the node, calling OnExit and OnDisabled callbacks for proper cleanup.
            /// </summary>
            Reset,
        }

        /// <summary>
        /// Defines policies for handling yielded node completion.
        /// </summary>
        public enum NodeCompletedPolicy
        {
            /// <summary>
            /// Return the child's status (Success or Failure) when it completes.
            /// The yield node completes with the same status.
            /// </summary>
            Return,

            /// <summary>
            /// Reset the child node and continue running when it completes.
            /// The yield node always returns Running, creating looping behavior.
            /// </summary>
            Reset,
        }
    }
}

#endif