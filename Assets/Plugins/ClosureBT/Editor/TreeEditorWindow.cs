#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using ClosureBT.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClosureBT.UI;
using static ClosureBT.UI.VisualElementBuilderHelper;
using static ClosureBT.BT;
using Object = UnityEngine.Object;

namespace ClosureBT.Editor
{
    public class TreeEditorWindow : EditorWindow, IVisualElementController
    {
        [SerializeField] private string _treeFieldName;
        [SerializeField] private string _rootNodeName;
        [SerializeField] private int _instanceID;
        [SerializeField] private string _globalObjectIdString; // GlobalObjectId must be stored as string for serialization

        private GlobalObjectId _globalObjectId; // Cached, reconstructed from string

        private static readonly Stack<TreeEditorWindow> _stack = new();
        public static TreeEditorWindow Current => _stack.TryPeek(out var window) ? window : null;

        private bool _isUpdating = false; // Guard flag to prevent re-entrant calls

        public Object Object { get; private set; }
        public Node SelectedNode { get; private set; }
        public Node RootNode { get; private set; }
        public Node PreviousRootNode { get; private set; }

        private NodeInspectorView _inspectorView;

        // Snapshot controls UI elements
        private Label _snapshotStatusLabel;
        private Button _previousButton;
        private Button _nextButton;
        private Button _resumeButton;
        private SliderInt _snapshotSlider;
        private VisualElement _snapshotControlsContainer;

        // var _gridBG = DrawGridPattern.CreateGridTexture();
        private static Texture2D _gridBG;

        public static void Open(Object targetObject, string rootNodeName)
        {
            if (targetObject == null)
            {
                Debug.LogError("Cannot open TreeEditorWindow with null target object.");
                return;
            }

            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(targetObject);
            var instanceID = targetObject.GetInstanceID();

            // Check if a window with the same instanceID and rootNodeName is already open
            var existingWindows = Resources.FindObjectsOfTypeAll<TreeEditorWindow>();
            foreach (var existingWindow in existingWindows)
            {
                // Fast path: Match by instanceID (unique per object in session) and rootNodeName
                if (existingWindow._instanceID == instanceID && existingWindow._rootNodeName == rootNodeName)
                {
                    // Focus the existing window instead of creating a new one
                    existingWindow.Focus();
                    return;
                }
            }

            // No existing window found, create a new one
            var window = CreateInstance<TreeEditorWindow>();
            window.titleContent = new GUIContent($"({rootNodeName}) Tree Editor");
            window._treeFieldName = rootNodeName;
            window._rootNodeName = rootNodeName;
            window._instanceID = instanceID;
            window._globalObjectId = globalObjectId;
            window._globalObjectIdString = globalObjectId.ToString(); // Serialize as string
            window.Show();
            window.Focus();
            // Force CreateGUI to be called if it hasn't been already
            EditorApplication.delayCall += () =>
            {
                if (window && window.rootVisualElement.childCount == 0)
                {
                    window.CreateGUI();
                }
            };
        }

        private Object GetObject()
        {
            // Try instance ID first (fast path, stable within sessions)
            if (_instanceID != 0)
            {
                var obj = EditorUtility.InstanceIDToObject(_instanceID);

                if (obj != null)
                    return obj;
            }

            if (_globalObjectId.assetGUID == default && !string.IsNullOrEmpty(_globalObjectIdString))
                GlobalObjectId.TryParse(_globalObjectIdString, out _globalObjectId);

            if (_globalObjectId.assetGUID != default)
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(_globalObjectId);

                if (obj != null)
                {
                    _instanceID = obj.GetInstanceID(); // Update instance ID for next time (fast path)
                    return obj;
                }
            }

            return null;
        }

        private void CreateGUI()
        {
            _stack.Push(this);
            OnInspectorUpdate();
            RootNode = GetNode(GetObject());

            // If we couldn't get the root node, show an error message
            if (RootNode == null)
            {
                rootVisualElement.Clear();
                ShowErrorMessage();
                _stack.Pop();
                return;
            }

            rootVisualElement.Clear();

            E(rootVisualElement, root =>
            {
                // Main content area with tree and inspector
                E<VisualElement>(mainContent =>
                {
                    Style(new()
                    {
                        flexGrow = 1,
                        flexShrink = 1,
                        flexDirection = FlexDirection.Row,
                        position = Position.Relative,
                        overflow = Overflow.Hidden,
                    });

                    E<FlexColumn>(() =>
                    {
                        Style(new()
                        {
                            flexGrow = 1,
                            flexDirection = FlexDirection.ColumnReverse,
                        });

                        E(InfoBarSection());

                        E(new SnapshotIndexControllerElement(() => RootNode));

                        E(new NodeStackViewerElement(() => RootNode, n => SelectedNode = n), treeViewer =>
                        {
                            Style(new()
                            {
                                flexGrow = 1,
                                minWidth = 200,
                            });
                        });
                    });

                    // Inspector panel on the right
                    _inspectorView = new NodeInspectorView(() => SelectedNode);
                    E(new ResizablePanel(ResizablePanel.BarPosition.Left, _inspectorView), panel =>
                    {
                        Style(new() { width = 450 });
                    });
                });
            });

            _stack.Pop();
        }

        public void ReloadGUI()
        {
            // Unsubscribe before clearing
            rootVisualElement.Clear();
            CreateGUI();
        }

        //*********************************************************************************************

        private void OnInspectorUpdate()
        {
            // Prevent re-entrant calls that cause stack overflow
            if (_isUpdating)
                return;

            _isUpdating = true;
            try
            {
                Object = GetObject();
                RootNode = GetNode(Object);

                // If we don't have a root node but we have valid data, try to reload the GUI
                if (RootNode == null && (_globalObjectId.assetGUID != default || _instanceID != 0) && !string.IsNullOrEmpty(_rootNodeName))
                {
                    // Attempt to recreate the GUI in case the object is now available
                    if (Object != null)
                    {
                        ReloadGUI();
                    }
                }

                if (PreviousRootNode != RootNode)
                    PreviousRootNode = RootNode;

                if (Object is MonoBehaviour mb)
                    titleContent.text = $"({mb.GetType().Name} / {_treeFieldName}) Tree Graph";
            }
            finally
            {
                _isUpdating = false;
            }
        }

        //*********************************************************************************************

        private void ShowErrorMessage()
        {
            E(rootVisualElement, _ =>
            {
                E<VisualElement>(errorContainer =>
                {
                    Style(new()
                    {
                        flexGrow = 1,
                        justifyContent = Justify.Center,
                        alignItems = Align.Center,
                        paddingLeft = 40,
                        paddingRight = 40,
                    });

                    E<VisualElement>(errorCard =>
                    {
                        Style(new()
                        {
                            backgroundColor = new Color(0.15f, 0.1f, 0.1f, 1f), // Darker red tint
                            borderWidth = 2,
                            borderColor = new Color(0.8f, 0.2f, 0.2f, 1f), // Red border
                            borderRadius = 8,
                            paddingLeft = 24,
                            paddingRight = 24,
                            paddingTop = 20,
                            paddingBottom = 20,
                            maxWidth = 500,
                            alignItems = Align.Center,
                        });

                        // Icon/Title section
                        E<Label>(iconLabel =>
                        {
                            iconLabel.text = "⚠";
                            Style(new()
                            {
                                fontSize = 48,
                                color = new Color(0.95f, 0.3f, 0.3f, 1f), // Bright red
                                unityTextAlign = TextAnchor.MiddleCenter,
                                marginBottom = 12,
                            });
                        });

                        // Main error title
                        E<Label>(titleLabel =>
                        {
                            titleLabel.text = "Failed to Load Node";
                            Style(new()
                            {
                                fontSize = 18,
                                color = ColorPalette.VeryLightGrayText,
                                unityFontStyleAndWeight = FontStyle.Bold,
                                unityTextAlign = TextAnchor.MiddleCenter,
                                marginBottom = 12,
                            });
                        });

                        // Node name (prominent)
                        E<VisualElement>(nodeNameContainer =>
                        {
                            Style(new()
                            {
                                backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.6f),
                                borderRadius = 4,
                                paddingLeft = 12,
                                paddingRight = 12,
                                paddingTop = 8,
                                paddingBottom = 8,
                                marginBottom = 16,
                            });

                            E<Label>(nodeNameLabel =>
                            {
                                nodeNameLabel.text = _rootNodeName;
                                Style(new()
                                {
                                    fontSize = 16,
                                    color = ColorPalette.OrangeAccent,
                                    unityFontStyleAndWeight = FontStyle.Bold,
                                    unityTextAlign = TextAnchor.MiddleCenter,
                                });
                            });
                        });

                        // Description text
                        E<Label>(descLabel =>
                        {
                            descLabel.text = "Tahe node could not be loaded from the target object.\nPerhaps the scene has changed or a domain reload broke the reference";
                            Style(new()
                            {
                                fontSize = 12,
                                color = ColorPalette.MediumGrayText,
                                unityTextAlign = TextAnchor.MiddleCenter,
                                whiteSpace = WhiteSpace.Normal,
                                marginBottom = 16,
                            });
                        });

                        // Technical details section (collapsible-style)
                        E<VisualElement>(detailsContainer =>
                        {
                            Style(new()
                            {
                                backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f),
                                borderRadius = 4,
                                paddingLeft = 12,
                                paddingRight = 12,
                                paddingTop = 8,
                                paddingBottom = 8,
                                borderWidth = 1,
                                borderColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                            });

                            E<Label>(detailsLabel =>
                            {
                                var details = $"Global ID: {(_globalObjectId.assetGUID != default ? _globalObjectId.ToString() : "Not available")}\n";
                                details += $"Instance ID: {(_instanceID != 0 ? _instanceID.ToString() : "Not available")}";
                                detailsLabel.text = details;
                                Style(new()
                                {
                                    fontSize = 10,
                                    color = ColorPalette.DimGrayText,
                                    unityTextAlign = TextAnchor.MiddleCenter,
                                    whiteSpace = WhiteSpace.Normal,
                                });
                            });
                        });
                    });
                });
            });
        }

        private Node GetNode(Object @object)
        {
            if (@object)
            {
                var type = @object.GetType();
                var field = type.GetField(_rootNodeName);

                if (field != null)
                    return (Node)field.GetValue(@object);
                else
                {
                    var property = type.GetProperty(_rootNodeName);

                    if (property != null)
                        return (Node)property.GetValue(@object);
                }
            }

            return null;
        }

        public void OnNodeVisualElementClicked(Node node, ClickEvent e)
        {
            if (e.clickCount == 1)
                SelectedNode = node;

            else if (e.clickCount == 2)
                node.Editor.OpenInTextEditor();
        }

        private VisualElement InfoBarSection() => E<VisualElement>(infoBar =>
        {
            Style(new()
            {
                backgroundColor = ColorPalette.WindowBackground,
                paddingLeft = 12,
                paddingRight = 12,
                paddingTop = 3,
                paddingBottom = 3,
                borderTopWidth = 1,
                borderTopColor = ColorPalette.SubtleBorder,
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.FlexStart,
                alignItems = Align.Center,
                flexShrink = 0,
            });

            // First hint
            E(new Label("Click and drag to pan"), label =>
            {
                Style(new()
                {
                    fontSize = 9,
                    color = ColorPalette.DimGrayText,
                    unityFontStyleAndWeight = FontStyle.Normal,
                    marginRight = 8,
                });
            });

            // Separator
            E(new Label("•"), separator =>
            {
                Style(new()
                {
                    fontSize = 9,
                    color = ColorPalette.SubtleBorder,
                    marginRight = 8,
                });
            });

            // Second hint
            E(new Label("Double click node to open in editor"), label =>
            {
                Style(new()
                {
                    fontSize = 9,
                    color = ColorPalette.DimGrayText,
                    unityFontStyleAndWeight = FontStyle.Normal,
                });
            });

            // Spacer to push gear icon to the right
            E<VisualElement>(spacer =>
            {
                Style(new()
                {
                    flexGrow = 1,
                });
            });

            // Gear icon button
            E(new Button(() => SettingsService.OpenUserPreferences("Preferences/ClosureBT")), gearButton =>
            {
                gearButton.text = "⋮";
                Style(new()
                {
                    fontSize = 20,
                    color = ColorPalette.DimGrayText,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    minWidth = 20,
                    minHeight = 20,
                    borderWidth = 1,
                    backgroundColor = Color.clear,
                    unityFontStyleAndWeight = FontStyle.Bold,
                });
                gearButton.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    gearButton.style.color = ColorPalette.OrangeAccent;
                });
                gearButton.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    gearButton.style.color = ColorPalette.DimGrayText;
                });
            });
        });

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Deserialize GlobalObjectId from string on enable (after domain reload or window reopen)
            if (_globalObjectId.assetGUID == default && !string.IsNullOrEmpty(_globalObjectIdString))
            {
                GlobalObjectId.TryParse(_globalObjectIdString, out _globalObjectId);
            }

            // Ensure CreateGUI is called if the window is enabled but empty
            if (rootVisualElement.childCount == 0 && (_globalObjectId.assetGUID != default || _instanceID != 0))
            {
                CreateGUI();
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SelectedNode = null;
                RootNode = null;
            }
        }
    }
}

#endif
