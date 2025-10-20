#if UNITASK_INSTALLED
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using ClosureAI.UI;
using static ClosureAI.UI.VisualElementBuilderHelper;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace ClosureAI.Editor.UI
{
    public class InspectorPropertyField : VisualElement
    {
#if ODIN_INSPECTOR
        private PropertyTree propertyTree;
        private IMGUIContainer odinContainer;
#else
        private PropertyField unityPropertyField;
        private SerializedObject serializedVariable;
#endif
        private VariableUnityObjectWrapper wrapper;

        public InspectorPropertyField(AI.VariableType variable)
        {
            wrapper = VariableUnityObjectWrapper.Get(variable);

#if ODIN_INSPECTOR
            // Odin Inspector approach
            propertyTree = PropertyTree.Create(wrapper);
            var valueProperty = propertyTree?.GetPropertyAtPath("Variable._value");

            if (valueProperty != null)
            {
                var guiContent = new GUIContent(variable.Name);

                odinContainer = new IMGUIContainer(() =>
                {
                    if (propertyTree != null && valueProperty != null)
                    {
                        propertyTree.UpdateTree();
                        propertyTree.BeginDraw(true);

                        valueProperty.Draw(guiContent);

                        propertyTree.EndDraw();

                        if (GUI.changed)
                            propertyTree.ApplyChanges();
                    }
                });

                Add(odinContainer);
            }
            else
            {
                AddUnsupportedDrawerUI(variable);
            }
#else
            // Unity PropertyField approach
            serializedVariable = new SerializedObject(wrapper);
            var valueProperty = serializedVariable.FindProperty("Variable._value");

            if (valueProperty != null)
            {
                unityPropertyField = new PropertyField(valueProperty)
                {
                    label = variable.Name
                };
                unityPropertyField.Bind(serializedVariable);
                Add(unityPropertyField);
            }
            else
            {
                AddUnsupportedDrawerUI(variable);
            }
#endif
            RegisterCallback<DetachFromPanelEvent>(_ => Cleanup());
        }

        private void AddUnsupportedDrawerUI(AI.VariableType variable)
        {
            E(this, _ =>
            {
                E<FlexRow>(() =>
                {
                    Style(new()
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        justifyContent = Justify.SpaceBetween,
                    });

                    E(new Label($"{variable.Name}"));
                    E(new Label("(UNSUPPORTED DRAWER)"), static _ =>
                    {
                        Style(new()
                        {
                            unityFontStyleAndWeight = FontStyle.Italic,
                            fontSize = 10f,
                        });
                    });
                });
            });
        }

        private void Cleanup()
        {
#if ODIN_INSPECTOR
            propertyTree?.Dispose();
#else
            serializedVariable?.Dispose();
#endif
            if (wrapper != null)
                Object.DestroyImmediate(wrapper);
        }
    }
}

#endif