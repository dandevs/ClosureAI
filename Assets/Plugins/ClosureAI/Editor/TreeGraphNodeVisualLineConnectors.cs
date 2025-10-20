#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using ClosureAI.Editor.UI;
using ClosureAI.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using static ClosureAI.AI;

namespace ClosureAI.Editor
{
    // TODO: Clean this up
    public class NodeLineConnectionDrawer
    {
        public readonly VisualElement RootVisualElement;
        public readonly Node RootNode;
        public readonly List<LineConnection> Connections = new();
        private readonly Dictionary<Node, VisualElement> _nodeToVisualElement;

        public NodeLineConnectionDrawer(VisualElement rootVisualElement, Node rootNode, Dictionary<Node, VisualElement> nodeToVisualElement)
        {
            RootVisualElement = rootVisualElement;
            RootNode = rootNode;
            _nodeToVisualElement = nodeToVisualElement;
            PopulateConnectionsFromNode(RootNode);
        }

        private void PopulateConnectionsFromNode(Node node)
        {
            if (node is CompositeNode composite)
            {
                if (node is YieldNode)
                    return;

                var compositeVE = _nodeToVisualElement[node];

                int childIndex = 0;
                foreach (var child in composite.Children)
                {
                    var childVE = _nodeToVisualElement[child];
                    var _child = child;
                    bool isFirstChild = (childIndex == 0);

                    var connection = new LineConnection(RootVisualElement, compositeVE, childVE, () =>
                    {
                        return _child.Status switch
                        {
                            Status.Failure => ColorPalette.StatusFailureColor,
                            Status.Success => ColorPalette.StatusSuccessColor,
                            Status.Running when _child.SubStatus != SubStatus.Done => ColorPalette.StatusRunningColor,
                            Status.Running when _child.SubStatus == SubStatus.Done => ColorPalette.StatusCancelledColor,
                            _ => ColorPalette.StatusDefaultColor,
                        };
                    }, isFirstChild);

                    Connections.Add(connection);

                    if (child is CompositeNode or DecoratorNode)
                        PopulateConnectionsFromNode(child);

                    childIndex++;
                }
            }
            else if (node is DecoratorNode decorator)
            {
                PopulateConnectionsFromNode(decorator.Child);
            }
            // else
            //     Debug.LogError($"Node {node.Name} is neither a type of Composite or Decorator");
        }

        public void Draw(Painter2D painter)
        {
            // foreach (var connection in Connections)
            //     connection.Draw(painter);

            for (var i = Connections.Count - 1; i >= 0; i--)
                Connections[i].Draw(painter);
        }
    }

    public class LineConnection
    {
        public readonly VisualElement FromElement;
        public readonly VisualElement ToElement;
        public readonly VisualElement RootElement;
        public readonly Func<Color> GetColor;
        public readonly bool IsFirstChild;

        public LineConnection(VisualElement rootElement, VisualElement fromElement, VisualElement toElement, Func<Color> getColor, bool isFirstChild = false)
        {
            RootElement = rootElement;
            FromElement = fromElement;
            ToElement = toElement;
            GetColor = getColor;
            IsFirstChild = isFirstChild;
        }

        public void Draw(Painter2D painter)
        {
            var point = FromElement.ChangeCoordinatesTo(RootElement, Vector2.zero);

            var startPoint = MiddleRight(FromElement);
            var endPoint = MiddleLeft(ToElement);
            var centerPointA = CenterOfRightLeftEdges(FromElement, ToElement).WithY(startPoint.y);
            var centerPointB = centerPointA.WithY(endPoint.y);

            // Get the status color (from ColorPalette, already optimized)
            var statusColor = GetColor();

            painter.BeginPath();
            painter.strokeColor = statusColor;
            painter.lineWidth = 2.5f; // Slightly thinner for elegance

            // Start from the right middle of the parent node
            painter.MoveTo(startPoint);

            // Calculate the corner radius for smooth transitions
            const float cornerRadius = 18f;

            // Horizontal line from start
            float horizontalDistance = centerPointA.x - startPoint.x;
            if (Mathf.Abs(horizontalDistance) > cornerRadius)
            {
                // For first child, use straight line at first corner to avoid gap
                if (IsFirstChild)
                {
                    // Draw straight to the vertical line position
                    painter.LineTo(new Vector2(centerPointA.x, startPoint.y));

                    // Vertical line, minus corner radius at bottom
                    float verticalDistance = centerPointB.y - startPoint.y;
                    if (Mathf.Abs(verticalDistance) > cornerRadius)
                    {
                        painter.LineTo(new Vector2(centerPointA.x, centerPointB.y - Mathf.Sign(verticalDistance) * cornerRadius));

                        // Second rounded corner (going from vertical to horizontal)
                        Vector2 controlPoint2 = new Vector2(centerPointA.x, centerPointB.y);
                        Vector2 cornerEnd2 = new Vector2(centerPointA.x + cornerRadius, centerPointB.y);
                        painter.BezierCurveTo(controlPoint2, controlPoint2, cornerEnd2);

                        // Final horizontal line to end point
                        painter.LineTo(endPoint);
                    }
                    else
                    {
                        // Distance too small for corner, use straight line
                        painter.LineTo(new Vector2(centerPointA.x, centerPointB.y));
                        painter.LineTo(endPoint);
                    }
                }
                else
                {
                    // For non-first children, use rounded corners
                    painter.LineTo(new Vector2(centerPointA.x - cornerRadius, startPoint.y));

                    // First rounded corner (going from horizontal to vertical)
                    Vector2 controlPoint1 = new Vector2(centerPointA.x, startPoint.y);
                    Vector2 cornerEnd1 = new Vector2(centerPointA.x, startPoint.y + Mathf.Sign(centerPointB.y - startPoint.y) * cornerRadius);
                    painter.BezierCurveTo(controlPoint1, controlPoint1, cornerEnd1);

                    // Vertical line, minus corner radii at both ends
                    float verticalDistance = centerPointB.y - cornerEnd1.y;
                    if (Mathf.Abs(verticalDistance) > cornerRadius)
                    {
                        painter.LineTo(new Vector2(centerPointA.x, centerPointB.y - Mathf.Sign(verticalDistance) * cornerRadius));

                        // Second rounded corner (going from vertical to horizontal)
                        Vector2 controlPoint2 = new Vector2(centerPointA.x, centerPointB.y);
                        Vector2 cornerEnd2 = new Vector2(centerPointA.x + cornerRadius, centerPointB.y);
                        painter.BezierCurveTo(controlPoint2, controlPoint2, cornerEnd2);

                        // Final horizontal line to end point
                        painter.LineTo(endPoint);
                    }
                    else
                    {
                        // Distance too small for corner, use simpler curve
                        painter.LineTo(new Vector2(centerPointA.x, centerPointB.y));
                        painter.LineTo(endPoint);
                    }
                }
            }
            else
            {
                // Too close for rounded corners, use straight lines
                painter.LineTo(centerPointA);
                painter.LineTo(centerPointB);
                painter.LineTo(endPoint);
            }

            painter.Stroke();
        }

        public Vector2 MiddleRight(VisualElement element)
        {
            return element.ChangeCoordinatesTo(RootElement, new Vector2(element.localBound.width, element.localBound.height / 2f));
        }

        public Vector2 MiddleLeft(VisualElement element)
        {
            return element.ChangeCoordinatesTo(RootElement, new Vector2(0f, element.localBound.height / 2f));
        }

        public Vector2 MiddleBottom(VisualElement element)
        {
            return element.ChangeCoordinatesTo(RootElement, Vector2.zero);
        }

        public Vector2 CenterOfRightLeftEdges(VisualElement rightEdge, VisualElement leftEdge)
        {
            return (MiddleRight(rightEdge) + MiddleLeft(leftEdge)) / 2f;
        }
    }
}

#endif