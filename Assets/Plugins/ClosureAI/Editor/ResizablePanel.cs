#if UNITASK_INSTALLED
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ClosureAI.Editor.UI
{
    /// <summary>
    /// A resize bar can be dragged to change the panel's width or height.
    /// </summary>
    public class ResizablePanel : VisualElement
    {
        private const float DefaultWidth = 400f;
        private const float DefaultHeight = 300f;
        private const float MinWidth = 200f;
        private const float MaxWidth = 800f;
        private const float MinHeight = 150f;
        private const float MaxHeight = 600f;
        private const float ResizeBarWidth = 6f;
        private const float TransitionDuration = 0.15f;

        private readonly VisualElement _resizeBar;
        private readonly VisualElement _resizeHandle;
        private readonly VisualElement _contentContainer;
        private readonly BarPosition _barPosition;

        private bool _dragging;
        private bool _hovering;
        private float _startMouseX;
        private float _startMouseY;
        private float _startWidth;
        private float _startHeight;

        public ResizablePanel(BarPosition barPosition, VisualElement content)
        {
            name = "resizable-panel";
            _barPosition = barPosition;

            // Panel styling
            style.flexShrink = 0;
            style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f); // Dark background

            // Set initial size based on bar position
            if (_barPosition is BarPosition.Left or BarPosition.Right)
            {
                style.width = DefaultWidth;
                style.height = new StyleLength(StyleKeyword.Auto);
            }
            else // Top or Bottom
            {
                style.width = new StyleLength(StyleKeyword.Auto);
                style.height = DefaultHeight;
            }

            // Content container with appropriate padding based on bar position
            _contentContainer = new VisualElement
            {
                name = "content-container",
                style = { flexGrow = 1 },
            };

            SetContentPadding();

            content.style.flexGrow = 1;
            _contentContainer.Add(content);
            Add(_contentContainer);

            // Create resize bar with position-specific styling
            _resizeBar = new VisualElement { name = "resize-bar" };
            SetResizeBarProperties();

            // Resize handle - visual indicator
            _resizeHandle = new VisualElement { name = "resize-handle" };
            SetResizeHandleProperties();

            _resizeBar.Add(_resizeHandle);
            Add(_resizeBar);

            // Event callbacks
            _resizeBar.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _resizeBar.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _resizeBar.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _resizeBar.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            _resizeBar.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            // Add USS classes for additional styling flexibility
            AddToClassList("resizable-panel");
            AddToClassList($"resizable-panel--{barPosition.ToString().ToLower()}");
            _resizeBar.AddToClassList("resize-bar");
            _resizeHandle.AddToClassList("resize-handle");
            _contentContainer.AddToClassList("content-container");
        }

        private void SetContentPadding()
        {
            switch (_barPosition)
            {
                case BarPosition.Left:
                    _contentContainer.style.paddingLeft = ResizeBarWidth;
                    break;
                case BarPosition.Right:
                    _contentContainer.style.paddingRight = ResizeBarWidth;
                    break;
                case BarPosition.Top:
                    _contentContainer.style.paddingTop = ResizeBarWidth;
                    break;
                case BarPosition.Bottom:
                    _contentContainer.style.paddingBottom = ResizeBarWidth;
                    break;
            }
        }

        private void SetResizeBarProperties()
        {
            _resizeBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            _resizeBar.style.position = Position.Absolute;

            // Set up transitions
            _resizeBar.style.transitionProperty = new List<StylePropertyName>
            {
                new("background-color"),
                new("width"),
                new("height")
            };
            _resizeBar.style.transitionDuration = new List<TimeValue>
            {
                new(TransitionDuration),
                new(TransitionDuration),
                new(TransitionDuration)
            };
            _resizeBar.style.transitionTimingFunction = new List<EasingFunction>
            {
                EasingMode.EaseInOut,
                EasingMode.EaseInOut,
                EasingMode.EaseInOut
            };

            switch (_barPosition)
            {
                case BarPosition.Left:
                    _resizeBar.style.width = ResizeBarWidth;
                    _resizeBar.style.top = 0;
                    _resizeBar.style.bottom = 0;
                    _resizeBar.style.left = 0;
                    break;
                case BarPosition.Right:
                    _resizeBar.style.width = ResizeBarWidth;
                    _resizeBar.style.top = 0;
                    _resizeBar.style.bottom = 0;
                    _resizeBar.style.right = 0;
                    break;
                case BarPosition.Top:
                    _resizeBar.style.height = ResizeBarWidth;
                    _resizeBar.style.left = 0;
                    _resizeBar.style.right = 0;
                    _resizeBar.style.top = 0;
                    break;
                case BarPosition.Bottom:
                    _resizeBar.style.height = ResizeBarWidth;
                    _resizeBar.style.left = 0;
                    _resizeBar.style.right = 0;
                    _resizeBar.style.bottom = 0;
                    break;
            }
        }

        private void SetResizeHandleProperties()
        {
            _resizeHandle.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            _resizeHandle.style.position = Position.Absolute;

            _resizeHandle.style.transitionProperty = new List<StylePropertyName>
            {
                new("background-color"),
                new("height"),
                new("width")
            };
            _resizeHandle.style.transitionDuration = new List<TimeValue>
            {
                new(TransitionDuration),
                new(TransitionDuration),
                new(TransitionDuration)
            };

            switch (_barPosition)
            {
                case BarPosition.Left:
                case BarPosition.Right:
                    // Vertical handle for horizontal resize
                    _resizeHandle.style.width = 2;
                    _resizeHandle.style.height = 20;
                    _resizeHandle.style.left = 2;
                    _resizeHandle.style.top = new Length(50, LengthUnit.Percent);
                    _resizeHandle.style.translate = new Translate(0, new Length(-50, LengthUnit.Percent));
                    break;
                case BarPosition.Top:
                case BarPosition.Bottom:
                    // Horizontal handle for vertical resize
                    _resizeHandle.style.width = 20;
                    _resizeHandle.style.height = 2;
                    _resizeHandle.style.top = 2;
                    _resizeHandle.style.left = new Length(50, LengthUnit.Percent);
                    _resizeHandle.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
                    break;
            }
        }

        // -----------------------------------------------------------------------------------------

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            if (_dragging) return;

            _hovering = true;
            UpdateVisualState();
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (_dragging) return;

            _hovering = false;
            UpdateVisualState();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) // left mouse only
                return;

            _dragging = true;
            _startMouseX = evt.position.x;
            _startMouseY = evt.position.y;
            _startWidth = resolvedStyle.width;
            _startHeight = resolvedStyle.height;
            _resizeBar.CapturePointer(evt.pointerId);

            UpdateVisualState();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging) return;

            switch (_barPosition)
            {
                case BarPosition.Left:
                    {
                        var delta = evt.position.x - _startMouseX;
                        var newSize = Mathf.Clamp(_startWidth - delta, MinWidth, MaxWidth);
                        style.width = newSize;
                    }
                    break;
                case BarPosition.Right:
                    {
                        var delta = evt.position.x - _startMouseX;
                        var newSize = Mathf.Clamp(_startWidth + delta, MinWidth, MaxWidth);
                        style.width = newSize;
                    }
                    break;
                case BarPosition.Top:
                    {
                        var delta = evt.position.y - _startMouseY;
                        var newSize = Mathf.Clamp(_startHeight - delta, MinHeight, MaxHeight);
                        style.height = newSize;
                    }
                    break;
                case BarPosition.Bottom:
                    {
                        var delta = evt.position.y - _startMouseY;
                        var newSize = Mathf.Clamp(_startHeight + delta, MinHeight, MaxHeight);
                        style.height = newSize;
                    }
                    break;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_dragging) return;

            _dragging = false;
            _resizeBar.ReleasePointer(evt.pointerId);

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            var isHorizontal = _barPosition == BarPosition.Left || _barPosition == BarPosition.Right;

            if (_dragging)
            {
                // Active dragging state
                _resizeBar.style.backgroundColor = new Color(0.4f, 0.6f, 1f, 1f); // Blue accent
                _resizeHandle.style.backgroundColor = new Color(1f, 1f, 1f, 1f); // White

                if (isHorizontal)
                {
                    _resizeBar.style.width = ResizeBarWidth + 2;
                    _resizeHandle.style.height = 30;
                }
                else
                {
                    _resizeBar.style.height = ResizeBarWidth + 2;
                    _resizeHandle.style.width = 30;
                }
            }
            else if (_hovering)
            {
                // Hover state
                _resizeBar.style.backgroundColor = new Color(0.45f, 0.45f, 0.45f, 1f); // Lighter
                _resizeHandle.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Lighter

                if (isHorizontal)
                {
                    _resizeBar.style.width = ResizeBarWidth + 1;
                    _resizeHandle.style.height = 25;
                }
                else
                {
                    _resizeBar.style.height = ResizeBarWidth + 1;
                    _resizeHandle.style.width = 25;
                }
            }
            else
            {
                // Default state
                _resizeBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                _resizeHandle.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

                if (isHorizontal)
                {
                    _resizeBar.style.width = ResizeBarWidth;
                    _resizeHandle.style.height = 20;
                }
                else
                {
                    _resizeBar.style.height = ResizeBarWidth;
                    _resizeHandle.style.width = 20;
                }
            }
        }

        public enum BarPosition
        {
            Left,
            Top,
            Right,
            Bottom,
        }
    }
}

#endif