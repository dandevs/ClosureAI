#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a leaf node that always returns Running and marks itself as always invalid.
        /// This node continuously invalidates, forcing re-entry in reactive trees.
        /// </summary>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that perpetually returns Running</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Just Running" as the default node name.
        /// See <see cref="JustRunning(string, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode JustRunning(Action setup = null) => JustRunning("Just Running", setup);

        /// <summary>
        /// Creates a leaf node that always returns Running and marks itself as always invalid.
        /// This node continuously invalidates, forcing re-entry in reactive trees each tick.
        /// </summary>
        /// <param name="name">The name of the node for debugging and visualization</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that perpetually returns Running</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Calls AlwaysInvalidate() to mark itself as always invalid</item>
        /// <item>Always returns <see cref="Status.Running"/> from OnBaseTick</item>
        /// <item>Never completes on its own</item>
        /// <item>Forces re-entry in reactive parent nodes every tick</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Placeholder for behaviors under development</item>
        /// <item>Forcing continuous re-evaluation in reactive trees</item>
        /// <item>Creating infinite loops when combined with decorators</item>
        /// <item>Testing and debugging behavior tree execution</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Race(() =>
        /// {
        ///     JustRunning(); // Keeps the race running
        ///     WaitUntil(() => shouldStop); // Wins when condition becomes true
        /// });
        /// </code>
        ///
        /// <para><b>Reactive Behavior:</b></para>
        /// Due to AlwaysInvalidate(), this node constantly signals invalidation to parent reactive nodes,
        /// causing them to re-check children and potentially restart sequences/selectors each tick.
        /// </remarks>
        public static LeafNode JustRunning(string name, Action setup = null) => Leaf("Just Running", () =>
        {
            SetNodeName(name);
            AlwaysInvalidate();
            OnBaseTick(() => Status.Running);
            setup?.Invoke();
        });

        //***********************************************************************************************************

        /// <summary>
        /// Creates a leaf node that always returns Success immediately.
        /// This node completes in a single tick and always succeeds.
        /// </summary>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that always succeeds</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Just Success" as the default node name.
        /// See <see cref="JustSuccess(string, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode JustSuccess(Action setup = null) => JustSuccess("Just Success", setup);

        /// <summary>
        /// Creates a leaf node that always returns Success immediately.
        /// This node completes in a single tick and always succeeds.
        /// </summary>
        /// <param name="name">The name of the node for debugging and visualization</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that always succeeds</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Always returns <see cref="Status.Success"/> from OnBaseTick</item>
        /// <item>Completes in a single tick</item>
        /// <item>Does not perform any actions</item>
        /// <item>Immediately transitions to Done state</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Optional tasks in sequences (always succeed even if not needed)</item>
        /// <item>Placeholder nodes during development</item>
        /// <item>Fallback success nodes in selectors</item>
        /// <item>Testing behavior tree execution paths</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Selector(() =>
        /// {
        ///     Condition("Has Weapon", () => hasWeapon);
        ///     JustSuccess(); // Succeed anyway if no weapon (optional weapon system)
        /// });
        /// </code>
        /// </remarks>
        public static LeafNode JustSuccess(string name, Action setup = null) => Leaf("Just Success", () =>
        {
            SetNodeName(name);
            OnBaseTick(() => Status.Success);
            setup?.Invoke();
        });

        //***********************************************************************************************************

        /// <summary>
        /// Creates a leaf node that always returns Failure immediately.
        /// This node completes in a single tick and always fails.
        /// </summary>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that always fails</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Just Failure" as the default node name.
        /// See <see cref="JustFailure(string, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode JustFailure(Action setup = null) => JustFailure("Just Failure", setup);

        /// <summary>
        /// Creates a leaf node that always returns Failure immediately.
        /// This node completes in a single tick and always fails.
        /// </summary>
        /// <param name="name">The name of the node for debugging and visualization</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that always fails</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Always returns <see cref="Status.Failure"/> from OnBaseTick</item>
        /// <item>Completes in a single tick</item>
        /// <item>Does not perform any actions</item>
        /// <item>Immediately transitions to Done state</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Forcing failure in sequences for testing</item>
        /// <item>Triggering selector fallback paths</item>
        /// <item>Placeholder nodes during development</item>
        /// <item>Debugging and testing behavior tree execution</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Selector(() =>
        /// {
        ///     JustFailure(); // Force trying next option
        ///     Do("Fallback Action", () => PerformFallback());
        /// });
        /// </code>
        /// </remarks>
        public static LeafNode JustFailure(string name, Action setup = null) => Leaf("Just Failure", () =>
        {
            SetNodeName(name);
            OnBaseTick(() => Status.Failure);
            setup?.Invoke();
        });
    }
}

#endif