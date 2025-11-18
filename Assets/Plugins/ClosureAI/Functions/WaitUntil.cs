#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a leaf node that waits until a condition becomes true, then succeeds.
        /// This node is reactive-aware and will invalidate when the condition becomes false again.
        /// </summary>
        /// <param name="name">The name of the node for debugging and visualization</param>
        /// <param name="condition">A function that returns the boolean condition to check each tick</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits for the condition to become true</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Returns <see cref="Status.Running"/> while the condition is false</item>
        /// <item>Returns <see cref="Status.Success"/> once the condition becomes true</item>
        /// <item>Evaluates the condition function every tick</item>
        /// <item>Invalidates when the condition becomes false (triggers re-evaluation in reactive trees)</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Waiting for state changes (e.g., "wait until door opens")</item>
        /// <item>Synchronization points (e.g., "wait until animation completes")</item>
        /// <item>Conditional delays (e.g., "wait until enemy is in range")</item>
        /// <item>Event-driven behavior (e.g., "wait until player input detected")</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Sequence(() =>
        /// {
        ///     Do("Start Animation", () => animator.Play("Attack"));
        ///     WaitUntil("Animation Done", () => !animator.IsPlaying("Attack"));
        ///     Do("Finish", () => CompleteAttack());
        /// });
        /// </code>
        ///
        /// <para><b>Reactive Behavior:</b></para>
        /// When used in a reactive tree, this node will signal invalidation if the condition becomes false after
        /// succeeding, allowing parent sequences/selectors to respond to the changing condition.
        ///
        /// <para><b>Difference from Wait:</b></para>
        /// Wait waits for a time duration, WaitUntil waits for a condition to become true.
        /// </remarks>
        public static LeafNode WaitUntil(string name, Func<bool> condition, Action lifecycle = null) => Leaf("Wait Until", () =>
        {
            SetNodeName(name);
            OnBaseTick(() => condition() ? Status.Success : Status.Running);
            OnInvalidCheck(() => condition() != true);
            lifecycle?.Invoke();
        });

        /// <summary>
        /// Creates a leaf node that waits until a condition becomes true, then succeeds.
        /// Uses "Wait Until" as the default name.
        /// </summary>
        /// <param name="condition">A function that returns the boolean condition to check each tick</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits for the condition to become true</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Wait Until" as the default node name.
        /// See <see cref="WaitUntil(string, Func{bool}, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode WaitUntil(Func<bool> condition, Action lifecycle = null) =>
            WaitUntil("Wait Until", condition, lifecycle);

        //******************************************************************************************

        /// <summary>
        /// Creates a leaf node that waits while a condition remains true, then succeeds when it becomes false.
        /// This is the inverse of WaitUntil - it succeeds when the condition becomes false.
        /// </summary>
        /// <param name="name">The name of the node for debugging and visualization</param>
        /// <param name="condition">A function that returns the boolean condition to check each tick</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits while the condition is true</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Returns <see cref="Status.Running"/> while the condition is true</item>
        /// <item>Returns <see cref="Status.Success"/> once the condition becomes false</item>
        /// <item>Evaluates the condition function every tick</item>
        /// <item>Invalidates when the condition is true (opposite of WaitUntil)</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Waiting for clearing conditions (e.g., "wait while door is closed")</item>
        /// <item>Pause behaviors (e.g., "wait while player is invincible")</item>
        /// <item>Inverse synchronization (e.g., "wait while enemy is alive")</item>
        /// <item>Hold patterns (e.g., "wait while button is held")</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Sequence(() =>
        /// {
        ///     Do("Engage Shield", () => shield.Activate());
        ///     WaitWhile("Shield Active", () => shield.IsActive);
        ///     Do("Shield Depleted", () => OnShieldDepleted());
        /// });
        /// </code>
        ///
        /// <para><b>Difference from WaitUntil:</b></para>
        /// WaitUntil succeeds when condition becomes true.
        /// WaitWhile succeeds when condition becomes false.
        /// </remarks>
        public static LeafNode WaitWhile(string name, Func<bool> condition, Action lifecycle = null) => Leaf("Wait While", () =>
        {
            SetNodeName(name);
            OnBaseTick(() => condition() ? Status.Running : Status.Success);
            OnInvalidCheck(condition);
            lifecycle?.Invoke();
        });

        /// <summary>
        /// Creates a leaf node that waits while a condition remains true, then succeeds when it becomes false.
        /// Uses "Wait While" as the default name.
        /// </summary>
        /// <param name="condition">A function that returns the boolean condition to check each tick</param>
        /// <param name="lifecycle">Optional lifecycle callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits while the condition is true</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Wait While" as the default node name.
        /// See <see cref="WaitWhile(string, Func{bool}, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode WaitWhile(Func<bool> condition, Action lifecycle = null) =>
            WaitWhile("Wait While", condition, lifecycle);
    }
}

#endif
