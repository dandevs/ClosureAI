#if UNITASK_INSTALLED
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace ClosureAI.Editor
{
    public static class NodeEditorUtility
    {
        /// <summary>
        /// Extracts file path and line number from a stack trace line
        /// </summary>
        /// <param name="stackTraceLine">A line from the stack trace</param>
        /// <returns>Tuple containing file path and line number, or (null, -1) if not found</returns>
        public static (string, int) ExtractFileInfo(string stackTraceLine)
        {
            var atIndex = stackTraceLine.LastIndexOf(" (at ", StringComparison.Ordinal);

            if (atIndex >= 0)
            {
                var fileInfo = stackTraceLine.Substring(atIndex + 5); // +5 to skip " (at "

                // Remove closing parenthesis if it exists
                if (fileInfo.EndsWith(")"))
                {
                    fileInfo = fileInfo.Substring(0, fileInfo.Length - 1);
                }

                // Split into filepath and line number
                var colonIndex = fileInfo.LastIndexOf(':');

                if (colonIndex >= 0)
                {
                    var filePath = fileInfo.Substring(0, colonIndex);
                    var lineNumberStr = fileInfo.Substring(colonIndex + 1);

                    if (int.TryParse(lineNumberStr, out var lineNumber))
                        return (filePath, lineNumber);
                }
            }

            return (string.Empty, -1);
        }

        public static T GetInterface<T>(this VisualElement visualElement)
        {
            while (visualElement != null)
            {
                if (visualElement is T t)
                    return t;

                visualElement = visualElement.parent;
            }

            return default;
        }

        public static void OpenInTextEditor(string sourceFilePath, int sourceLineNumber)
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(sourceFilePath), sourceLineNumber);
        }
    }
}
#endif

#endif