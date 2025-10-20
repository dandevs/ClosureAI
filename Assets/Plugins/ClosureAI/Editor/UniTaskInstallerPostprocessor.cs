#if !UNITASK_INSTALLED
using UnityEditor;
using UnityEditor.Build;

namespace ClosureAI.Editor
{
    /// <summary>
    /// Detects when the ClosureAI package is first imported and shows the UniTask installer window.
    /// This ensures the installer appears even when the package is imported as a .unitypackage.
    /// </summary>
    public class UniTaskInstallerPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check if UniTaskInstallerWindow.cs was just imported
            // This indicates the ClosureAI package is being imported
            foreach (string asset in importedAssets)
            {
                if (asset.EndsWith("UniTaskInstallerWindow.cs") && asset.Contains("ClosureAI"))
                {
                    // Package is being imported - schedule installer check
                    // Use delayCall to ensure Unity is fully ready after import
                    EditorApplication.delayCall += ShowInstallerOnImport;
                    break;
                }
            }
        }

        private static void ShowInstallerOnImport()
        {
            // Remove this callback to prevent multiple calls
            EditorApplication.delayCall -= ShowInstallerOnImport;

            // Check if UniTask is already installed
            if (IsUniTaskInstalled())
                return;

            // Check if user has previously dismissed the installer
            // This handles re-imports of the package in projects that already dismissed it
            string projectGuid = PlayerSettings.productGUID.ToString();
            string key = $"ClosureAI_{projectGuid}_HasSeenUniTaskInstaller";
            bool hasSeenInstaller = EditorPrefs.GetBool(key, false);

            if (!hasSeenInstaller)
            {
                UniTaskInstallerWindow.ShowWindow();
            }
        }

        private static bool IsUniTaskInstalled()
        {
            // Check if UNITASK_INSTALLED define exists
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            return defines.Contains(UniTaskInstallerWindow.UNITASK_INSTALLED_DEFINE);
        }
    }
}
#endif
