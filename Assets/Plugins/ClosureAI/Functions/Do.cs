#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a leaf node that executes an action and immediately returns Success.
        /// This node completes in a single tick and always succeeds.
        /// </summary>
        /// <param name="name">The name of the node for debugging and visualization</param>
        /// <param name="action">The action to execute when the node ticks</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that executes the action and succeeds</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Executes the provided action in OnBaseTick</item>
        /// <item>Always returns <see cref="Status.Success"/> after executing the action</item>
        /// <item>Completes in a single tick</item>
        /// <item>Does not invalidate (remains Done unless reset)</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Performing side-effect actions (e.g., "log message", "set flag")</item>
        /// <item>Triggering events or animations (e.g., "play sound", "spawn particle")</item>
        /// <item>Modifying state (e.g., "increment counter", "change mode")</item>
        /// <item>Quick actions that should always succeed</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Sequence(() =>
        /// {
        ///     Do("Log Start", () => Debug.Log("Starting sequence"));
        ///     Do("Set Flag", () => isActive = true);
        ///     Do("Play Sound", () => audioSource.Play());
        /// });
        /// </code>
        ///
        /// <para><b>Note:</b></para>
        /// If you need an action that can fail, use a <see cref="Leaf"/> node with custom OnBaseTick logic instead.
        /// </remarks>
        public static Node Do(string name, Action action, Action setup = null) => Leaf("Do", () =>
        {
            SetNodeName(name);
            OnBaseTick(() =>
            {
                action();
                return Status.Success;
            });

            setup?.Invoke();
        });

        /// <summary>
        /// Creates a leaf node that executes an action and immediately returns Success.
        /// Uses "Do" as the default name.
        /// </summary>
        /// <param name="action">The action to execute when the node ticks</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that executes the action and succeeds</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Do" as the default node name.
        /// See <see cref="Do(string, Action, Action)"/> for detailed behavior description.
        /// </remarks>
        public static Node Do(Action action, Action setup = null)
        {
            return Do("Do", action, setup);
        }
    }
}

#endif