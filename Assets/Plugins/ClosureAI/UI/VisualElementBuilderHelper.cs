using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ClosureBT.UI
{
    public static class VisualElementBuilderHelper
    {
        private static readonly Stack<VisualElement> _stack = new();
        public static VisualElement Element => _stack.TryPeek(out var element) ? element : null;
        public static IVisualElementScheduler Scheduler => Element.schedule;

        public static T E<T>(Action<T> setup) where T : VisualElement, new()
        {
            var element = new T();
            Element?.Add(element);

            _stack.Push(element);
            setup.Invoke(element);
            _stack.Pop();

            return element;
        }

        public static T E<T>(Action setup) where T : VisualElement, new()
        {
            return E<T>(_ => setup());
        }

        public static T E<T>(T element, Action<T> setup = null) where T : VisualElement
        {
            Element?.Add(element);

            _stack.Push(element);
            setup?.Invoke(element);
            _stack.Pop();

            return element;
        }

        public static void FlexGap(float gap)
        {
            if (Element is FlexGapView flexGapView)
            {
                flexGapView.Gap = gap;
            }
        }

        public static T Parent<T>() where T : VisualElement
        {
            var current = Element.parent;

            while (current != null)
            {
                if (current is T t)
                    return t;
                else
                    current = current.parent;
            }

            return null;
        }

        public static void Style(StyleApplyHelper style)
        {
        }
    }

    public class FlexGapView : VisualElement
    {
        private float _gap;
        private int _previousCount;

        public float Gap
        {
            get => _gap;
            set
            {
                _gap = value;
                _previousCount = 0;
                ApplyMarginsForGap(0, childCount - 1);
            }
        }

        protected FlexGapView()
        {
            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (childCount > _previousCount)
                {
                    ApplyMarginsForGap(_previousCount, childCount - 1);
                    _previousCount = childCount;
                }
            });
        }

        private void ApplyMarginsForGap(int startIndex, int endIndex)
        {
            switch (style.flexDirection.value)
            {
                case FlexDirection.Row:
                    for (var i = startIndex; i < endIndex; i++)
                        this[i].style.marginRight = Gap;

                    break;

                case FlexDirection.RowReverse:
                    for (var i = startIndex; i < endIndex; i++)
                        this[i].style.marginLeft = Gap;

                    break;

                case FlexDirection.Column:
                    for (var i = startIndex; i < endIndex; i++)
                        this[i].style.marginBottom = Gap;

                    break;

                case FlexDirection.ColumnReverse:
                    for (var i = startIndex; i < endIndex; i++)
                        this[i].style.marginTop = Gap;

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class FlexRow : FlexGapView
    {
        public FlexRow()
        {
            style.flexDirection = FlexDirection.Row;
        }

        public FlexRow(float gap)
        {
            style.flexDirection = FlexDirection.Row;
            Gap = gap;
        }
    }

    public class FlexColumn : FlexGapView
    {
        public FlexColumn()
        {
            style.flexDirection = FlexDirection.Column;
        }

        public FlexColumn(float gap)
        {
            style.flexDirection = FlexDirection.Column;
            Gap = gap;
        }
    }
}
