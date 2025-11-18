#if UNITASK_INSTALLED
using System;

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
    }
}


#endif
