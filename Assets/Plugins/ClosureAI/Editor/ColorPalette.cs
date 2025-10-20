using UnityEngine;
using UnityEditor;

namespace ClosureAI.Editor
{
    public static partial class ColorPalette
    {
        public static bool IsDarkMode => EditorGUIUtility.isProSkin;

        private static GUISkin InspectorSkin
        {
            get
            {
                try
                {
                    return EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                }
                catch
                {
                    // EditorGUIUtility might not be ready during domain reload
                    return null;
                }
            }
        }

        // Base color constants for consistency
        public static Color DarkBackground => new Color(0.12f, 0.12f, 0.12f, 1f);
        public static Color MediumDarkBackground => new Color(0.15f, 0.15f, 0.15f, 1f);
        public static Color DarkerBackground => new Color(0.1f, 0.1f, 0.1f, 1f);
        public static Color MediumBackground => new Color(0.18f, 0.18f, 0.18f, 1f);

        public static Color BlueAccent => new Color(0.3f, 0.6f, 1f, 1f);
        public static Color BlueAccentTransparent => new Color(0.3f, 0.6f, 1f, 0.6f);
        public static Color OrangeAccent => new Color(1f, 0.6f, 0.2f, 1f);
        public static Color GreenAccent => new Color(0.2f, 0.8f, 0.2f, 1f);

        public static Color VeryLightGrayText => new Color(0.9f, 0.9f, 0.9f, 1f);
        public static Color LightGrayText => new Color(0.8f, 0.8f, 0.8f, 1f);
        public static Color MediumGrayText => new Color(0.7f, 0.7f, 0.7f, 1f);
        public static Color DimGrayText => new Color(0.6f, 0.6f, 0.6f, 1f);
        public static Color SubtleBorder => new Color(0.2f, 0.2f, 0.2f, 1f);
        public static Color MediumBorder => new Color(0.3f, 0.3f, 0.3f, 1f);

        // Main Inspector background colors
        public static Color WindowBackground => GetBackgroundColor("window");
        public static Color BoxBackground => GetBackgroundColor("box");
        public static Color HelpBoxBackground => GetBackgroundColor("HelpBox");
        public static Color GroupBoxBackground => GetBackgroundColor("GroupBox");
        public static Color ToolbarBackground => GetBackgroundColor("Toolbar");
        public static Color ScrollViewBackground => GetBackgroundColor("ScrollView");

        // Inspector-specific backgrounds
        public static Color InspectorBackground
        {
            get
            {
                try
                {
                    if (EditorStyles.inspectorDefaultMargins?.normal.background != null)
                        return SampleTextureColor(EditorStyles.inspectorDefaultMargins.normal.background);
                }
                catch
                {
                    // EditorStyles not ready during domain reload
                }
                return IsDarkMode ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);
            }
        }

        public static Color InspectorTitleBackground => GetBackgroundColor("IN TitleText");
        public static Color PropertyFieldBackground => GetBackgroundColor("TextField");
        public static Color FoldoutBackground
        {
            get
            {
                try
                {
                    if (EditorStyles.foldout?.normal.background != null)
                        return SampleTextureColor(EditorStyles.foldout.normal.background);
                }
                catch
                {
                    // EditorStyles not ready during domain reload
                }
                return WindowBackground;
            }
        }

        // Alternative backgrounds
        public static Color AlternateBackground => IsDarkMode
            ? new Color(0.25f, 0.25f, 0.25f)
            : new Color(0.82f, 0.82f, 0.82f);

        public static Color HeaderBackground => IsDarkMode
            ? new Color(0.18f, 0.18f, 0.18f)
            : new Color(0.85f, 0.85f, 0.85f);

        // Snapshot Index Controller colors - now using base constants
        public static Color SnapshotHistoryBackground => new Color(0.15f, 0.12f, 0.08f, 1f); // Slightly warmer tone for history
        public static Color SnapshotLiveBackground => DarkBackground;

        public static Color SnapshotHistoryBorder => new Color(1f, 0.6f, 0.2f, 0.3f); // Transparent orange
        public static Color SnapshotLiveBorder => SubtleBorder;

        public static Color SnapshotHistoryIndicator => OrangeAccent;
        public static Color SnapshotLiveIndicator => GreenAccent;

        public static Color SnapshotStatusText => LightGrayText;
        public static Color SnapshotInfoText => DimGrayText;
        public static Color SnapshotIndexText => VeryLightGrayText;

        public static Color SnapshotIndexBackground => MediumBackground;
        public static Color SnapshotButtonBorder => new Color(0.3f, 0.6f, 0.3f, 1f); // Green accent for buttons

        // Node Inspector colors - consolidated from NodeInspectorView
        public static Color NodeInspectorBackground => DarkBackground;
        public static Color NodeInspectorSectionBackground => MediumDarkBackground;
        public static Color NodeInspectorPropertyBackground => new Color(0.1f, 0.1f, 0.1f, 0.5f);
        public static Color NodeInspectorAccentBorder => BlueAccent;
        public static Color NodeInspectorSubtleBorder => BlueAccentTransparent;

        // Status colors for node borders and connections (brighter, more vibrant)
        public static Color StatusSuccessColor => new Color(0.3f, 0.9f, 0.3f, 1f);      // Brighter green
        public static Color StatusFailureColor => new Color(0.95f, 0.3f, 0.3f, 1f);    // Brighter red
        public static Color StatusRunningColor => new Color(1f, 0.8f, 0.2f, 1f);       // Brighter yellow
        public static Color StatusCancelledColor => new Color(0.922f, 0.502f, 0.259f);          // Brighter blue
        public static Color StatusDefaultColor => new Color(0.5f, 0.5f, 0.5f, 1f);     // Neutral gray
        public static Color StatusNoneColor => new Color(0.5f, 0.5f, 0.5f, 1f);        // Neutral gray
    }
}
