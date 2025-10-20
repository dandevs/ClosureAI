#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        //********************************************************************************************
        // OnInvalidCheck lifecycle method

        /// <summary>
        /// Registers a callback that determines whether a node should invalidate in a reactive behavior tree.
        /// When a reactive parent (Sequence/SequenceAlways/Selector marked with <c>Reactive *</c>) detects invalidation,
        /// it resets subsequent nodes and re-enters from the invalidated node.
        /// </summary>
        /// <remarks>
        /// <para><b>Reactive Trees:</b> Only has effect in trees marked with the <c>Reactive *</c> multiplier</para>
        /// <para><b>Called When:</b> Reactive parents check completed children for invalidation before ticking the current child</para>
        /// <para><b>Return true:</b> Triggers invalidation - parent resets later nodes and re-enters this one</para>
        /// <para><b>Return false:</b> No invalidation - execution continues normally</para>
        /// <para><b>Common Use:</b> Detect when conditions change (target moved, state changed, etc.)</para>
        /// <para><b>Note:</b> Only ONE OnInvalidCheck can be registered per node. Calling this multiple times will overwrite.</para>
        /// </remarks>
        /// <param name="onInvalidateCheck">Function returning true when the node should invalidate (re-enter).</param>
        /// <example>
        /// <code>
        /// Tree = Reactive * SequenceAlways("Root", () =>
        /// {
        ///     var lastPosition = Variable(() => Vector3.zero);
        ///
        ///     Leaf("MoveTo", () =>
        ///     {
        ///         OnEnter(() => lastPosition.Value = targetPosition);
        ///
        ///         // If target moves more than 5 units, invalidate and re-enter
        ///         OnInvalidCheck(() =>
        ///             Vector3.Distance(targetPosition, lastPosition.Value) > 5f);
        ///
        ///         OnBaseTick(() => MoveTowards(targetPosition));
        ///     });
        ///
        ///     Wait(2f); // If MoveTo invalidates while waiting, this gets reset
        /// });
        /// </code>
        /// </example>
        public static void OnInvalidCheck(Func<bool> onInvalidateCheck)
        {
            if (CurrentNode != null)
                CurrentNode.OnInvalidateCheck = onInvalidateCheck;
        }
    }
}


#endif