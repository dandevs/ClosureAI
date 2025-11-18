#if UNITASK_INSTALLED
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureBT
{
    public partial class BT
    {
        /// <summary>
        /// Creates a yield node that dynamically inserts and switches between child nodes at runtime.
        /// This is the most advanced yield variant, providing full control over node switching, reset policies, and looping behavior.
        /// </summary>
        /// <param name="name">The name of the yield node for debugging and visualization</param>
        /// <param name="setup">A function that receives a YieldController and returns a function that returns the current node to yield</param>
        /// <returns>A yield node with fully configurable dynamic behavior</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Calls the getNode function (returned from setup) EVERY TICK to determine which node should run</item>
        /// <item>Automatically handles switching between different nodes based on getNode return value</item>
        /// <item>Manages node reset behavior according to configured policies</item>
        /// <item>Supports looping (auto-reset when child completes) or returning child status</item>
        /// <item>Uses a state machine internally to handle complex transitions gracefully</item>
        /// </list>
        ///
        /// <para><b>YieldController Configuration (via setup function):</b></para>
        /// <list type="bullet">
        /// <item><b>NodeChangeResetPolicy:</b> Controls what happens when switching between different nodes
        ///   <list type="bullet">
        ///     <item>None: Abrupt switch (old node not reset)</item>
        ///     <item>Reset: Gracefully resets old node before switching (default for YieldSimpleCached)</item>
        ///   </list>
        /// </item>
        /// <item><b>NodeExitResetPolicy:</b> Controls what happens when the yield node exits
        ///   <list type="bullet">
        ///     <item>None: Yielded node is not reset</item>
        ///     <item>Reset: Yielded node is gracefully reset (default for YieldSimpleCached)</item>
        ///   </list>
        /// </item>
        /// <item><b>ConsumeTickOnStateChange:</b> Controls whether state changes happen immediately or wait for next tick
        ///   <list type="bullet">
        ///     <item>true: State changes consume a tick (wait until next tick to switch) (default)</item>
        ///     <item>false: State changes happen immediately in the same tick</item>
        ///   </list>
        /// </item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>State machines where different states use different subtrees</item>
        /// <item>Dynamic behavior selection based on complex runtime conditions</item>
        /// <item>Looping behaviors that reset and restart when complete</item>
        /// <item>Planning systems with dynamic goal selection</item>
        /// <item>Context-sensitive behavior switching</item>
        /// </list>
        ///
        /// <para><b>Example - Simple State Machine:</b></para>
        /// <code>
        /// YieldDynamic("Enemy AI", controller =>
        /// {
        ///     controller.WithResetYieldedNodeOnNodeChange(); // Reset old state when switching
        ///
        ///     return _ => currentState switch
        ///     {
        ///         EnemyState.Patrol => PatrolBehavior(),
        ///         EnemyState.Combat => CombatBehavior(),
        ///         EnemyState.Flee => FleeBehavior(),
        ///         _ => IdleBehavior()
        ///     };
        /// });
        /// // Automatically switches between states, resetting old behaviors cleanly
        /// </code>
        ///
        /// <para><b>Example - Dynamic Priority Selection:</b></para>
        /// <code>
        /// YieldDynamic("Dynamic Priority", controller =>
        /// {
        ///     controller
        ///         .WithResetYieldedNodeOnNodeChange()
        ///         .WithConsumeTickOnStateChange(false); // Switch immediately
        ///
        ///     return _ =>
        ///     {
        ///         if (enemyInRange) return AttackEnemy();
        ///         if (healthLow) return Heal();
        ///         if (itemNearby) return CollectItem();
        ///         return Patrol();
        ///     };
        /// });
        /// // Dynamically switches between behaviors based on priority each tick
        /// </code>
        ///
        /// <para><b>Setup Function Pattern:</b></para>
        /// The setup function receives a YieldController for configuration and must return a
        /// Func&lt;YieldController, Node&gt; that will be called every tick to get the current node.
        /// The returned function receives the same controller, allowing state to be shared.
        ///
        /// <para><b>Technical Details:</b></para>
        /// <list type="bullet">
        /// <item>Uses a state machine (STATE_DEFAULT, STATE_HANDLE_RESET_NODE)</item>
        /// <item>Handles node switching gracefully with proper cleanup</item>
        /// <item>Supports async reset operations via ResetGracefully()</item>
        /// <item>Manages child node lifecycle (OnEnter/OnExit) automatically</item>
        /// <item>Integrates with Unity editor for tree visualization</item>
        /// </list>
        ///
        /// <para><b>Difference from YieldSimpleCached:</b></para>
        /// YieldSimpleCached evaluates getNode once and caches the result.
        /// YieldDynamic evaluates getNode every tick, allowing dynamic switching.
        ///
        /// <para><b>Performance Considerations:</b></para>
        /// The getNode function is called every tick, so keep it lightweight. For expensive node creation,
        /// consider caching nodes outside the yield and just returning references.
        /// </remarks>
        public static YieldNode YieldDynamic(string name, YieldSetupFunc setup) => Composite<YieldNode>("Yield", () =>
        {
            var self = (YieldNode)CurrentNode;
            Node yieldedNode = null;
            Node nodeToReset = null;
            Node nodeToChangeTo = null;

            var _active = Variable(static () => false);
            var _state = Variable(0);
            var _lastCompletedStatus = Variable(Status.Success);
            var controller = new YieldController(() => yieldedNode);
            var getNode = setup(controller);

            SetNodeName(name);

#if UNITY_EDITOR
            var originalGetNode = getNode;
            getNode = (ctrl) =>
            {
                var result = originalGetNode(ctrl);

                if (result != null)
                    result.Editor.RootNode = self.Editor.RootNode;

                return result;
            };
#endif

            OnInvalidCheck(static () => true);

            void SetYieldedNode(Node node)
            {
                if (yieldedNode == node)
                    return;

                self.Children.Clear();
                yieldedNode = node;

                if (!Node.IsInvalid(node))
                {
                    ForceAddChild(self, node);
#if UNITY_EDITOR
                    if (!self.RootEditor.YieldNodes.Contains(self))
                    {
                        self.RootEditor.YieldNodes.Add(self);
                        self.RootEditor.NotifyTreeStructureChanged(self);
                    }
#endif
                }
                else
                {
#if UNITY_EDITOR
                    self.RootEditor.YieldNodes.Remove(self);
                    self.RootEditor.NotifyTreeStructureChanged(self);
#endif
                }
            }

            OnEnter(() =>
            {
                yieldedNode = null;
                _active.Value = true;
            });

            OnDeserialize(() =>
            {
                self.Children.Clear();

                if (_active.Value && yieldedNode != null)
                {
                    ForceAddChild(self, yieldedNode);

#if UNITY_EDITOR
                    if (!self.RootEditor.YieldNodes.Contains(self))
                    {
                        self.RootEditor.YieldNodes.Add(self);
                        self.RootEditor.NotifyTreeStructureChanged(self);
                    }
#endif
                }
            });

            OnExit(async ct =>
            {
                if (controller.NodeExitResetPolicy == YieldResetPolicy.Reset && yieldedNode != null)
                    await ResetNode(yieldedNode);

                self.Children.Clear();
                _active.Value = false;

#if UNITY_EDITOR
                self.RootEditor.YieldNodes.Remove(self);
                self.RootEditor.NotifyTreeStructureChanged(self);
#endif
            });

            //-----------------------------------------------------

            const int STATE_DEFAULT = 0;
            const int STATE_HANDLE_RESET_NODE = 1;

            //-----------------------------------------------------

            OnBaseTick(() =>
            {
                bool workDone;
                do
                {
                    workDone = false;

                    if (_state.Value == STATE_DEFAULT)
                    {
                        var newNode = getNode(controller);

                        // Handle initial node assignment
                        if (newNode != yieldedNode && yieldedNode == null)
                        {
                            SetYieldedNode(newNode);
                            workDone = true;
                        }
                        // Handle node switching
                        else if (newNode != yieldedNode && newNode != null)
                        {
                            if (controller.NodeChangeResetPolicy == YieldResetPolicy.Reset && yieldedNode != null)
                            {
                                _state.Value = STATE_HANDLE_RESET_NODE;
                                nodeToReset = yieldedNode;
                                nodeToChangeTo = newNode;
                                workDone = true;
                            }
                            else
                            {
                                SetYieldedNode(newNode);
                                workDone = true;
                            }
                        }

                        // Tick the current node and handle completion
                        if (_state.Value == STATE_DEFAULT && (yieldedNode?.Tick(out var status) ?? false))
                        {
                            _lastCompletedStatus.Value = status;

                            // Check if state changed before completing - if so, continue in loop instead
                            var nodeAfterCompletion = getNode(controller);
                            if (nodeAfterCompletion != yieldedNode)
                            {
                                // State changed, switch to new node instead of returning
                                if (controller.NodeChangeResetPolicy == YieldResetPolicy.Reset)
                                {
                                    _state.Value = STATE_HANDLE_RESET_NODE;
                                    nodeToReset = yieldedNode;
                                    nodeToChangeTo = nodeAfterCompletion;
                                    workDone = true;
                                }
                                else
                                {
                                    SetYieldedNode(nodeAfterCompletion);
                                    workDone = true;
                                }
                            }
                            else
                            {
                                return status;
                            }
                        }
                    }

                    // Handle resetting a node for switching
                    if (_state.Value == STATE_HANDLE_RESET_NODE)
                    {
                        if (nodeToReset != null && nodeToReset.ResetGracefully())
                        {
                            SetYieldedNode(nodeToChangeTo);
                            nodeToReset = null;
                            nodeToChangeTo = null;
                            _state.Value = STATE_DEFAULT;
                            workDone = true;
                        }
                    }


                }
                while (!controller.ConsumeTickOnStateChange && workDone);

                return Status.Running;
            });
        });

        //*************************************************************************************************

        /// <summary>
        /// Creates a yield node that dynamically inserts and switches between child nodes at runtime.
        /// Uses "Yield Dynamic" as the default name.
        /// </summary>
        /// <param name="setup">A function that receives a YieldController and returns a function that returns the current node to yield</param>
        /// <returns>A yield node with fully configurable dynamic behavior</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Yield Dynamic" as the default node name.
        /// See <see cref="YieldDynamic(string, YieldSetupFunc)"/> for detailed behavior description.
        /// </remarks>
        public static YieldNode YieldDynamic(YieldSetupFunc setup)
        {
            return YieldDynamic("Yield Dynamic", setup);
        }


        /// <summary>
        /// Creates a yield node that gracefully resets child nodes when switching between them.
        /// This is a convenience wrapper around YieldDynamic with reset policies enabled.
        /// </summary>
        /// <param name="name">The name of the yield node for debugging and visualization</param>
        /// <param name="setup">A function that receives a YieldController and returns a function that returns the current node to yield</param>
        /// <returns>A yield node configured for safe node switching</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Automatically configures NodeChangeResetPolicy to Reset</item>
        /// <item>Automatically configures NodeExitResetPolicy to Reset</item>
        /// <item>Ensures old nodes are gracefully cleaned up when switching</item>
        /// <item>Same configuration as YieldSimpleCached but allows dynamic switching</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// YieldSwitching("State Machine", _ => GetCurrentStateBehavior());
        /// // Safely switches between states, cleaning up old state before entering new one
        /// </code>
        ///
        /// <para><b>Equivalent to:</b></para>
        /// <code>
        /// YieldDynamic(controller =>
        /// {
        ///     controller
        ///         .WithResetYieldedNodeOnNodeChange()
        ///         .WithResetYieldedNodeOnSelfExit();
        ///     return _ => MyBehavior();
        /// });
        /// </code>
        /// </remarks>
        public static YieldNode YieldSwitching(string name, YieldSetupFunc setup)
        {
            return YieldDynamic(name, controller =>
            {
                controller.WithResetYieldedNodeOnNodeChange().WithResetYieldedNodeOnSelfExit();
                return setup(controller);
            });
        }
    }
}

#endif
