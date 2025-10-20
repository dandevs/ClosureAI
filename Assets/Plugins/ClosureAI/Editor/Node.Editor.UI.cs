#if UNITASK_INSTALLED
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using ClosureAI.UI;
using static ClosureAI.AI;

namespace ClosureAI.Editor.UI
{
    public static class NodeUI
    {
        public static readonly ConditionalWeakTable<Node, VisualElement> NodeToVisualElement = new();

        public static VisualElement DrawNodeRecursiveElement(Node node, Dictionary<Node, VisualElement> nodeToVisualElement, int depth = 0)
        {
            switch (node)
            {
                case null:
                    return null;

                case CompositeNode composite:
                {
                    var row = new FlexRow(40);
                    var column = new FlexColumn(20);
                    column.style.flexShrink = 0;

                    if (composite is not YieldNode)
                    {
                        foreach (var child in composite.Children)
                            column.Add(DrawNodeRecursiveElement(child, nodeToVisualElement, depth + 1));
                    }

                    var view = new NodeElementView(node);
                    row.Add(view);
                    row.Add(column);
                    AddToDictionary(node, view);

                    return row;
                }

                case DecoratorNode decorator:
                {
                    var row = new FlexRow(40);
                    var column = new FlexColumn(0);
                    var view = new NodeElementView(node);

                    column.Add(view);
                    view.style.borderBottomWidth = 6f;
                        // view.style.borderBottomLeftRadius = 0f;
                        // view.style.borderBottomRightRadius = 0f;


                    column.Add(DrawNodeRecursiveElement(decorator.Child, nodeToVisualElement, depth + 1));
                    row.Add(column);
                    AddToDictionary(node, view);

                    return row;
                }

                default:
                {
                    var view = new NodeElementView(node);
                    AddToDictionary(node, view);
                    return view;
                }
            }

            void AddToDictionary(Node node, VisualElement view)
            {
                NodeToVisualElement.AddOrUpdate(node, view);
                // nodeToVisualElement?.TryAdd(node, view);
                nodeToVisualElement[node] = view;
            }
        }
    }

    public class NodeElementView : VisualElement
    {
        private readonly Node _node;
        private const float borderWidth = 3f;
        private const float borderRadius = 8f;
        private readonly TreeEditorWindow treeEditor;
        private bool _isHovered;

        public NodeElementView(Node node)
        {
            // var controller = this.GetInterface<IOnNodeVisualElementClicked>();
            IOnNodeVisualElementClicked controller = null;
            _node = node;

            // Enhanced sizing for better readability
            style.width = 150;
            style.height = 44;
            style.minWidth = style.width;
            style.minHeight = style.height;
            style.backgroundColor = ColorPalette.AlternateBackground;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            // Add internal padding for better text spacing
            style.paddingTop = 4;
            style.paddingBottom = 4;
            style.paddingLeft = 12;
            style.paddingRight = 12;

            // Enhanced transitions for smooth color changes
            style.transitionProperty = new()
            {
                value = new()
                {
                    "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
                    "background-color"
                },
            };

            style.transitionDuration = new()
            {
                value = new() { 0.25f }
            };

            // Modern border styling
            ChangeBorderColor(Color.gray);
            ChangeBorderWidth(2f);
            ChangeBorderRadius(6f);

            // Add blue left border indicator if node is reactive
            if (node.IsReactive)
            {
                  var leftBorderBox = new VisualElement();
                  leftBorderBox.style.position = Position.Absolute;
                  leftBorderBox.style.left = 0;
                  leftBorderBox.style.top = 0;
                  leftBorderBox.style.bottom = 0;
                  leftBorderBox.style.width = 10f;
                  leftBorderBox.style.backgroundColor = ColorPalette.BlueAccent;
                  leftBorderBox.style.borderTopLeftRadius = 3f;
                  leftBorderBox.style.borderBottomLeftRadius = 3f;
                  leftBorderBox.style.borderTopRightRadius = 1f;
                  leftBorderBox.style.borderBottomRightRadius = 1f;
                      //   leftBorderBox.style.pointerEvents = PointerEvents.None;
                  Add(leftBorderBox);
            }

            var label = new Label(node.Name);
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.fontSize = 12; // Slightly larger for better readability
            label.style.unityFontStyleAndWeight = FontStyle.Bold; // Make main name bold
                label.style.flexGrow = 1;
            Add(label);

            // Add previous name label if available
            if (node.Editor.NameHistory != null && node.Editor.NameHistory.Length > 0)
            {
                var previousName = node.Editor.NameHistory[^1];

                if (!string.IsNullOrEmpty(previousName) && previousName != node.Name)
                {
                    var previousNameLabel = new Label(previousName);
                    previousNameLabel.style.position = Position.Absolute;
                    previousNameLabel.style.bottom = 3;
                    previousNameLabel.style.left = 0;
                    previousNameLabel.style.right = 0;
                    previousNameLabel.style.fontSize = 9;
                    previousNameLabel.style.color = new Color(0.55f, 0.55f, 0.55f, 0.8f); // Improved visibility
                    previousNameLabel.style.overflow = Overflow.Hidden;
                    previousNameLabel.style.textOverflow = TextOverflow.Ellipsis;
                    previousNameLabel.style.whiteSpace = WhiteSpace.NoWrap;
                    previousNameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    previousNameLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                    Add(previousNameLabel);
                }
            }
            // node.Editor.OnTreeStructureChanged += OnStructureChanged;

            // Add hover effects
            RegisterCallback<MouseEnterEvent>(e =>
            {
                _isHovered = true;
                UpdateHoverState();
            });

            RegisterCallback<MouseLeaveEvent>(e =>
            {
                _isHovered = false;
                UpdateHoverState();
            });

            RegisterCallback<ClickEvent>(e =>
            {
                controller ??= this.GetInterface<IOnNodeVisualElementClicked>();
                controller.OnNodeVisualElementClicked(node, e);
            });

            // RegisterCallback<DetachFromPanelEvent>(e =>
            // {
            //     node.Editor.OnTreeStructureChanged -= OnStructureChanged;
            // });

            // void OnStructureChanged(Node _node)
            // {
            //     if (node.IsRootNode)
            //         return;
            //
            //     Clear();
            //     Add(NodeUI.DrawNodeRecursiveElement(node));
            // }

            schedule.Execute(() =>
            {
                var color = node.Status switch
                {
                    Status.Success => ColorPalette.StatusSuccessColor,
                    Status.Failure => ColorPalette.StatusFailureColor,
                    Status.None => ColorPalette.StatusNoneColor,

                    Status.Running when node.SubStatus != SubStatus.Done => ColorPalette.StatusRunningColor,
                    Status.Running when node.SubStatus == SubStatus.Done => ColorPalette.StatusCancelledColor,

                    _ => ColorPalette.StatusDefaultColor,
                };

                // if (Application.isPlaying && Node.IsNotNullOrBroken(node) && node.Status != Status.None)
                //     color = node.IsInvalid() && node.Status != Status.Running ? Color.red : color;

                ChangeBorderColor(color);
            })
            .Every(0);
        }

        private void UpdateHoverState()
        {
            if (_isHovered)
            {
                // Brighten background on hover
                style.backgroundColor = new Color(
                    ColorPalette.AlternateBackground.r + 0.1f,
                    ColorPalette.AlternateBackground.g + 0.1f,
                    ColorPalette.AlternateBackground.b + 0.1f,
                    ColorPalette.AlternateBackground.a
                );
                // Enhance shadow on hover
                // style.boxShadow = new BoxShadowStyleValue(
                //     new StyleColor(new Color(0, 0, 0, 0.5f)),
                //     new StyleLength(new Length(0, LengthUnit.Pixel)),
                //     new StyleLength(new Length(0, LengthUnit.Pixel)),
                //     new StyleLength(new Length(6, LengthUnit.Pixel)),
                //     new StyleLength(new Length(0, LengthUnit.Pixel))
                // );
            }
            else
            {
                // Reset to normal state
                style.backgroundColor = ColorPalette.AlternateBackground;
                // style.boxShadow = new BoxShadowStyleValue(
                //     new StyleColor(new Color(0, 0, 0, 0.3f)),
                //     new StyleLength(new Length(0, LengthUnit.Pixel)),
                //     new StyleLength(new Length(0, LengthUnit.Pixel)),
                //     new StyleLength(new Length(4, LengthUnit.Pixel)),
                //     new StyleLength(new Length(0, LengthUnit.Pixel))
                // );
            }
        }

        private void ChangeBorderColor(Color color)
        {
            style.borderBottomColor = color;
            style.borderTopColor = color;
            style.borderLeftColor = color;
            style.borderRightColor = color;
        }

        private void ChangeBorderWidth(float width)
        {
            style.borderBottomWidth = width;
            style.borderTopWidth = width;
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
        }

        private void ChangeBorderRadius(float radius)
        {
            style.borderBottomLeftRadius = radius;
            style.borderBottomRightRadius = radius;
            style.borderTopLeftRadius = radius;
            style.borderTopRightRadius = radius;
        }
    }
}

#endif