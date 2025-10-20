#if UNITASK_INSTALLED
using System.Runtime.CompilerServices;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Marks the current node to always invalidate in a reactive behavior tree.
        /// This is a convenience method equivalent to <c>OnInvalidCheck(() => true)</c>.
        /// When used in a reactive parent, this node will always trigger re-entry after completion.
        /// </summary>
        /// <remarks>
        /// <para><b>Shorthand For:</b> <c>OnInvalidCheck(() => true)</c></para>
        /// <para><b>Reactive Trees:</b> Only has effect in trees marked with the <c>Reactive *</c> multiplier</para>
        /// <para><b>Effect:</b> After this node completes, if a sibling is running, this will invalidate and reset that sibling</para>
        /// <para><b>Use Case:</b> Nodes that should always re-evaluate when revisited, like constantly changing conditions</para>
        /// <para><b>Performance:</b> Aggressively inlined for zero overhead</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// Tree = Reactive * SequenceAlways("Root", () =>
        /// {
        ///     Condition("PlayerNearby", () =>
        ///     {
        ///         AlwaysInvalidate(); // Always re-check player proximity
        ///         OnBaseTick(() => IsPlayerNearby() ? Status.Success : Status.Failure);
        ///     });
        ///
        ///     // If player proximity changes, this gets reset and we start over
        ///     Leaf("InteractWithPlayer", () => { /* ... */ });
        /// });
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AlwaysInvalidate()
        {
            OnInvalidCheck(static () => true);
        }
    }
}

#endif