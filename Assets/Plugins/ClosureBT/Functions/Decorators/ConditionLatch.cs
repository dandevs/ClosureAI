#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a condition decorator that latches once the condition becomes true.
            /// Once latched, the child node continues to run until completion, even if the condition becomes false.
            /// The latch is reset only when the child node completes (returns Success or Failure).
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging purposes</param>
            /// <param name="condition">The condition function to evaluate. Once this returns true, the node latches</param>
            /// <param name="setup">Optional setup action to configure the decorator</param>
            /// <returns>A decorator node that implements condition latching behavior</returns>
            /// <remarks>
            /// This decorator is useful when you want to start an action when a condition becomes true
            /// and ensure it runs to completion even if the triggering condition becomes false.
            ///
            /// Behavior:
            /// - Initially waits for the condition to become true
            /// - Once true, "latches" and continues running the child even if condition becomes false
            /// - Only resets the latch when the child completes
            /// - Returns Failure if condition is false and not latched
            /// </remarks>
            public static DecoratorNode ConditionLatch(string name, Func<bool> condition, Action setup = null) => Decorator("Condition Latch", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                var _latched = Variable(static () => false); // Once true, stays true until child completes
                var _previous = Variable(false);
                var _condition = false;

                SetNodeName(name);
                OnExit(_ =>
                {
                    _latched.SetValueSilently(false);
                    return ExitNode(node.Child);
                });

                OnInvalidCheck(() =>
                {
                    if (!_latched.Value)
                    {
                        var ok = condition();
                        _latched.SetValueSilently(ok && node.Child.IsInvalid());
                    }

                    return _latched.Value;
                });

                OnBaseTick(() =>
                {
                    // Latch the condition once it becomes true
                    if (!_latched.Value && (_condition = condition()))
                        _latched.Value = true;

                    _previous.Value = _condition;

                    // If condition is true or we're latched, run the child
                    if (_latched.Value)
                    {
                        return node.Child.Tick(out var status, true)
                            ? status
                            : Status.Running;
                    }
                    else
                    {
                        return Status.Failure;
                        // Condition is false and we're not latched, reset child and fail
                        // return node.Child.ResetGracefully() ? Status.Failure : Status.Running;
                    }
                });
            });

            /// <summary>
            /// Creates a condition decorator that latches once the condition becomes true.
            /// Once latched, the child node continues to run until completion, even if the condition becomes false.
            /// </summary>
            /// <param name="condition">The condition function to evaluate. Once this returns true, the node latches</param>
            /// <param name="setup">Optional setup action to configure the decorator</param>
            /// <returns>A decorator node that implements condition latching behavior</returns>
            /// <remarks>
            /// This is a convenience overload that uses "Condition Latch" as the default name.
            /// See the named version for detailed behavior description.
            /// </remarks>
            public static DecoratorNode ConditionLatch(Func<bool> condition, Action setup = null)
            {
                return ConditionLatch("Condition Latch", condition, setup);
            }
        }
    }
}

#endif
