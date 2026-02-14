#if UNITASK_INSTALLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ClosureBT.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClosureBT
{
    public static partial class BT
    {
        public static Node CurrentNode => _nodeStack.TryPeek(out var node) ? node : null;

        private static readonly Stack<Node> _nodeStack = new();
        private static readonly Stack<DecoratorNode> _decorators = new();
        private static readonly List<Node> _nodesCache = new();

        private static readonly Action<Node> _nodeDisableReset = static node =>
        {
            node.Resetting = false;
            node.Active = false;
            node.SubStatus = SubStatus.None;
            node.Status = Status.None;
        };

        private static readonly Action<Node> _nodeDisableResetWithBlockReEnter = static node =>
        {
            node.Resetting = false;
            node.Active = false;
            node.BlockReEnter = false;
            node.SubStatus = SubStatus.None;
            node.Status = Status.None;
        };

        [Serializable]
        public partial class Node
        {
            private static CancellationTokenSource _cancelledTokenSource;
            private static CancellationTokenSource CancelledTokenSource
            {
                get
                {
                    if (_cancelledTokenSource == null)
                    {
                        _cancelledTokenSource = new();
                        _cancelledTokenSource.Cancel();
                    }

                    return _cancelledTokenSource;
                }
            }

            private string _name;
            public string Name
            {
                get => _name;
                set
                {
                    if (_name != value)
                    {
#if UNITY_EDITOR
                        if (Editor != null)
                        {
                            Array.Resize(ref Editor.NameHistory, Editor.NameHistory.Length + 1);
                            Editor.NameHistory[^1] = _name;
                        }
#endif
                        _name = value;
                    }
                }
            }


            [NonSerialized]
            public Node Parent;

            // Hot/status fields moved up and tightly packed
            [SerializeField] private Status _status;
            [SerializeField] private SubStatus _subStatus;

            [Flags]
            private enum NodeFlags : byte
            {
                None = 0,
                IsReactive = 1 << 0,
                Resetting = 1 << 1,
                ResettingGracefully = 1 << 2,
                Active = 1 << 3,
            }

            [SerializeField] private NodeFlags _flags;
            public bool Done => SubStatus == SubStatus.Done && Status is not Status.None;

            public bool Resetting
            {
                get => (_flags & NodeFlags.Resetting) != 0;
                set
                {
                    var wasResetting = (_flags & NodeFlags.Resetting) != 0;
                    if (value) _flags |= NodeFlags.Resetting;
                    else _flags &= ~NodeFlags.Resetting;

                    if (!value && wasResetting)
                    {
                        TryReturnCancellationTokenSource();
                        ResettingGracefully = false;
                        // SkipResetGracefulStatusChange = false;
                    }
                }
            }

            public bool IsReactive
            {
                get => (_flags & NodeFlags.IsReactive) != 0;
                set
                {
                    if (value) _flags |= NodeFlags.IsReactive;
                    else _flags &= ~NodeFlags.IsReactive;
                }
            }

            public bool ResettingGracefully
            {
                get => (_flags & NodeFlags.ResettingGracefully) != 0;
                set
                {
                    if (value) _flags |= NodeFlags.ResettingGracefully;
                    else _flags &= ~NodeFlags.ResettingGracefully;
                }
            }

            public Status Status
            {
                get => _status;
                set
                {
#if UNITY_EDITOR
                    var changed = _status != value;
                    var previous = _status;
#endif
                    _status = value;

                    if (_status == Status.None && SubStatus == SubStatus.None)
                        Resetting = false;

#if UNITY_EDITOR
                    if (changed)
                    {
                        Editor.NotifyStatusChanged(this, previous);
                        // Editor.StatusAsync.Value = _status;
                    }
#endif
                }
            }

            public SubStatus SubStatus
            {
                get => _subStatus;
                set
                {
#if UNITY_EDITOR
                    var changed = _subStatus != value;
#endif

                    _subStatus = value;

#if UNITY_EDITOR
                    if (changed && value == SubStatus.Done)
                    {
                        Editor.NotifySubStatusChanged(this);
                        // Editor.SubStatusAsync.Value = _subStatus;
                    }
#endif
                }
            }

            public bool Active
            {
                get => (_flags & NodeFlags.Active) != 0;
                internal set
                {
                    if (value) _flags |= NodeFlags.Active;
                    else _flags &= ~NodeFlags.Active;
                }
            }

            public bool BlockReEnter { get; internal set; }

            public readonly List<Action> OnTicks = new();
            public readonly List<Action> OnPreTicks = new();
            internal Func<bool> OnInvalidateCheck = static () => false;

            internal readonly List<Action<Node>> OnAnyTicks = new();

            private void InvokeOnAnyTicks()
            {
                for (var i = 0; i < OnAnyTicks.Count; i++)
                    OnAnyTicks[i]?.Invoke(this);
            }

            private CancellationTokenSource _cancellationTokenSource;
            internal CancellationTokenSource CancellationTokenSource => _cancellationTokenSource ??= CancellationTokenSourcePool.Get();

            //----------------------------------------------------------------------

            // Cast type based if async or not
            // 1. (Func<CancellationToken, UniTask>)func
            // 2. (Action)func
            public readonly List<(MethodLifecycleType type, object func)> OnEnabledActions = new();
            public readonly List<(MethodLifecycleType type, object func)> OnEnterActions = new();
            public readonly List<(MethodLifecycleType type, object func)> OnExitActions = new();
            public readonly List<(MethodLifecycleType type, object func)> OnDisabledActions = new();
            public readonly List<(MethodLifecycleType type, object func)> OnSuccessActions = new();
            public readonly List<(MethodLifecycleType type, object func)> OnFailureActions = new();
            public readonly List<Action> OnDeserializeActions = new();

            //-----------------------------------------------------------------------

            [SerializeReference]
            public List<VariableType> Variables = new();
            public Func<Status> baseTick = static () => Status.Success;

            protected Node(string name = null)
            {
                Name = name ?? "Node";

#if UNITY_EDITOR
                if (Editor == null)
                {
                    if (_nodeStack.TryPeek(out var parentNode))
                        Editor = new(this, parentNode.Editor.RootNode);
                    else
                        Editor = new(this, this);
                }
#endif
            }

            internal bool BaseTick()
            {
                foreach (var tick in OnPreTicks)
                    tick();

                Status = baseTick();

                foreach (var tick in OnTicks)
                    tick();

                return Status != Status.Running && Status != Status.None;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TickThenReset() => Tick(out _) && ResetGracefully();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TickThenReset(out Status status) => Tick(out status) && ResetGracefully();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Tick() => Tick(out _);

            public bool Tick(out Status status, bool allowReEnter = false)
            {
                InvokeOnAnyTicks();

                if (Resetting)
                {
                    status = Status;
                    return false;
                }

                if ((Status is (Status.Success or Status.Failure) && (SubStatus == SubStatus.Done && !allowReEnter)))
                {
                    status = Status;
                    return true;
                }

                if (BlockReEnter)
                {
                    BlockReEnter = false;
                    status = Status;
                    return true;
                }

                if (Status == Status.None)
                {
                    SubStatus = SubStatus.Enabling;
                    Status = Status.Running;

                    foreach (var variable in Variables)
                        variable.Initialize?.Invoke();

                    Active = true;
                    ExecuteOnEnableMethods(static node =>
                    {
                        // Check if we got reset during Enabling
                        if (node.Resetting)
                        {
                            node.SubStatus = SubStatus.Disabling;
                            node.ExecuteOnDisableMethods(_nodeDisableReset);
                        }
                        else
                        {
                            node.SubStatus = SubStatus.Entering;
                            node.ExecuteOnEnterMethods(static node =>
                            {
                                // Move onto exiting if it's restarting
                                if (node.Resetting)
                                {
                                    node.SubStatus = SubStatus.Exiting;

                                    node.ExecuteOnExitMethods(static node =>
                                    {
                                        node.ExecuteOnDisableMethods(_nodeDisableReset);
                                    });
                                }
                                else
                                {
                                    node.SubStatus = SubStatus.Running;
                                    node.Status = Status.Running;
                                }
                            })
                            .Forget();
                        }
                    })
                    .Forget();
                }

                if (allowReEnter && SubStatus == SubStatus.Done)
                {
                    SubStatus = SubStatus.Entering;
                    Status = Status.Running;

                    ExecuteOnEnterMethods(static node =>
                    {
                        if (node.Resetting)
                        {
                            node.SubStatus = SubStatus.Exiting;

                            node.ExecuteOnExitMethods(static node =>
                            {
                                node.ExecuteOnDisableMethods(_nodeDisableReset);
                            });
                        }
                        else
                        {
                            node.SubStatus = SubStatus.Running;
                            node.Status = Status.Running;
                        }
                    });

                    status = Status;
                    return false;
                }

                if (Status == Status.Running && SubStatus == SubStatus.Running)
                {
                    try
                    {
                        BaseTick();
                    }
                    catch (NodeException)
                    {
                        Status = Status.Failure;
#if UNITY_EDITOR
                        EditorApplication.isPaused = true;
#endif
                        throw; // Already wrapped, just rethrow
                    }
                    catch (Exception exception)
                    {
                        Status = Status.Failure;

#if UNITY_EDITOR
                        EditorApplication.isPaused = true;
                        Editor.NotifyExceptionThrown(this);
                        throw new NodeException(this, exception);
#else
                        Debug.LogException(new NodeException(this, exception));
#endif
                    }
                }

                if (Status is Status.Success or Status.Failure && SubStatus == SubStatus.Running)
                {
                    // Cancel any active async OnTick
                    if (OnAnyTicks.Count > 0)
                    {
                        _cancellationTokenSource?.Cancel();
                        _cancellationTokenSource = null;
                        OnAnyTicks.Clear();
                    }

                    if (Status is Status.Success)
                    {
                        SubStatus = SubStatus.Succeeding;
                        Status = Status.Running;

                        ExecuteOnSuccessMethods(static node =>
                        {
                            node.SubStatus = SubStatus.Exiting;

                            node.ExecuteOnExitMethods(static node =>
                            {
                                if (node.Resetting)
                                {
                                    node.ExecuteOnDisableMethods(_nodeDisableReset);
                                }
                                else
                                {
                                    node.SubStatus = SubStatus.Done;
                                    node.Status = Status.Success;
                                    node.BlockReEnter = true;
                                }
                            })
                            .Forget();
                        })
                        .Forget();
                    }
                    else
                    {
                        // Execute failure
                        SubStatus = SubStatus.Failing;
                        Status = Status.Running;

                        ExecuteOnFailureMethods(static node =>
                        {
                            node.SubStatus = SubStatus.Exiting;

                            node.ExecuteOnExitMethods(static node =>
                            {
                                if (node.Resetting)
                                {
                                    node.ExecuteOnDisableMethods(_nodeDisableReset);
                                }
                                else
                                {
                                    node.SubStatus = SubStatus.Done;
                                    node.Status = Status.Failure;
                                    node.BlockReEnter = true;
                                }
                            })
                            .Forget();
                        })
                        .Forget();
                    }
                }

                BlockReEnter = false;
                status = Status;
                return Status is Status.Success or Status.Failure;
            }

            // Return true if invalid
            public bool IsInvalid()
            {
                // if (Status == Status.None || (Status == Status.Running && SubStatus == SubStatus.Done))
                if (Status == Status.None)
                    return true;
                else
                    return OnInvalidateCheck();
            }

            internal bool TryReturnCancellationTokenSource()
            {
                var cts = _cancellationTokenSource;
                _cancellationTokenSource = null;
                return CancellationTokenSourcePool.TryReturn(cts);
            }

            public CancellationToken GetCancellationToken()
            {
                return Resetting && !ResettingGracefully
                    ? CancelledTokenSource.Token
                    : CancellationTokenSource.Token;
            }

            internal void CancelCancellationTokenSource(bool force = false)
            {
                if (force)
                {
                    if (_cancellationTokenSource is { IsCancellationRequested: false })
                        _cancellationTokenSource.Cancel();
                }
                else
                {
                    if (_cancellationTokenSource is { IsCancellationRequested: false } && SubStatus is not SubStatus.Done and not SubStatus.None)
                        _cancellationTokenSource.Cancel();
                }
            }

            public bool HasValidCancellationToken()
            {
                if (_cancellationTokenSource == null)
                    return false;

                return !_cancellationTokenSource.IsCancellationRequested;
            }

            public bool ResetImmediately()
            {
                if (Status == Status.None && SubStatus == SubStatus.None)
                    return true;

                if (!Resetting || ResettingGracefully)
                {
                    TraverseDepthFirst(this, static node => node.SubStatus != SubStatus.None, static node =>
                    {
                        if (node.Done)
                        {
                            node.SubStatus = SubStatus.Disabling;
                            node.ExecuteOnDisableMethods(_nodeDisableReset);
                        }
                        else
                        {
                            node.Resetting = true;
                            node.ResettingGracefully = false;
                            node.CancelCancellationTokenSource();
                        }
                    });
                }

                if (SubStatus == SubStatus.Enabling)
                {
                    SubStatus = SubStatus.Disabling;

                    ExecuteOnDisableMethods(_nodeDisableResetWithBlockReEnter);

                    return false;
                }

                if (SubStatus == SubStatus.Running)
                {
                    SubStatus = SubStatus.Exiting;

                    ExecuteOnExitMethods(static node =>
                    {
                        node.ExecuteOnDisableMethods(_nodeDisableReset);
                    });

                    return false;
                }

                if (SubStatus == SubStatus.Done)
                {
                    SubStatus = SubStatus.Disabling;

                    ExecuteOnDisableMethods(_nodeDisableResetWithBlockReEnter);

                    return false;
                }

                return false;
            }

            public bool ResetGracefully(bool invokeAnyTick = true)
            {
                var initSubStatus = SubStatus;

                if (invokeAnyTick)
                    InvokeOnAnyTicks();

                switch (SubStatus)
                {
                    case SubStatus.None: return true;
                    case SubStatus.Disabling: return false;
                    case SubStatus.Exiting: return false;

                    case SubStatus.Enabling:
                    case SubStatus.Entering:
                        CancelCancellationTokenSource();
                        _cancellationTokenSource = null;
                        break;

                    case SubStatus.Running:
                        if (OnAnyTicks.Count > 0) // OnBaseTick/OnTick is using AsyncTickCore
                        {
                            // OnAnyTick handlers cleared on completion
                            CancelCancellationTokenSource();
                            _cancellationTokenSource = null;
                        }
                        break;

                    case SubStatus.Done: break;
                }

                Traverse(this, static node => node.Status != Status.None, static node =>
                {
                    if (node.SubStatus is SubStatus.Entering or SubStatus.Enabling or SubStatus.Running)
                    {
                        node.CancelCancellationTokenSource();
                        node._cancellationTokenSource = null;
                    }
                });

                TraverseDepthFirst(this, static node => node.Status != Status.None, static node =>
                {
                    if (node.SubStatus is SubStatus.Done)
                    {
                        node.SubStatus = SubStatus.Disabling;
                        node.ExecuteOnDisableMethods(_nodeDisableResetWithBlockReEnter);
                    }
                    else
                    {
                        node.ResettingGracefully = true;
                        node.Resetting = true;
                    }
                });

                if (SubStatus == SubStatus.Enabling)
                {
                    SubStatus = SubStatus.Disabling;

                    ExecuteOnDisableMethods(_nodeDisableResetWithBlockReEnter);
                }
                else if (SubStatus == SubStatus.Running)
                {
                    SubStatus = SubStatus.Exiting;

                    ExecuteOnExitMethods(static node =>
                    {
                        node.Resetting = false;

                        node.ExecuteOnDisableMethods(static node =>
                        {
                            node.Active = false;
                            node.SubStatus = SubStatus.None;
                            node.Status = Status.None;
                        });
                    });
                }
                else if (SubStatus == SubStatus.Done)
                {
                    SubStatus = SubStatus.Disabling;

                    ExecuteOnDisableMethods(static node =>
                    {
                        node.Active = false;
                        node.SubStatus = SubStatus.None;
                        node.Status = Status.None;
                    });
                }

                if (initSubStatus == SubStatus.Running)
                    _cancellationTokenSource = null;

                return SubStatus is SubStatus.None;
            }

            public bool Exit(Action<Node> continuation, bool suppressCancellationThrow = false)
            {
                if (SubStatus is SubStatus.None or SubStatus.Done)
                    return true;

                if (SubStatus != SubStatus.Exiting)
                {
                    SubStatus = SubStatus.Exiting;

                    if (Status == Status.Running && OnAnyTicks.Count > 0)
                    {
                        CancelCancellationTokenSource();
                        _cancellationTokenSource = null;
                    }

                    ExecuteOnExitMethods(continuation, suppressCancellationThrow).Forget();
                }

                InvokeOnAnyTicks();
                return SubStatus != SubStatus.Exiting;
            }

            public bool Exit() => Exit(static node =>
            {
                if (node.Resetting)
                {
                    node.ExecuteOnDisableMethods(static node =>
                    {
                        node.Resetting = false;
                        node.SubStatus = SubStatus.None;
                        node.Status = Status.None;
                    });
                }
                else
                    node.SubStatus = SubStatus.Done;
            });

            public static DecoratorNode operator +(DecoratorNode left, Node right)
            {
                var deco = left;
                // Debug.Log($"Attempting left: {left.Name} (child left = {left.Child?.Name ?? "None"}), right: {right.Name}");

                while (deco.Child != null)
                {
                    if (deco.Child is DecoratorNode child)
                        deco = child;
                    else
                        break;
                }

                if (deco != right)
                {
                    deco.Child = right;
                    right.Parent = deco;
                }

                return left;
            }

            public NodeSnapshotData CreateSnapshot()
            {
                _nodesCache.Clear();

                Traverse(_nodesCache, this, static _ => true, static (list, node) =>
                {
                    list.Add(node);
                });

                var count = _nodesCache.Count;

                var data = new NodeSnapshotData
                {
                    NodeStatuses = new(count),
                    VariableValues = new(count),

                    #if UNITY_EDITOR
                    Children = new(),
                    YieldedNodes = new(),
                    #endif
                };

#if UNITY_EDITOR
                data.YieldedNodes.AddRange(RootEditor.YieldNodes);
#endif

                for (var i = 0; i < _nodesCache.Count; i++)
                {
                    var node = _nodesCache[i];
                    data.NodeStatuses.Add((node.Status, node.SubStatus));
#if UNITY_EDITOR
                    switch (node)
                    {
                        case CompositeNode composite:
                            var list = new List<Node>();
                            list.AddRange(composite.Children);
                            data.Children.Add(list);
                            break;
                        case DecoratorNode decorator:
                            data.Children.Add(decorator.Child != null ? new() { decorator.Child } : new());
                            break;
                    }
#endif

                    foreach (var variable in node.Variables)
                    {
                        var value = variable.GetValueAsObject();

                        // Create a shallow copy if value is a list
                        if (value is System.Collections.IList list)
                        {
                            // Create a new list of the same type with the same elements
                            var listType = value.GetType();

                            if (listType.IsGenericType)
                            {
                                var elementType = listType.GetGenericArguments()[0];
                                var newListType = typeof(List<>).MakeGenericType(elementType);
                                var newList = (IList)Activator.CreateInstance(newListType);

                                foreach (var item in list)
                                    newList.Add(item);

                                data.VariableValues.Add(newList);
                            }
                            else
                            {
                                // Non-generic list, create ArrayList copy
                                var newList = new ArrayList();

                                foreach (var item in list)
                                    newList.Add(item);

                                data.VariableValues.Add(newList);
                            }
                        }
                        else
                        {
                            // Not a list, add as-is
                            data.VariableValues.Add(value);
                        }
                    }
                }

                _nodesCache.Clear();
                return data;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Load(NodeSnapshotData snapshotData) => Load(snapshotData, out _);

            public bool Load(NodeSnapshotData snapshotData, out Action revert, OrderedSnapshotDictionary initialSnapshots = null)
            {
                var errored = false;
                var j = 0;
                var i = 0;
                revert = null;

#if UNITY_EDITOR
                var revertStack = new Stack<Action>();

                foreach (var yieldNode in RootEditor.YieldNodes)
                {
                    foreach (var child in yieldNode.Children)
                        initialSnapshots?.TryAdd(child, child.CreateSnapshot());
                }

                RootEditor.YieldNodes.Clear();
#endif

                try
                {
                    Traverse(0, this, static _ => true, (_, node) =>
                    {
                        (node._status, node._subStatus) = snapshotData.NodeStatuses[i++];

                        foreach (var variable in node.Variables)
                            variable.SetValueFromObject(snapshotData.VariableValues[j++]);

                        node.NotifyOnDeserialized();
                    });
                }
                catch
                {
                    errored = true;
                }

                if (i < snapshotData.NodeStatuses.Count)
                {
                    errored = true;
                    // Debug.LogError($"Node count mismatch! Expected {_nodesCache.Count} nodes, but found {snapshotData.NodeStatuses.Count} NodeStatuses in serialized data.");
                }

                if (j < snapshotData.VariableValues.Count)
                {
                    errored = true;
                    // Debug.LogError($"Variable count mismatch! Expected {snapshotData.VariableValues.Count} variables, but found {j} variables in serialized data.");
                }

#if UNITY_EDITOR
                if (errored)
                {
                    // Debug.LogWarning($"Using Editor known nodes instead.  = = = "  +snapshotData.Children.Count);
                    i = 0;
                    j = 0;
                    var k = 0;

                    Traverse(0, this, static _ => true, (_, node) =>
                    {
                        if (node is CompositeNode composite)
                        {
                            var originalChildren = composite.Children.ToArray();
                            var newChildren = snapshotData.Children[k];

                            composite.Children.Clear();
                            composite.Children.AddRange(newChildren);
                            var newChildrenOriginalSnapshot = composite.CreateSnapshot();

                            revertStack.Push(() =>
                            {
                                composite.Load(newChildrenOriginalSnapshot);
                                composite.Children.Clear();
                                composite.Children.AddRange(originalChildren);
                            });

                            k++;
                        }
                        else if (node is DecoratorNode decorator)
                        {
                            var originalChild = decorator.Child;
                            var newChild = snapshotData.Children[k].Count > 0 ? snapshotData.Children[k][0] : null;

                            if (newChild != null)
                            {
                                var snapshot = newChild.CreateSnapshot();
                                initialSnapshots?.TryAdd(newChild, snapshot);

                                revertStack.Push(() =>
                                {
                                    newChild.Load(snapshot);
                                    decorator.Child = originalChild;
                                });

                                decorator.Child = newChild;
                            }

                            k++;
                        }

                        (node._status, node._subStatus) = snapshotData.NodeStatuses[i++];

                        foreach (var variable in node.Variables)
                            variable.SetValueFromObject(snapshotData.VariableValues[j++]);

                        // node.NotifyOnDeserialized();
                    });

                    // Create combined revert action that executes all actions in reverse order
                    revert = () =>
                    {
                        while (revertStack.Count > 0)
                            revertStack.Pop().Invoke();
                    };
                }
#endif

                return !errored;
            }

            internal void NotifyOnDeserialized()
            {
                foreach (var action in OnDeserializeActions)
                    action();
            }

            /// <summary>
            /// Since Node is serialized, it's possible Unity creates a default "empty" node.
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
//             public static bool IsNotNullOrBroken(Node node)
//             {
// #if UNITY_EDITOR
//                 if (node is { Editor: null }) // Not sure how Editor ends up null, but w/e
//                     return false;
// #endif
//
//                 return node != null && !string.IsNullOrEmpty(node.Name);
//             }

            // public static bool IsNullOrBroken(Node node) => !IsNotNullOrBroken(node);
            public static bool IsInvalid(Node node)
            {
#if UNITY_EDITOR
                if (node is { Editor: null }) // Not sure how Editor ends up null, but w/e
                    return true;
#endif

                return node == null || string.IsNullOrEmpty(node.Name);
            }
        }



        public static void ForceAddChild(CompositeNode parent, Node child)
        {
            parent.Children.Add(child);
            child.Parent = parent;

#if UNITY_EDITOR
            // Node.Traverse(parent, child, static _ => true, static (parent, node) =>
            // {
                // node.Editor.RootNode = parent.Editor.RootNode;
            // });

            // child.Editor.RootNode = parent.Editor.RootNode;
            parent.Editor.NotifyTreeStructureChanged(parent);
            // Debug.Log($"ForceAddChild: {parent.Editor.RootNode.Name} -> {parent.Name} -> {child.Name}");
#endif
        }

        //********************************************************************************************

        public enum MethodLifecycleType
        {
            Sync, Async
        }

        public enum Status
        {
            None, Running, Success, Failure
        }

        public enum SubStatus
        {
            None, Enabling, Entering, Running, Succeeding, Failing, Exiting, Disabling, Done
        }
    }
}

#endif
