#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static ClosureAI.AI;
using Object = UnityEngine.Object;

namespace ClosureAI.Editor.UI
{
    public class NodeVisualizerView : VisualElement, IOnNodeVisualElementClicked
    {
        private Node _node;
        private readonly IVisualElementController _controller;
        private static Texture2D _gridBG;
        private readonly Dictionary<Node, VisualElement> _nodeToVisualElement = new();
        private NodeLineConnectionDrawer _nodeLineConnectionDrawer = null;

        public NodeVisualizerView(Func<Node> getNode, IVisualElementController controller)
        {
            _node = getNode();
            _controller = controller;
            var previousNode = _node;

            schedule.Execute(() =>
            {
                _node = getNode();

                if (_node != previousNode)
                {
                    OnNodeChanged();
                    previousNode = _node;
                }

                // MarkDirtyRepaint();
            })
            .Every(0);

            NodeHistoryTracker.OnSnapshotIndexChanged += MarkDirtyRepaint;

            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Object.DestroyImmediate(_gridBG);
                NodeHistoryTracker.OnSnapshotIndexChanged -= MarkDirtyRepaint;
            });

            // schedule.Execute(() =>
            // {
            //     if (!_gridBG)
            //     {
            //         _gridBG = DrawGridPattern.CreateGridTexture();
            //         style.backgroundImage = Background.FromTexture2D(_gridBG);
            //         style.backgroundSize = new BackgroundSize(_gridBG.width, _gridBG.height);
            //         style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            //     }
            //
            //     style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, -scrollOffset.x);
            //     style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, -scrollOffset.y);
            // })
            // .Every(0);

            OnNodeChanged();

            generateVisualContent += mgc =>
            {
                if (Node.IsInvalid(_node))
                    return;

                _nodeLineConnectionDrawer.Draw(mgc.painter2D);
            };
        }

        private void OnNodeChanged()
        {
            Clear();

            if (Node.IsInvalid(_node))
                return;

            _node.Editor.OnTreeStructureChanged -= OnTreeStructureChanged;
            _node.Editor.OnTreeStructureChanged += OnTreeStructureChanged;

            _node.RootEditor.OnChildrenStatusChanged -= OnNodeStatusChanged;
            _node.RootEditor.OnChildrenStatusChanged += OnNodeStatusChanged;

            _nodeToVisualElement.Clear();
            var result = NodeUI.DrawNodeRecursiveElement(_node, _nodeToVisualElement);
            _nodeLineConnectionDrawer = new(this, _node, _nodeToVisualElement);

            if (result != null)
                Add(result);
        }

        private void OnNodeStatusChanged(Node node)
        {
            MarkDirtyRepaint();
        }

        public void OnNodeVisualElementClicked(Node node, ClickEvent e)
        {
            _controller.OnNodeVisualElementClicked(node, e);
        }

        void OnTreeStructureChanged(Node node)
        {
            OnNodeChanged();
        }
    }
}

#endif