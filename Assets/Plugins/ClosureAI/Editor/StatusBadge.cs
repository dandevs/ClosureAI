#if UNITASK_INSTALLED
using UnityEngine;
using UnityEngine.UIElements;
using static ClosureAI.UI.VisualElementBuilderHelper;

namespace ClosureAI.Editor.UI
{
    public class StatusBadge : VisualElement
    {
        private Label statusLabel;
        private VisualElement badgeContainer;

        public string Text
        {
            get => statusLabel?.text ?? string.Empty;
            set
            {
                if (statusLabel != null)
                    statusLabel.text = value;
            }
        }

        public Color BackgroundColor
        {
            get => badgeContainer?.style.backgroundColor.value ?? Color.clear;
            set
            {
                if (badgeContainer != null)
                    badgeContainer.style.backgroundColor = value;
            }
        }

        public Color TextColor
        {
            get => statusLabel?.style.color.value ?? Color.white;
            set
            {
                if (statusLabel != null)
                    statusLabel.style.color = value;
            }
        }

        public StatusBadge() => E(this, _ =>
        {
            Style(new()
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginTop = 8,
            });

            E<VisualElement>(statusBadge =>
            {
                badgeContainer = statusBadge;

                Style(new()
                {
                    borderRadius = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 5,
                    paddingBottom = 5,
                    minWidth = 80,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(1, 1, 1, 0.1f),
                    borderBottomColor = new Color(0, 0, 0, 0.2f),
                    borderLeftColor = new Color(1, 1, 1, 0.05f),
                    borderRightColor = new Color(0, 0, 0, 0.1f),
                });

                E<Label>(label =>
                {
                    statusLabel = label;

                    Style(new()
                    {
                        fontSize = 9,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        letterSpacing = 0.3f,
                    });
                });
            });
        });
    }
}

#endif