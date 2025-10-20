#if UNITASK_INSTALLED
using UnityEditor;
using UnityEngine;

namespace ClosureAI.Editor
{
    [CustomPropertyDrawer(typeof(AI.Node))]
    public class NodePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Begin property drawing
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects - label takes remaining space, button is fixed width
            var labelRect = new Rect(position.x, position.y, position.width - 125, position.height);
            var buttonRect = new Rect(position.x + position.width - 120, position.y, 120, position.height);

            // Draw just the label (no dropdown/foldout)
            EditorGUI.LabelField(labelRect, label);

            if (GUI.Button(buttonRect, "Open Node Graph"))
            {
                TreeEditorWindow.Open(property.serializedObject.targetObject, property.name);
            }

            // End property drawing
            EditorGUI.EndProperty();
        }
    }
}

#endif