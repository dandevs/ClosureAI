#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        //********************************************************************************************
        // OnTrueReset lifecycle method

        /// <summary>
        /// Registers a synchronous callback to execute during reset operations (<see cref="Node.ResetImmediately"/> or <see cref="Node.ResetGracefully"/>).
        /// This is called alongside <see cref="OnDisabled"/> but is intended for custom reset logic.
        /// Multiple OnTrueReset callbacks can be registered on the same node.
        /// </summary>
        /// <remarks>
        /// <para><b>Called When:</b> ResetImmediately() or ResetGracefully() is invoked, during the reset flow</para>
        /// <para><b>Difference from OnDisabled:</b> OnDisabled is for cleanup (unsubscribe, deallocate), OnTrueReset is for custom reset state</para>
        /// <para><b>Multiple Allowed:</b> Unlike OnDisabled, multiple OnTrueReset callbacks can be registered</para>
        /// <para><b>Use Case:</b> Custom state resets, clearing caches, or resetting external systems</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute during reset operations.</param>
        /// <example>
        /// <code>
        /// Leaf("CachedNode", () =>
        /// {
        ///     var cache = new Dictionary&lt;string, object&gt;();
        ///
        ///     OnEnabled(() => InitializeCache(cache));
        ///
        ///     OnTrueReset(() =>
        ///     {
        ///         // Clear cache on reset
        ///         cache.Clear();
        ///     });
        ///
        ///     OnDisabled(() => cache = null); // Final cleanup
        /// });
        /// </code>
        /// </example>
        public static void OnTrueReset(Action action)
        {
            if (CurrentNode != null)
            {
                CurrentNode.OnTrueReset ??= new();
                CurrentNode.OnTrueReset.Add(action);
            }
        }
    }
}

#endif