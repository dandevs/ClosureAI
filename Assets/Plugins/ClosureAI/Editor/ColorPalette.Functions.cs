using UnityEngine;

namespace ClosureAI.Editor
{
    public static partial class ColorPalette
    {
        // Helper method to extract background color from style
        private static Color GetBackgroundColor(string styleName)
        {
            try
            {
                var skin = InspectorSkin;
                if (skin == null)
                    return IsDarkMode ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);

                var style = skin.FindStyle(styleName);
                if (style?.normal.background != null)
                {
                    return SampleTextureColor(style.normal.background);
                }
            }
            catch
            {
                // EditorStyles might not be initialized yet during domain reload
            }

            // Fallback to Unity's default colors
            return IsDarkMode ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);
        }

        // Sample the center pixel of a texture to get its representative color
        private static Color SampleTextureColor(Texture2D texture)
        {
            if (texture == null) return Color.clear;

            try
            {
                return texture.GetPixel(texture.width / 2, texture.height / 2);
            }
            catch
            {
                // Some textures might not be readable
                return IsDarkMode ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);
            }
        }
    }
}
