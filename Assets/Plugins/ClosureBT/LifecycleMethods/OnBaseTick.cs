#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        //********************************************************************************************
        // OnBaseTick lifecycle methods

        /// <summary>
        /// Registers the main execution logic for a node during the <see cref="SubStatus.Running"/> phase.
        /// This is the core function that determines the node's status (Success, Failure, or Running).
        /// Called every tick while the node is in the Running substatus.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? <b>Running</b> ? Success/Failure ? Exiting ? Done</para>
        /// <para><b>Called When:</b> Every tick while SubStatus == SubStatus.Running, after OnPreTick and before OnTick</para>
        /// <para><b>Return Status.Running:</b> Keep node active, will be called again next tick</para>
        /// <para><b>Return Status.Success:</b> Transition to Succeeding ? OnSuccess ? OnExit ? Done</para>
        /// <para><b>Return Status.Failure:</b> Transition to Failing ? OnFailure ? OnExit ? Done</para>
        /// <para><b>Note:</b> Only ONE OnBaseTick can be registered per node. Calling this multiple times will overwrite.</para>
        /// </remarks>
        /// <param name="action">The function that executes the node's main logic and returns the current status.</param>
        /// <example>
        /// <code>
        /// Leaf("WaitForTarget", () =>
        /// {
        ///     OnBaseTick(() =>
        ///     {
        ///         if (target == null)
        ///             return Status.Failure;
        ///         if (Vector3.Distance(transform.position, target.position) &lt; range)
        ///             return Status.Success;
        ///         return Status.Running;
        ///     });
        /// });
        /// </code>
        /// </example>
        public static void OnBaseTick(Func<Status> action)
        {
            if (CurrentNode != null)
                CurrentNode.baseTick = action;
        }

        /// <summary>
        /// Registers the main asynchronous execution logic for a node with Tick Core support during the <see cref="SubStatus.Running"/> phase.
        /// This allows multi-tick async operations within the main node logic.
        /// Called every tick while the node is in the Running substatus.
        /// </summary>
        /// <remarks>
        /// <para><b>Lifecycle Position:</b> None ? Enabling ? Entering ? <b>Running</b> ? Success/Failure ? Exiting ? Done</para>
        /// <para><b>Tick Core Pattern:</b> The <c>Func&lt;UniTask&gt;</c> parameter returns a task that completes on the next tick.</para>
        /// <para><b>Multi-Tick Support:</b> The async function can span multiple frames while returning Status.Running between ticks.</para>
        /// <para><b>Status Return:</b> Must eventually return Success or Failure to transition out of Running.</para>
        /// <para><b>Note:</b> Only ONE OnBaseTick can be registered per node. Calling this multiple times will overwrite.</para>
        /// </remarks>
        /// <param name="action">The async function receiving a CancellationToken and tick function, returning the node's status.</param>
        /// <example>
        /// <code>
        /// Leaf("ComplexOperation", () =>
        /// {
        ///     OnBaseTick(async (ct, tick) =>
        ///     {
        ///         StartOperation();
        ///         await tick(); // Returns Running this tick
        ///         ContinueOperation();
        ///         await tick(); // Returns Running next tick
        ///         if (FinalizeOperation())
        ///             return Status.Success;
        ///         return Status.Failure;
        ///     });
        /// });
        /// </code>
        /// </example>
        public static void OnBaseTick(Func<CancellationToken, Func<UniTask>, UniTask<Status>> action)
        {
            if (CurrentNode != null)
                CurrentNode.baseTick = CreateAsyncTickCore(CurrentNode, action);
        }
    }
}


#endif
