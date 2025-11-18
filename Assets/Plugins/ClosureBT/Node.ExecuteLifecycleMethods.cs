#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        public partial class Node
        {
            internal static async UniTaskVoid ExecuteOnLifecycleMethods(
                List<(MethodLifecycleType, object)> methods,
                bool suppressCancellationThrow,
                Node node,
                Action<Node> continuation,
                string lifecycle)
            {
                try
                {
                    foreach (var (type, func) in methods)
                    {
                        if (type == MethodLifecycleType.Async)
                        {
                            try
                            {
                                await ((Func<CancellationToken, UniTask>)func).Invoke(node.GetCancellationToken());
                            }
                            catch (OperationCanceledException)
                            {
                                if (!node.Resetting && !suppressCancellationThrow)
                                    throw;
                            }
                            catch (Exception exception)
                            {
                                Debug.LogException(new NodeException(node, exception, lifecycle));
                            }
                        }
                        else
                        {
                            try
                            {
                                ((Action)func).Invoke();
                            }
                            catch (Exception exception)
                            {
                                Debug.LogException(new NodeException(node, exception, lifecycle));
                            }
                        }
                    }
                }
                finally
                {
                    if (!node.Resetting)
                        node.TryReturnCancellationTokenSource();
                }

                continuation?.Invoke(node);
            }

            internal UniTaskVoid ExecuteOnEnableMethods(Action<Node> continuation, bool suppressCancellationThrow = false)
            {
                return ExecuteOnLifecycleMethods(OnEnabledActions, suppressCancellationThrow, this, continuation, "On Enable");
            }

            internal UniTaskVoid ExecuteOnEnterMethods(Action<Node> continuation, bool suppressCancellationThrow = false)
            {
                return ExecuteOnLifecycleMethods(OnEnterActions, suppressCancellationThrow, this, continuation, "On Enter");
            }

            internal UniTaskVoid ExecuteOnExitMethods(Action<Node> continuation, bool suppressCancellationThrow = false)
            {
                return ExecuteOnLifecycleMethods(OnExitActions, suppressCancellationThrow, this, continuation, "On Exit");
            }

            internal UniTaskVoid ExecuteOnDisableMethods(Action<Node> continuation, bool suppressCancellationThrow = false)
            {
                return ExecuteOnLifecycleMethods(OnDisabledActions, suppressCancellationThrow, this, continuation, "On Disable");
            }

            internal UniTaskVoid ExecuteOnSuccessMethods(Action<Node> continuation, bool suppressCancellationThrow = false)
            {
                return ExecuteOnLifecycleMethods(OnSuccessActions, suppressCancellationThrow, this, continuation, "On Success");
            }

            internal UniTaskVoid ExecuteOnFailureMethods(Action<Node> continuation, bool suppressCancellationThrow = false)
            {
                return ExecuteOnLifecycleMethods(OnFailureActions, suppressCancellationThrow, this, continuation, "On Failure");
            }
        }
    }
}

#endif
