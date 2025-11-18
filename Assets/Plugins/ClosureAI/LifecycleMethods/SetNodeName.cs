#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Sets or changes the display name of the current node.
        /// This is useful for dynamically naming nodes based on runtime parameters or for debugging.
        /// </summary>
        /// <remarks>
        /// <para><b>When to Use:</b> Inside node setup closures to override or modify the node's name</para>
        /// <para><b>Debugging:</b> Helps identify nodes in inspector visualization or logs</para>
        /// <para><b>Dynamic Names:</b> Can be combined with variables to create context-aware names</para>
        /// </remarks>
        /// <param name="name">The new name to assign to the current node.</param>
        /// <example>
        /// <code>
        /// public Node MoveTo(Vector3 target) => Leaf("MoveTo", () =>
        /// {
        ///     SetNodeName($"MoveTo ({target.x:F1}, {target.y:F1}, {target.z:F1})");
        ///     OnBaseTick(() => MoveTowards(target));
        /// });
        ///
        /// // Or dynamically based on state:
        /// Leaf("DynamicNode", () =>
        /// {
        ///     var state = Variable(() => "Idle");
        ///     OnEnter(() => SetNodeName($"State: {state.Value}"));
        /// });
        /// </code>
        /// </example>
        public static void SetNodeName(string name)
        {
            if (CurrentNode != null)
                CurrentNode.Name = name;
        }
    }
}

#endif
