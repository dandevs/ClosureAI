#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        //********************************************************************************************
        // OnDeserialize lifecycle method

        /// <summary>
        /// Registers a callback to execute when a node is deserialized (loaded from saved state or reconstructed).
        /// Use this to restore runtime state, reconnect references, or reinitialize transient data after deserialization.
        /// Multiple OnDeserialize callbacks can be registered on the same node.
        /// </summary>
        /// <remarks>
        /// <para><b>Called When:</b> Node is deserialized from a saved state or reconstructed from serialized data</para>
        /// <para><b>Use Case:</b> Restore non-serializable references, rebuild caches, reconnect to external systems</para>
        /// <para><b>Multiple Allowed:</b> You can register multiple OnDeserialize callbacks</para>
        /// <para><b>Error Handling:</b> Logs an error if called outside of a node context</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute after deserialization.</param>
        /// <example>
        /// <code>
        /// Leaf("PersistentNode", () =>
        /// {
        ///     var target = Variable&lt;GameObject&gt;(() => null);
        ///     var targetID = Variable(() => "");
        ///
        ///     OnEnabled(() =>
        ///     {
        ///         target.Value = FindTarget();
        ///         targetID.Value = target.Value.GetInstanceID().ToString();
        ///     });
        ///
        ///     OnDeserialize(() =>
        ///     {
        ///         // Restore target reference after deserialization
        ///         target.Value = FindObjectByID(targetID.Value);
        ///     });
        /// });
        /// </code>
        /// </example>
        public static void OnDeserialize(Action action)
        {
            if (CurrentNode != null)
                CurrentNode.OnDeserializeActions.Add(action);
            else
                Debug.LogError($"{nameof(OnDeserialize)} called outside of a node");
        }
    }
}

#endif