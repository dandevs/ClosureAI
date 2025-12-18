#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        //********************************************************************************************
        // OnPreTick lifecycle method

        /// <summary>
        /// Registers a synchronous callback to execute every tick during the <see cref="SubStatus.Running"/> phase, before <see cref="OnBaseTick"/>.
        /// Use this to update state or perform setup that should happen before the main tick logic executes.
        /// Multiple OnPreTick callbacks can be registered on the same node.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? <b>Running</b> ? Success/Failure ? Exiting ? Done</para>
        /// <para><b>Called When:</b> Every tick while SubStatus == SubStatus.Running, before OnBaseTick executes</para>
        /// <para><b>Order:</b> <b>OnPreTick</b> ? OnBaseTick ? OnTick</para>
        /// <para><b>Multiple Allowed:</b> You can register multiple OnPreTick callbacks</para>
        /// <para><b>Use Case:</b> Prepare data, update sensors, or refresh state before main logic</para>
        /// </remarks>
        /// <param name="action">The synchronous action to execute before BaseTick each frame.</param>
        /// <example>
        /// <code>
        /// Leaf("ChaseTarget", () =>
        /// {
        ///     var targetDistance = Variable(() => 0f);
        ///
        ///     OnPreTick(() =>
        ///     {
        ///         // Update distance before main logic runs
        ///         targetDistance.Value = Vector3.Distance(transform.position, target.position);
        ///     });
        ///
        ///     OnBaseTick(() =>
        ///     {
        ///         // Use the freshly updated distance
        ///         if (targetDistance.Value &lt; attackRange)
        ///             return Status.Success;
        ///         MoveToward(target);
        ///         return Status.Running;
        ///     });
        /// });
        /// </code>
        /// </example>
        public static void OnPreTick(Action action)
        {
            CurrentNode?.OnPreTicks.Add(action);
        }

        /// <summary>
        /// Registers an asynchronous callback with Tick Core support to execute during the <see cref="SubStatus.Running"/> phase before <see cref="OnBaseTick"/>.
        /// This allows multi-tick async side effects that run alongside the main node logic.
        /// Unlike <see cref="OnBaseTick"/>, this doesn't affect the node's status.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None → Enabling → Entering → <b>Running</b> → Success/Failure → Exiting → Done</para>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Side Effects Only:</b> OnPreTick doesn't return a status - it's for side effects like updating sensors, preparing data, etc.</para>
        /// <para><b>Async Fire-and-Forget:</b> The async operation runs in the background; the node continues immediately.</para>
        /// </remarks>
        /// <param name="action">The async action receiving a CancellationToken and tick function for multi-tick side effects.</param>
        public static void OnPreTick(Func<CancellationToken, Func<UniTask>, UniTask> action)
        {
            if (CurrentNode != null)
            {
                var core = CreateAsyncTickCore(CurrentNode, async (ct, tick) =>
                {
                    await action(ct, tick);
                    return Status.Success;
                });

                CurrentNode.OnPreTicks.Add(() => core());
            }
        }
    }
}


#endif
