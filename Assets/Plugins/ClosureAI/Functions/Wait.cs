#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a leaf node that waits for a specified duration before succeeding.
        /// The duration is evaluated fresh every tick, allowing for dynamic wait times.
        /// </summary>
        /// <param name="name">The name of the wait node for debugging and visualization</param>
        /// <param name="duration">A function that returns the wait duration in seconds</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits for the specified duration</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Returns <see cref="Status.Running"/> while elapsed time is less than duration</item>
        /// <item>Returns <see cref="Status.Success"/> once elapsed time reaches or exceeds duration</item>
        /// <item>Tracks elapsed time using Unity's Time.timeAsDouble for accuracy</item>
        /// <item>Re-evaluates the duration function every tick (allows dynamic durations)</item>
        /// <item>Resets elapsed time to zero on OnEnter</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Delays between actions (e.g., "attack ? wait 2 seconds ? attack again")</item>
        /// <item>Animation timing (e.g., "play animation ? wait for duration ? continue")</item>
        /// <item>Cooldown periods (e.g., "use ability ? wait 5 seconds ? ready")</item>
        /// <item>Timed sequences (e.g., "display message ? wait 3 seconds ? hide")</item>
        /// </list>
        ///
        /// <para><b>Example - Fixed Duration:</b></para>
        /// <code>
        /// Sequence(() =>
        /// {
        ///     Do("Fire Weapon", () => Fire());
        ///     Wait("Cooldown", 2f);
        ///     Do("Ready", () => isReady = true);
        /// });
        /// </code>
        ///
        /// <para><b>Example - Dynamic Duration:</b></para>
        /// <code>
        /// Sequence(() =>
        /// {
        ///     Do("Start Spell", () => CastSpell());
        ///     Wait("Cast Time", () => spellData.castTime); // Duration can change
        ///     Do("Complete Spell", () => CompleteSpell());
        /// });
        /// </code>
        ///
        /// <para><b>Technical Details:</b></para>
        /// Uses Time.timeAsDouble instead of Time.deltaTime for more accurate time tracking.
        /// The elapsed time is calculated as the difference between ticks, which is frame-rate independent.
        /// </remarks>
        public static LeafNode Wait(string name, Func<float> duration, Action setup = null) => Leaf("Wait", () =>
        {
            var _elapsed = Variable(static () => 0f);
            var _duration = Variable(0f, duration);
            var timeLastTick = Time.timeAsDouble;

            SetNodeName(name);

            OnEnter(() => timeLastTick = Time.timeAsDouble);
            OnDeserialize(() => timeLastTick = Time.timeAsDouble);

            OnBaseTick(() =>
            {
                _duration.Value = duration();
                _elapsed.Value += (float)(Time.timeAsDouble - timeLastTick);
                timeLastTick = Time.timeAsDouble;
                return _elapsed.Value < _duration.Value ? Status.Running : Status.Success;
            });

            setup?.Invoke();
        });

        /// <summary>
        /// Creates a leaf node that waits for a specified duration before succeeding.
        /// Uses "Wait" as the default name.
        /// </summary>
        /// <param name="duration">A function that returns the wait duration in seconds</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits for the specified duration</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Wait" as the default node name.
        /// See <see cref="Wait(string, Func{float}, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode Wait(Func<float> duration, Action setup = null) => Wait("Wait", duration, setup);

        /// <summary>
        /// Creates a leaf node that waits for a specified fixed duration before succeeding.
        /// </summary>
        /// <param name="name">The name of the wait node for debugging and visualization</param>
        /// <param name="duration">The wait duration in seconds (constant value)</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits for the specified duration</returns>
        /// <remarks>
        /// This overload accepts a constant float duration instead of a function.
        /// See <see cref="Wait(string, Func{float}, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode Wait(string name, float duration, Action setup = null) => Wait(name, () => duration, setup);

        /// <summary>
        /// Creates a leaf node that waits for a specified fixed duration before succeeding.
        /// Uses "Wait" as the default name.
        /// </summary>
        /// <param name="duration">The wait duration in seconds (constant value)</param>
        /// <param name="setup">Optional setup callbacks to configure the node's behavior</param>
        /// <returns>A leaf node that waits for the specified duration</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Wait" as the default node name and accepts a constant duration.
        /// See <see cref="Wait(string, Func{float}, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode Wait(float duration, Action setup = null) => Wait(() => duration, setup);
    }
}

#endif
