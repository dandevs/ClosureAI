#if UNITY_EDITOR
using System.Collections.Generic;
using ClosureAI.Editor;
using SingularityGroup.HotReload;
using UnityEngine;

namespace ClosureAI.HotReload
{
    internal static class UIHotReloadHandler
    {
        private static IEnumerable<TreeEditorWindow> Windows => Resources.FindObjectsOfTypeAll<TreeEditorWindow>();

        [InvokeOnHotReload]
        public static void ReloadAllWindows()
        {
            foreach (var window in Windows)
                window.ReloadGUI();
        }
    }
}
#endif
