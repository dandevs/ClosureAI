#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureAI
{
    public static partial class AI
    {
        //********************************************************************************************
        // OnTick lifecycle methods

        /// <summary>
        /// Registers a synchronous callback to execute every tick during the <see cref="SubStatus.Running"/> phase, after <see cref="OnBaseTick"/>.
        /// Use this to update shared state, perform side effects, or run logic that should happen regardless of BaseTick's return value.
        /// Unlike <see cref="OnBaseTick"/>, multiple OnTick callbacks can be registered on the same node.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None → Enabling → Entering → <b>Running</b> → Success/Failure → Exiting → Done</para>
        /// <para><b>Called When:</b> Every tick while SubStatus == SubStatus.Running, after OnBaseTick executes</para>
        /// <para><b>Order:</b> OnPreTick → OnBaseTick → <b>OnTick</b></para>
        /// <para><b>Multiple Allowed:</b> Unlike OnBaseTick, you can register multiple OnTick callbacks</para>
        /// <para><b>No Return Value:</b> OnTick doesn't affect node status (use OnBaseTick for that)</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute after BaseTick each frame.</param>
        /// <example>
        /// <code>
        /// Sequence("Parent", () =>
        /// {
        ///     var sharedState = Variable(() => 0);
        ///
        ///     OnTick(() =>
        ///     {
        ///         // Update shared state for all children
        ///         sharedState.Value = CalculateCurrentState();
        ///     });
        ///
        ///     Leaf("Child1", () => OnBaseTick(() => UseSharedState(sharedState.Value)));
        ///     Leaf("Child2", () => OnBaseTick(() => UseSharedState(sharedState.Value)));
        /// });
        /// </code>
        /// </example>
        public static void OnTick(Action action)
        {
            CurrentNode?.OnTicks.Add(action);
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Running"/> phase.
        /// This allows multi-tick async side effects that run alongside the main node logic.
        /// Unlike <see cref="OnBaseTick"/>, this doesn't affect the node's status.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None → Enabling → Entering → <b>Running</b> → Success/Failure → Exiting → Done</para>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Side Effects Only:</b> OnTick doesn't return a status - it's for side effects like updating UI, playing sounds, etc.</para>
        /// <para><b>Async Fire-and-Forget:</b> The async operation runs in the background; the node continues immediately.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and tick function for multi-tick side effects.</param>
        /// <example>
        /// <code>
        /// Leaf("Attack", () =>
        /// {
        ///     OnBaseTick(() => PerformAttack());
        ///
        ///     OnTick(async (ct, tick) =>
        ///     {
        ///         // Play sound effect over multiple frames
        ///         StartAttackSound();
        ///         await tick();
        ///         ContinueAttackSound();
        ///         await tick();
        ///         EndAttackSound();
        ///     });
        /// });
        /// </code>
        /// </example>
        public static void OnTick(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            if (CurrentNode != null)
            {
                var core = CreateAsyncTickCore(CurrentNode, async (ct, tick) =>
                {
                    await action(ct, tick);
                    return Status.Success;
                });

                CurrentNode.OnTicks.Add(() => core());
            }
        }
    }
}


#endif