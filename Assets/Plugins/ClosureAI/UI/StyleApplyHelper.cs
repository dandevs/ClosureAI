using UnityEngine;
using UnityEngine.UIElements;

namespace ClosureBT.UI
{
    public struct StyleApplyHelper
    {
        public StyleApplyHelper(VisualElement visualElement = null)
        {
            _visualElement = visualElement ?? VisualElementBuilderHelper.Element;
        }

        private VisualElement _visualElement;
        public VisualElement VisualElement => _visualElement ??= VisualElementBuilderHelper.Element;

        public StyleEnum<Align> alignContent { set => VisualElement.style.alignContent = value; }
        public StyleEnum<Align> alignItems { set => VisualElement.style.alignItems = value; }
        public StyleEnum<Align> alignSelf { set => VisualElement.style.alignSelf = value; }
        public StyleColor backgroundColor { set => VisualElement.style.backgroundColor = value; }
        public StyleBackground backgroundImage { set => VisualElement.style.backgroundImage = value; }
        public StyleBackgroundPosition backgroundPositionX { set => VisualElement.style.backgroundPositionX = value; }
        public StyleBackgroundPosition backgroundPositionY { set => VisualElement.style.backgroundPositionY = value; }
        public StyleBackgroundRepeat backgroundRepeat { set => VisualElement.style.backgroundRepeat = value; }
        public StyleBackgroundSize backgroundSize { set => VisualElement.style.backgroundSize = value; }
        public StyleColor borderBottomColor { set => VisualElement.style.borderBottomColor = value; }
        public StyleLength borderBottomLeftRadius { set => VisualElement.style.borderBottomLeftRadius = value; }
        public StyleLength borderBottomRightRadius { set => VisualElement.style.borderBottomRightRadius = value; }
        public StyleFloat borderBottomWidth { set => VisualElement.style.borderBottomWidth = value; }
        public StyleColor borderLeftColor { set => VisualElement.style.borderLeftColor = value; }
        public StyleFloat borderLeftWidth { set => VisualElement.style.borderLeftWidth = value; }
        public StyleColor borderRightColor { set => VisualElement.style.borderRightColor = value; }
        public StyleFloat borderRightWidth { set => VisualElement.style.borderRightWidth = value; }
        public StyleColor borderTopColor { set => VisualElement.style.borderTopColor = value; }
        public StyleLength borderTopLeftRadius { set => VisualElement.style.borderTopLeftRadius = value; }
        public StyleLength borderTopRightRadius { set => VisualElement.style.borderTopRightRadius = value; }
        public StyleFloat borderTopWidth { set => VisualElement.style.borderTopWidth = value; }

        // Convenience properties for setting all border sides at once
        public StyleFloat borderWidth
        {
            set
            {
                VisualElement.style.borderTopWidth = value;
                VisualElement.style.borderRightWidth = value;
                VisualElement.style.borderBottomWidth = value;
                VisualElement.style.borderLeftWidth = value;
            }
        }

        public StyleLength borderRadius
        {
            set
            {
                VisualElement.style.borderTopLeftRadius = value;
                VisualElement.style.borderTopRightRadius = value;
                VisualElement.style.borderBottomLeftRadius = value;
                VisualElement.style.borderBottomRightRadius = value;
            }
        }

        public StyleColor borderColor
        {
            set
            {
                VisualElement.style.borderTopColor = value;
                VisualElement.style.borderRightColor = value;
                VisualElement.style.borderBottomColor = value;
                VisualElement.style.borderLeftColor = value;
            }
        }

        public StyleLength bottom { set => VisualElement.style.bottom = value; }
        public StyleColor color { set => VisualElement.style.color = value; }
        public StyleCursor cursor { set => VisualElement.style.cursor = value; }
        public StyleLength flexBasis { set => VisualElement.style.flexBasis = value; }
        public StyleEnum<FlexDirection> flexDirection { set => VisualElement.style.flexDirection = value; }
        public StyleFloat flexGrow { set => VisualElement.style.flexGrow = value; }
        public StyleFloat flexShrink { set => VisualElement.style.flexShrink = value; }
        public StyleEnum<Wrap> flexWrap { set => VisualElement.style.flexWrap = value; }
        public StyleLength fontSize { set => VisualElement.style.fontSize = value; }
        public StyleLength height { set => VisualElement.style.height = value; }
        public StyleEnum<Justify> justifyContent { set => VisualElement.style.justifyContent = value; }
        public StyleLength left { set => VisualElement.style.left = value; }
        public StyleLength letterSpacing { set => VisualElement.style.letterSpacing = value; }
        public StyleLength marginBottom { set => VisualElement.style.marginBottom = value; }
        public StyleLength marginLeft { set => VisualElement.style.marginLeft = value; }
        public StyleLength marginRight { set => VisualElement.style.marginRight = value; }
        public StyleLength marginTop { set => VisualElement.style.marginTop = value; }

        // Convenience property for setting all margin sides at once
        public StyleLength margin
        {
            set
            {
                VisualElement.style.marginTop = value;
                VisualElement.style.marginRight = value;
                VisualElement.style.marginBottom = value;
                VisualElement.style.marginLeft = value;
            }
        }

        public StyleLength maxHeight { set => VisualElement.style.maxHeight = value; }
        public StyleLength maxWidth { set => VisualElement.style.maxWidth = value; }
        public StyleLength minHeight { set => VisualElement.style.minHeight = value; }
        public StyleLength minWidth { set => VisualElement.style.minWidth = value; }
        public StyleFloat opacity { set => VisualElement.style.opacity = value; }
        public StyleEnum<Overflow> overflow { set => VisualElement.style.overflow = value; }
        public StyleLength paddingBottom { set => VisualElement.style.paddingBottom = value; }
        public StyleLength paddingLeft { set => VisualElement.style.paddingLeft = value; }
        public StyleLength paddingRight { set => VisualElement.style.paddingRight = value; }
        public StyleLength paddingTop { set => VisualElement.style.paddingTop = value; }

        // Convenience property for setting all padding sides at once
        public StyleLength padding
        {
            set
            {
                VisualElement.style.paddingTop = value;
                VisualElement.style.paddingRight = value;
                VisualElement.style.paddingBottom = value;
                VisualElement.style.paddingLeft = value;
            }
        }

        public StyleEnum<Position> position { set => VisualElement.style.position = value; }
        public StyleLength right { set => VisualElement.style.right = value; }
        public StyleRotate rotate { set => VisualElement.style.rotate = value; }
        public StyleScale scale { set => VisualElement.style.scale = value; }
        public StyleEnum<TextOverflow> textOverflow { set => VisualElement.style.textOverflow = value; }
        public StyleLength top { set => VisualElement.style.top = value; }
        public StyleTransformOrigin transformOrigin { set => VisualElement.style.transformOrigin = value; }
        public StyleTranslate translate { set => VisualElement.style.translate = value; }
        public StyleEnum<Visibility> visibility { set => VisualElement.style.visibility = value; }
        public StyleEnum<WhiteSpace> whiteSpace { set => VisualElement.style.whiteSpace = value; }
        public StyleLength width { set => VisualElement.style.width = value; }
        public StyleEnum<FontStyle> unityFontStyleAndWeight { set => VisualElement.style.unityFontStyleAndWeight = value; }
        public StyleEnum<TextAnchor> unityTextAlign { set => VisualElement.style.unityTextAlign = value; }
        public StyleFloat unityTextOutlineWidth { set => VisualElement.style.unityTextOutlineWidth = value; }
        public StyleColor unityTextOutlineColor { set => VisualElement.style.unityTextOutlineColor = value; }
        public StyleEnum<DisplayStyle> display { set => VisualElement.style.display = value; }
    }
}
