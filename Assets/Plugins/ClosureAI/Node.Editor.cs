#if UNITASK_INSTALLED
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ClosureAI.Editor;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        // This contains debugging info, such as determining which line and file the node was created
        public partial class Node
        {
            /// <summary>
            /// Utilities for handling editor related tasks. Only works in editor.
            /// </summary>
            // public EditorOnly Editor { get; private set; }
            public EditorOnly Editor
            {
                get
                {
                    if (_editor == null)
                    {
                        if (_nodeStack.TryPeek(out var parentNode))
                            _editor = new(this, parentNode.Editor.RootNode);
                        else
                            _editor = new(this, this);
                    }

                    return _editor;
                }
                set => _editor = value;
            }
            public EditorOnly RootEditor => Editor.RootNode.Editor;

            private EditorOnly _editor;

            public class EditorOnly
            {
                public static readonly List<Node> RootNodes = new();
                public static event Action<Node> OnRootNodeActivated = delegate {};
                public static event Action<Node> OnRootNodeDeactivated = delegate {};
                public string[] NameHistory = new string[0];

                private string _sourceFilePath;
                private int _sourceLineNumber;
                private static readonly string[] _lineSplitStrings = { "\n" };

                public Node Node;
                public Node RootNode;
                // public NodeHistoryTrackerV2 HistoryTracker { get; private set; }
                public bool IsRootNode => Node == RootNode;

                public event Action<Node> OnSubStatusChanged = delegate {};
                public event Action<Node> OnStatusChanged = delegate {};
                public event Action<Node> OnTreeStructureChanged = delegate {};
                public event Action<Node> OnMarkedForRecordingChanged = delegate {};
                public event Action<Node> OnExceptionThrown = delegate {};

                // This is global vs localized individual nodes in OnStatusChanged
                public event Action<Node> OnChildrenStatusChanged = delegate {};

                private List<YieldNode> _yieldNodes;
                public List<YieldNode> YieldNodes => RootNode.Editor._yieldNodes ??= new();
                // public Dictionary<int, NodeSnapshotData> FrameIndexSnapshotDict = new();

                // public readonly AsyncReactiveProperty<Status> StatusAsync = new(default);
                // public readonly AsyncReactiveProperty<SubStatus> SubStatusAsync = new(default);

                // public static readonly AsyncReactiveProperty<Node> ActivatedRootNodeAsync = new(null);
                // public static readonly AsyncReactiveProperty<Node> DeactivatedRootNodeAsync = new(null);

                private bool _markedForRecording = true;
                public bool MarkedForRecording
                {
                    get => _markedForRecording;
                    set
                    {
                        _markedForRecording = value;
                        OnMarkedForRecordingChanged(Node);
                    }
                }

                private bool _initialized = false;

                /// <summary>
                /// Called to initialize or re-initialize the editor after domain reload.
                /// Subscribe to events and set up any editor-specific state here.
                /// </summary>
                public void OnInitialize()
                {
                    if (_initialized)
                        return;

                    _initialized = true;

                    // Example: Initialize history tracking or other editor features
                    // NodeHistoryTrackerV2.Get(Node);

                    // Add your initialization logic here
                    // This is where you can subscribe to events or set up editor state
                }

                /// <summary>
                /// Static initialization method to be called after domain reload.
                /// Call this from InitializeOnLoad or similar Unity callbacks.
                /// </summary>
                public void NotifySubStatusChanged(Node node)
                {
                    OnSubStatusChanged(node);
                }

                public void NotifyStatusChanged(Node node, Status previousStatus)
                {
                    if (node.Editor.IsRootNode)
                    {
                        if (previousStatus == Status.None && node.Status != Status.None)
                        {
                            RootNodes.Add(node);
                            OnRootNodeActivated(node);
                            // ActivatedRootNodeAsync.Value = node;
                        }

                        else if (previousStatus != Status.None && node.Status == Status.None)
                        {
                            RootNodes.Remove(node);
                            OnRootNodeDeactivated(node);
                            // DeactivatedRootNodeAsync.Value = node;
                        }
                    }

                    OnStatusChanged(node);
                    RootNode.Editor.OnChildrenStatusChanged(node);
                }

                public void NotifyTreeStructureChanged(Node node)
                {
                    OnTreeStructureChanged(Node);

                    if (!IsRootNode)
                        RootNode?.Editor.NotifyTreeStructureChanged(node);
                }

                public void NotifyExceptionThrown(Node node)
                {
                    OnExceptionThrown(node);

                    if (!IsRootNode)
                        RootNode?.Editor.NotifyExceptionThrown(node);
                }

                public EditorOnly(Node node, Node rootNode)
                // public EditorOnly()
                {
                    Node = node;
                    RootNode = rootNode;
                    SetSourceInfoEditorOnly();
                }

                /// <summary>
                /// An improved, more concise version of source file detection for debugging purposes.
                /// </summary>
                /// <remarks>
                /// Determines the source file path and line number where the node was created by:
                /// <list type="number">
                /// <item>
                ///     <description>Extracting the full stack trace</description>
                /// </item>
                /// <item>
                ///     <description>First, attempts to find any anonymous/lambda method (contained in angle brackets)
                ///     that isn't part of the ClosureAI or Unity framework</description>
                /// </item>
                /// <item>
                ///     <description>If no lambda is found, falls back to the first user code line in the stack
                ///     (anything not from ClosureAI or UnityEngine namespaces)</description>
                /// </item>
                /// <item>
                ///     <description>Extracts the file path and line number from the identified stack line using Utils.ExtractFileInfo</description>
                /// </item>
                /// </list>
                ///
                /// This simplified approach requires fewer iterations through the stack trace compared to V1,
                /// focusing specifically on finding lambdas first, then falling back to any user code.
                /// </remarks>
                private void SetSourceInfoEditorOnly()
                {
                    var trace = StackTraceUtility.ExtractStackTrace();

                    var lines = trace.Split(_lineSplitStrings, StringSplitOptions.RemoveEmptyEntries)
                        .Where(static line =>
                        {
                            // return !line.StartsWith("ClosureAI.AI") && !line.StartsWith("UnityEngine.");
                            if (line.StartsWith("ClosureAI.AI"))
                                return false;

                            if (line.StartsWith("UnityEngine."))
                                return false;

                            if (line.StartsWith("System.Activator:CreateInstance"))
                                return false;

                            return true;
                        })
                        .ToArray();

                    var line = lines.FirstOrDefault(static line => line.Contains("<") && line.Contains(">"));

                    // Fallback if no anonymous method found
                    if (string.IsNullOrEmpty(line))
                        line = lines.FirstOrDefault();

                    (_sourceFilePath, _sourceLineNumber) = NodeEditorUtility.ExtractFileInfo(line);
                }

                public void OpenInTextEditor()
                {
                    if (_sourceLineNumber < 0)
                    {
                        Debug.LogWarning("Source line number is not set.");
                        return;
                    }

                    if (string.IsNullOrEmpty(_sourceFilePath))
                    {
                        Debug.LogWarning("Source file path is not set.");
                        return;
                    }

                    NodeEditorUtility.OpenInTextEditor(_sourceFilePath, _sourceLineNumber);
                }
            }
        }
    }
}
#endif

#endif
