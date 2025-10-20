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
            /// Creates a decorator that fails if its child doesn't complete within the specified timeout duration.
            /// If the child completes before the timeout, returns the child's status.
            /// If the timeout expires while the child is still running, exits the child and returns Failure.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="duration">A function that returns the timeout duration in seconds</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a timeout on its child</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Tracks elapsed time since OnEnter using Time.timeAsDouble</item>
            /// <item>Returns child's status if child completes before timeout</item>
            /// <item>Returns <see cref="Status.Failure"/> if timeout expires while child is Running</item>
            /// <item>Exits the child when timeout expires (calls child.Exit())</item>
            /// <item>Returns <see cref="Status.Running"/> while child is running and not timed out</item>
            /// <item>Re-evaluates duration function each tick (allows dynamic timeouts)</item>
            /// <item>Resets elapsed time to 0 on OnEnter</item>
            /// <item>Exits the child when decorator exits</item>
            /// <item>Invalidates when child is invalid (passes through invalidation)</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Preventing infinite loops or stuck behaviors</item>
            /// <item>Enforcing time limits on operations (e.g., "search for 5 seconds max")</item>
            /// <item>Aborting slow pathfinding or expensive computations</item>
            /// <item>Creating fallback behaviors when tasks take too long</item>
            /// <item>Testing if a condition becomes true within a time limit</item>
            /// </list>
            ///
            /// <para><b>Example - Search with Timeout:</b></para>
            /// <code>
            /// Selector("Find Target", () =>
            /// {
            ///     D.Timeout(5f); // Max 5 seconds to search
            ///     SearchForTarget();
            ///     // If search takes longer than 5 seconds, timeout fails and selector tries next option
            ///
            ///     UseDefaultTarget(); // Fallback
            /// });
            /// </code>
            ///
            /// <para><b>Example - Limited Pathfinding:</b></para>
            /// <code>
            /// Sequence("Navigate", () =>
            /// {
            ///     D.Timeout(2f);
            ///     CalculatePath();
            ///     // If pathfinding takes more than 2 seconds, timeout fails and sequence aborts
            ///
            ///     FollowPath();
            /// });
            /// </code>
            ///
            /// <para><b>Example - Wait with Max Duration:</b></para>
            /// <code>
            /// D.Timeout(10f);
            /// WaitUntil(() => targetVisible);
            /// // Wait for target to become visible, but give up after 10 seconds
            /// // Returns Success if target becomes visible within 10s
            /// // Returns Failure if 10 seconds pass without target becoming visible
            /// </code>
            ///
            /// <para><b>Example - Dynamic Timeout:</b></para>
            /// <code>
            /// D.Timeout(() => difficultyLevel * 3f); // Easier difficulty = more time allowed
            /// SolvePuzzle();
            /// </code>
            ///
            /// <para><b>Example - Repeated Action with Timeout:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     D.Timeout(1f);
            ///     D.Repeat();
            ///     TryOpenDoor();
            ///     // Repeatedly tries to open door for 1 second, then times out
            ///
            ///     BreakDownDoor(); // Fallback if timeout
            /// });
            /// </code>
            ///
            /// <para><b>Timeout Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Elapsed time starts at 0 when decorator enters (OnEnter)</item>
            /// <item>Elapsed time increments each tick by Time.deltaTime</item>
            /// <item>When elapsed >= duration, immediately exits child and returns Failure</item>
            /// <item>Child status is returned if child completes before timeout</item>
            /// </list>
            ///
            /// <para><b>Child Exit on Timeout:</b></para>
            /// When timeout expires, the decorator calls child.Exit() to ensure proper cleanup.
            /// This triggers the child's OnExit callbacks and disables the child gracefully.
            ///
            /// <para><b>Invalidation Logic:</b></para>
            /// OnInvalidCheck simply returns node.Child.IsInvalid() - passes through child's invalidation.
            /// The timeout does not affect invalidation logic, only execution time.
            ///
            /// <para><b>Difference from D.Cooldown:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Timeout:</b> Limits execution time, fails if child takes too long</item>
            /// <item><b>D.Cooldown:</b> Prevents execution for duration AFTER completion</item>
            /// </list>
            ///
            /// <para><b>Difference from Wait:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Timeout:</b> Decorator that fails child after duration while child is running</item>
            /// <item><b>Wait:</b> Leaf node that waits for duration then succeeds (no child)</item>
            /// </list>
            ///
            /// <para><b>Difference from D.Until:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Timeout:</b> Fails after time duration expires</item>
            /// <item><b>D.Until:</b> Loops child until condition or status, no time limit</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Timeout(5f);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode Timeout(string name, Func<float> duration, Action setup = null) => Decorator("Timeout", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                var _elapsed = Variable(static () => 0f);
                var _duration = Variable(0f, duration);
                var timeLastTick = Time.timeAsDouble;

                SetNodeName(name);
                OnInvalidCheck(() => node.Child.IsInvalid());
                OnExit(_ => ExitNode(node.Child));

                OnEnter(() =>
                {
                    timeLastTick = Time.timeAsDouble;
                    _elapsed.Value = 0f;
                });

                OnDeserialize(() => timeLastTick = Time.timeAsDouble);

                OnBaseTick(() =>
                {
                    _duration.Value = duration();
                    _elapsed.Value += (float)(Time.timeAsDouble - timeLastTick);
                    timeLastTick = Time.timeAsDouble;

                    // Check if timeout has expired
                    if (_elapsed.Value >= _duration.Value)
                    {
                        // Exit the child node and return failure
                        node.Child.Exit();
                        return Status.Failure;
                    }

                    // Tick the child normally
                    return node.Child.Tick(out var status) ? status : Status.Running;
                });

                setup?.Invoke();
            });

            /// <summary>
            /// Creates a decorator that fails if its child doesn't complete within the specified timeout duration.
            /// If the child completes before the timeout, returns the child's status.
            /// If the timeout expires while the child is still running, exits the child and returns Failure.
            /// </summary>
            /// <param name="duration">A function that returns the timeout duration in seconds</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a timeout on its child</returns>
            /// <remarks>
            /// See <see cref="Timeout(string, Func{float}, Action)"/> for full documentation.
            /// This overload uses an auto-generated name.
            /// </remarks>
            public static DecoratorNode Timeout(Func<float> duration, Action setup = null)
            {
                return Timeout("Timeout", duration, setup);
            }

            /// <summary>
            /// Creates a decorator that fails if its child doesn't complete within the specified timeout duration.
            /// If the child completes before the timeout, returns the child's status.
            /// If the timeout expires while the child is still running, exits the child and returns Failure.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="duration">The timeout duration in seconds (constant value)</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a timeout on its child</returns>
            /// <remarks>
            /// See <see cref="Timeout(string, Func{float}, Action)"/> for full documentation.
            /// This overload uses a constant duration.
            /// </remarks>
            public static DecoratorNode Timeout(string name, float duration, Action setup = null)
            {
                return Timeout(name, () => duration, setup);
            }

            /// <summary>
            /// Creates a decorator that fails if its child doesn't complete within the specified timeout duration.
            /// If the child completes before the timeout, returns the child's status.
            /// If the timeout expires while the child is still running, exits the child and returns Failure.
            /// </summary>
            /// <param name="duration">The timeout duration in seconds (constant value)</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a timeout on its child</returns>
            /// <remarks>
            /// See <see cref="Timeout(string, Func{float}, Action)"/> for full documentation.
            /// This overload uses a constant duration and auto-generated name.
            /// </remarks>
            public static DecoratorNode Timeout(float duration, Action setup = null)
            {
                return Timeout("Timeout", () => duration, setup);
            }
        }
    }
}

#endif