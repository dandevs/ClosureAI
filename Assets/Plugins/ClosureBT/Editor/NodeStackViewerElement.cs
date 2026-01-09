#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ClosureBT.Editor.UI;
using ClosureBT.UI;
using UnityEngine;
using UnityEngine.UIElements;
using static ClosureBT.UI.VisualElementBuilderHelper;
using static ClosureBT.BT;

namespace ClosureBT.Editor
{
    public class NodeStackViewerElement : ScrollView, IVisualElementController
    {
        private readonly Func<Node> _getNode;
        private readonly VisualElement _container;

        private readonly List<Node> _nodeStack = new();   // currently displayed order
        private readonly ConditionalWeakTable<Node, VisualElement> _nodeToView = new();
        private Action<Node> _setSelectedNode;
        private bool _hasDrawn;

        // Re-usable buffer to avoid per-frame allocations
        private readonly List<Node> _currentStackBuffer = new();

        // Drag-to-pan variables
        private bool _isDragging;
        private Vector2 _dragStartPosition;
        private Vector2 _scrollStartPosition;

        public Node Node => _getNode();

        public NodeStackViewerElement(Func<Node> getNode, Action<Node> setSelectedNode)
        {
            _getNode  = getNode;
            _container = new VisualElement();
            _setSelectedNode = setSelectedNode;
            mode = ScrollViewMode.VerticalAndHorizontal;

            Add(_container);
            Render();
            SetupDragToPan();
        }

        private void Render() => E(this, _ =>
        {
            Style(new()
            {
                display = DisplayStyle.Flex,
                flexGrow = 1,
                width = Length.Percent(100),
                overflow = Overflow.Hidden,
                backgroundColor = ColorPalette.NodeStackBackground,
            });

            Scheduler.Execute(RefreshStack).Every(0);
        });

        private static void BuildCurrentStack(Node root, List<Node> stack)
        {
            stack.Clear();

            if (Node.IsInvalid(root))
                return;

            stack.Add(root);

            if (root.Editor is { YieldNodes: not null })
                stack.AddRange(root.Editor.YieldNodes.Select(static yn => yn.Children[0]));

            // Reverse to match the original behaviour.
            stack.Reverse();
        }

        private void RefreshStack()
        {
            if (Node.IsInvalid(Node))
                return;

            BuildCurrentStack(Node, _currentStackBuffer);

            // Nothing changed? Early-out.
            if (_hasDrawn && _currentStackBuffer.SequenceEqual(_nodeStack))
                return;

            _hasDrawn = true;

            /* --- REMOVE disappeared nodes --- */
            foreach (var removed in _nodeStack.Except(_currentStackBuffer).ToArray())
            {
                if (_nodeToView.TryGetValue(removed, out var view))
                {
                    view.RemoveFromHierarchy();
                    _nodeToView.Remove(removed);
                }
            }

            /* --- ADD new nodes --- */
            const int padding = 32;
            const int horizontalPadding = 28;

            foreach (var added in _currentStackBuffer.Except(_nodeStack))
            {
                var i = _currentStackBuffer.IndexOf(added);

                var nView = E<VisualElement>(_ =>
                {
                    FlexGap(40);
                    Style(new()
                    {
                        display = DisplayStyle.Flex,
                        flexGrow = 1,
                        flexDirection = FlexDirection.Row,
                    });

                    E(new Label(), label =>
                    {
                        Style(new()
                        {
                            fontSize = 22,
                            marginRight = 24,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            color = ColorPalette.TertiaryText,
                        });

                        var idx = _currentStackBuffer.Count - i - 1;
                        label.text = $"{idx}.";
                    });

                    E(new NodeVisualizerView(() => added, this));
                });

                nView.style.paddingTop = padding;
                nView.style.paddingBottom = padding;
                nView.style.paddingLeft = horizontalPadding;
                nView.style.paddingRight = horizontalPadding;

                _nodeToView.AddOrUpdate(added, nView);
            }

            /* --- ENSURE CORRECT ORDER IN CONTAINER --- */
            for (var i = 0; i < _currentStackBuffer.Count; i++)
            {
                var node = _currentStackBuffer[i];

                if (!_nodeToView.TryGetValue(node, out var view))
                {
                    throw new InvalidOperationException(
                        $"Node {node.Name} at index {i} is not in the view. This should not happen.");
                }

                // If the view is not at the expected position, re-insert it.
                if (i >= _container.childCount || _container[i] != view)
                {
                    view.RemoveFromHierarchy();
                    _container.Insert(i, view);
                }
            }

            /* --- ZEBRA STRIPE --- */
            for (var i = 0; i < _currentStackBuffer.Count; i++)
            {
                if (_nodeToView.TryGetValue(_currentStackBuffer[i], out var view))
                {
                    view.style.backgroundColor = i % 2 == 0
                        ? new Color(0.15f, 0.15f, 0.15f, 0f) // Transparent to inherit parent
                        : ColorPalette.NodeStackZebraStripe;

                    // Add subtle border for alternating rows
                    view.style.borderBottomWidth = 1f;
                    view.style.borderBottomColor = ColorPalette.NodeStackDivider;
                }
            }

            _nodeStack.Clear();
            _nodeStack.AddRange(_currentStackBuffer);
        }

        public void OnNodeVisualElementClicked(Node node, ClickEvent e)
        {
            if (e.clickCount == 1)
                _setSelectedNode?.Invoke(node);

            if (e.clickCount == 2)
                node.Editor.OpenInTextEditor();
        }

        private void SetupDragToPan()
        {
            // Register mouse down event
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) // Left mouse button
                {
                    _isDragging = true;
                    _dragStartPosition = evt.mousePosition;
                    _scrollStartPosition = new Vector2(scrollOffset.x, scrollOffset.y);
                    evt.StopPropagation(); // Prevent other handlers from receiving this event
                }
            });

            // Register mouse move event
            RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (_isDragging)
                {
                    Vector2 delta = evt.mousePosition - _dragStartPosition;

                    // Update scroll offset (inverted because we're dragging the content)
                    scrollOffset = new Vector2(
                        _scrollStartPosition.x - delta.x,
                        _scrollStartPosition.y - delta.y
                    );

                    evt.StopPropagation();
                }
            });

            // Register mouse up event
            RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0 && _isDragging)
                {
                    _isDragging = false;
                    evt.StopPropagation();
                }
            });

            // Handle case where mouse leaves the element while dragging
            RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (_isDragging)
                {
                    _isDragging = false;
                }
            });
        }
    }
}

#endif
