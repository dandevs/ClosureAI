#if UNITASK_INSTALLED
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ClosureBT.Editor;
using UnityEngine;

namespace ClosureBT
{
    public partial class BT
    {
        public partial class VariableType
        {
            /// <summary>
            /// Editor only property - Stack trace lines from where this variable was created
            /// </summary>
            public string[] Lines { get; protected set; }
            public bool InsideUsePipe { get; protected set; }

            /// <summary>
            /// Editor only property - Source file path where this variable was created
            /// </summary>
            // public string FilePath { get; protected set; }

            private bool _attemptedToFindName = false;
            private string _name = "Variable";

            public string Name
            {
                get
                {
                    if (_attemptedToFindName)
                        return _name;

                    _attemptedToFindName = true;

                    var entries = new List<(string filePath, int lineNumber)>();

                    // Find the line number from the stack trace lines
                    if (Lines != null)
                    {
                        for (var i = 0; i < Lines.Length; i++)
                        {
                            if (Lines[i].StartsWith("ClosureBT.BT:Variable<") || Lines[i].Contains(":Use"))
                            {
                                if (i + 1 < Lines.Length)
                                    entries.Add(NodeEditorUtility.ExtractFileInfo(Lines[i + 1]));
                            }

                            if (Lines[i].Contains("ClosureBT.BT.UsePipe"))
                                InsideUsePipe = true;
                        }
                    }

                    // Default to private if we can't determine the name
                    var nameFound = false;

                    if (entries.Count > 0)
                    {
                        try
                        {
                            foreach (var (filePath, lineNumber) in entries)
                            {
                                var script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(filePath);

                                if (!script)
                                    continue;

                                // Get the text of the script
                                var text = script.text;
                                var lines = text.Split('\n');

                                if (lineNumber - 1 >= lines.Length)
                                    continue;

                                var line = lines[lineNumber - 1];

                                // Extract variable name using regex - look for pattern: [Name] = Variable or [Name] = Use...
                                // This matches both direct Variable calls and Use* method calls
                                var varAssignmentPattern = @"(\w+)\s*=\s*(?:Variable(?:<|\()|Use)";
                                var match = System.Text.RegularExpressions.Regex.Match(line, varAssignmentPattern);

                                if (match.Success && match.Groups.Count > 1)
                                {
                                    _name = match.Groups[1].Value;
                                    nameFound = true;

                                    // Variables with underscore are always internal/private
                                    IsPublicVariable = !_name.StartsWith("_");

                                    if (char.IsLower(_name[0]))
                                        _name = char.ToUpper(_name[0]) + _name[1..];
                                }

                                if (!IsPublicVariable)
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to extract variable name: {ex.Message}");
                        }
                    }

                    // If we couldn't find the name, assume it's private to be safe
                    if (!nameFound)
                        IsPublicVariable = false;

                    return _name;
                }
            }

            /// <summary>
            /// Editor-only method to mark this variable as non-public (hidden from inspector)
            /// </summary>
            public void MarkAsNonPublic()
            {
                IsPublicVariable = false;
            }
        }
    }
}
#endif

#endif
