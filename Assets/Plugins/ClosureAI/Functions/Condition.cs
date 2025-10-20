#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a leaf node that evaluates a boolean condition and returns Success or Failure based on the result.
        /// This node is reactive-aware and will invalidate when the condition value changes, allowing parent reactive nodes to respond.
        /// </summary>
        /// <param name="name">The name of the condition node for debugging and visualization</param>
        /// <param name="condition">A function that returns the boolean condition to evaluate each tick</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that succeeds when the condition is true and fails when false</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Evaluates the condition function every tick</item>
        /// <item>Returns <see cref="Status.Success"/> if the condition is true</item>
        /// <item>Returns <see cref="Status.Failure"/> if the condition is false</item>
        /// <item>Tracks the previous condition value to detect changes</item>
        /// <item>Invalidates when the condition value changes (triggers re-evaluation in reactive trees)</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Branching execution based on runtime state (e.g., "is enemy in range?")</item>
        /// <item>Checking prerequisites before actions (e.g., "do I have ammo?")</item>
        /// <item>Environmental checks (e.g., "is door open?", "is player visible?")</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Sequence(() =>
        /// {
        ///     Condition("Enemy in Range", () => Vector3.Distance(enemy.position, transform.position) &lt; 10f);
        ///     Do("Attack", () => AttackEnemy());
        /// });
        /// </code>
        ///
        /// <para><b>Reactive Behavior:</b></para>
        /// When used in a reactive tree, this node will signal invalidation when the condition changes,
        /// causing parent sequences/selectors to re-evaluate and potentially restart execution from this point.
        /// </remarks>
        public static Node Condition(string name, Func<bool> condition, Action lifecycle = null) => Leaf("Condition", () =>
        {
            var _previous = Variable(false);

            SetNodeName(name);
            OnInvalidCheck(() =>
            {
                return _previous.Value != condition();
                // var ok = condition();

                // if (ok != _previous.Value)
                //     return true;

                // _previous.Value = ok;
                // return false;
                // if (_previous.Value != ok)
                //     return true;

                // _previous.SetValueSilently(ok);
                // return false;
            });

            OnBaseTick(() =>
            {
                _previous.SetValueSilently(condition());
                return _previous.Value ? Status.Success : Status.Failure;
            });

            lifecycle?.Invoke();
        });

        /// <summary>
        /// Creates a leaf node that evaluates a boolean condition and returns Success or Failure based on the result.
        /// Uses "Condition" as the default name.
        /// </summary>
        /// <param name="condition">A function that returns the boolean condition to evaluate each tick</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that succeeds when the condition is true and fails when false</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Condition" as the default node name.
        /// See <see cref="Condition(string, Func{bool}, Action)"/> for detailed behavior description.
        /// </remarks>
        public static Node Condition(Func<bool> condition, Action lifecycle = null) =>
            Condition("Condition", condition, lifecycle);
    }
}

#endif