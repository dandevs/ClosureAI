using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClosureAI.UI;
using static ClosureAI.UI.VisualElementBuilderHelper;
using UnityEditor.Build; // added for NamedBuildTarget

namespace ClosureAI.Editor
{
    /// <summary>
    /// EditorWindow that prompts the user to install UniTask if it's not detected.
    /// Automatically opens on first load when UNITASK_INSTALLED define is missing.
    /// </summary>
    [InitializeOnLoad]
    public class UniTaskInstallerWindow : EditorWindow
    {
        private const string PREF_KEY = "ClosureAI";
        private const string INSTALLER_DISMISSED_KEY = "HasSeenUniTaskInstaller";
        private const string UNITASK_GITHUB_URL = "https://github.com/Cysharp/UniTask/releases/tag/2.5.10";
        internal const string UNITASK_INSTALLED_DEFINE = "UNITASK_INSTALLED";

        private static bool hasShownThisSession = false;

        // Static constructor - called when Unity loads and on every domain reload
        static UniTaskInstallerWindow()
        {
            // Delay call to ensure Unity is fully initialized
            EditorApplication.delayCall += CheckAndShowInstaller;
        }

        private static void CheckAndShowInstaller()
        {
            // Only check once per session to avoid showing multiple times during the same session
            if (hasShownThisSession)
                return;

            hasShownThisSession = true;

            // Check if UNITASK_INSTALLED define exists
            if (IsUniTaskInstalled())
            {
                // UniTask is installed, mark as seen so we don't show this again
                EditorPrefs.SetBool(Key(INSTALLER_DISMISSED_KEY), true);
                return;
            }

            // Check if user has permanently dismissed the installer
            bool hasSeenInstaller = EditorPrefs.GetBool(Key(INSTALLER_DISMISSED_KEY), false);
            if (hasSeenInstaller)
                return;

            ShowWindow();
        }

        private static bool IsUniTaskInstalled()
        {
            // Check if UNITASK_INSTALLED define exists
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            return defines.Contains(UNITASK_INSTALLED_DEFINE);
        }

        [MenuItem("Tools/ClosureAI/Show UniTask Installer")]
        public static void ShowWindow()
        {
            var window = GetWindow<UniTaskInstallerWindow>(true, "ClosureAI Setup", true);
            window.minSize = new Vector2(450, 320);
            window.maxSize = new Vector2(450, 320);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            E(rootVisualElement, _ =>
            {
                Style(new()
                {
                    backgroundColor = ColorPalette.WindowBackground,
                });

                // Header section
                E<VisualElement>(header =>
                {
                    Style(new()
                    {
                        backgroundColor = ColorPalette.HeaderBackground,
                        paddingLeft = 16,
                        paddingRight = 16,
                        paddingTop = 12,
                        paddingBottom = 12,
                        borderBottomWidth = 1,
                        borderBottomColor = ColorPalette.MediumBorder,
                    });

                    E<Label>(title =>
                    {
                        title.text = "UniTask Installation Required";
                        Style(new()
                        {
                            fontSize = 14,
                            color = ColorPalette.VeryLightGrayText,
                            unityFontStyleAndWeight = FontStyle.Bold,
                        });
                    });
                });

                // Content section
                E<VisualElement>(content =>
                {
                    Style(new()
                    {
                        paddingLeft = 16,
                        paddingRight = 16,
                        paddingTop = 16,
                        paddingBottom = 16,
                        flexGrow = 1,
                    });

                    // Description
                    E<Label>(desc =>
                    {
                        desc.text = "ClosureAI requires UniTask to function properly. This package provides essential async/await functionality for behavior trees.";
                        Style(new()
                        {
                            fontSize = 12,
                            color = ColorPalette.LightGrayText,
                            whiteSpace = WhiteSpace.Normal,
                            marginBottom = 16,
                        });
                    });

                    // Package info section
                    E<VisualElement>(packageSection =>
                    {
                        Style(new()
                        {
                            backgroundColor = ColorPalette.MediumDarkBackground,
                            borderRadius = 4,
                            borderWidth = 1,
                            borderColor = ColorPalette.SubtleBorder,
                            padding = 12,
                            marginBottom = 16,
                        });

                        E<Label>(packageName =>
                        {
                            packageName.text = "UniTask";
                            Style(new()
                            {
                                fontSize = 12,
                                color = ColorPalette.VeryLightGrayText,
                                unityFontStyleAndWeight = FontStyle.Bold,
                                marginBottom = 4,
                            });
                        });

                        E<Label>(version =>
                        {
                            version.text = "Version: 2.5.10";
                            Style(new()
                            {
                                fontSize = 10,
                                color = ColorPalette.DimGrayText,
                                marginBottom = 10,
                            });
                        });

                        E<Button>(linkButton =>
                        {
                            linkButton.text = "Open GitHub Release Page";
                            linkButton.clicked += () => Application.OpenURL(UNITASK_GITHUB_URL);

                            Style(new()
                            {
                                height = 26,
                                backgroundColor = ColorPalette.BlueAccent,
                                borderWidth = 0,
                                borderRadius = 3,
                                color = Color.white,
                            });
                        });
                    });

                    // Instructions
                    E<Label>(instructions =>
                    {
                        instructions.text = "Click 'Done' after installing UniTask.";
                        Style(new()
                        {
                            fontSize = 11,
                            color = ColorPalette.MediumGrayText,
                            whiteSpace = WhiteSpace.Normal,
                            marginBottom = 16,
                        });
                    });
                });

                // Footer with buttons
                E<VisualElement>(footer =>
                {
                    Style(new()
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.FlexEnd,
                        paddingLeft = 16,
                        paddingRight = 16,
                        paddingTop = 12,
                        paddingBottom = 12,
                        borderTopWidth = 1,
                        borderTopColor = ColorPalette.MediumBorder,
                    });

                    E<Button>(dismissButton =>
                    {
                        dismissButton.text = "Dismiss";
                        dismissButton.clicked += OnDismissClicked;

                        Style(new()
                        {
                            minWidth = 80,
                            height = 28,
                            marginRight = 8,
                            borderRadius = 3,
                        });
                    });

                    E<Button>(doneButton =>
                    {
                        doneButton.text = "Done";
                        doneButton.clicked += OnDoneClicked;

                        Style(new()
                        {
                            minWidth = 80,
                            height = 28,
                            borderRadius = 3,
                        });
                    });
                });
            });
        }

        private void OnDismissClicked()
        {
            Close();
        }

        private void OnDoneClicked()
        {
            // Add the UNITASK_INSTALLED define symbol if not already present
            if (!IsUniTaskInstalled())
                AddUniTaskDefineSymbol();

            // Mark as seen
            EditorPrefs.SetBool(Key(INSTALLER_DISMISSED_KEY), true);

            EditorUtility.DisplayDialog(
                "Setup Complete",
                "The UNITASK_INSTALLED define symbol has been added to your project.\n\nUnity will now recompile scripts.",
                "OK");

            Close();
        }

        private static void AddUniTaskDefineSymbol()
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

            // Check if already exists
            if (defines.Contains(UNITASK_INSTALLED_DEFINE))
                return;

            // Add the define
            if (!string.IsNullOrEmpty(defines))
                defines += ";" + UNITASK_INSTALLED_DEFINE;
            else
                defines = UNITASK_INSTALLED_DEFINE;

            PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
        }

        private static string Key(string str)
        {
            // Scope dismiss preference to the current project so other projects still see the installer.
            string projectGuid = PlayerSettings.productGUID.ToString();
            return $"{PREF_KEY}_{projectGuid}_{str}";
        }
    }
}
