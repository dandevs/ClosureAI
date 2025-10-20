#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public partial class AI
    {
        [Serializable]
        public abstract partial class VariableType
        {
            public Type ValueType { get; protected set; }
            public abstract object GetValueAsObject();
            public abstract void SetValueFromObject(object value);
            public Node Node { get; private set; }
            private Func<bool> _use;

            /// <summary>
            /// Indicates whether this variable should be visible in the inspector.
            /// Set to true for variables returned from Use* methods (without underscore prefix).
            /// Set to false for internal helper variables (with underscore prefix).
            /// </summary>
            public bool IsPublicVariable { get; internal set; } = true;

            public bool Use() => _use?.Invoke() ?? false;
            public void SetUse(Func<bool> use) => _use = use;
            public Func<bool> GetUse() => _use;
            internal Action Initialize;

            public void OnInitialize(Action onInitialize)
            {
                if (Initialize != null)
                {
                    var originalInitialize = Initialize;

                    Initialize = () =>
                    {
                        originalInitialize();
                        onInitialize();
                    };
                }
                else
                    Initialize = onInitialize;
            }

            protected VariableType(Node node)
            {
                Node = node;
            }
        }

        [Serializable]
        public class VariableType<T> : VariableType
        {
            [SerializeField]
            private T _value;
            public T Value
            {
                get => _value;
                set
                {
                    _value = value;

                    if (Node.Status != Status.None)
                        OnSignal?.Invoke(_value);
                }
            }

            public Func<T> Fn => _implicitFunc ??= () => Value;
            private Func<T> _implicitFunc;

            public event Action<T> OnSignal;

            public VariableType(T value, Node node, string[] lines = null) : base(node)
            {
                Value = value;
                ValueType = typeof(T);

#if UNITY_EDITOR
                Lines = lines;
                // FilePath = filePath;
#endif
            }

            // public void Signal() => OnSignal?.Invoke(_value);
            public void SetValueSilently(T value) => _value = value;

            public override object GetValueAsObject() => Value;

            public override void SetValueFromObject(object value)
            {
                try
                {
                    _value = (T)value;
                }
                catch (InvalidCastException ex)
                {
                    var valueType = value?.GetType().Name ?? "null";
                    var expectedType = typeof(T).Name;
                    var nodeInfo = Node != null ? $" in Node: {Node.Name}" : "";

                    var name = "";

#if UNITY_EDITOR
                    name = Name;
#endif

                    Debug.LogError($"Variable Cast Error: '{name}' cannot cast '{valueType}' to '{expectedType}'{nodeInfo}. " +
                                  $"Value: '{value}'{Environment.NewLine}Original Exception: {ex.Message}");

                    throw new InvalidCastException($"Cannot cast '{valueType}' to '{expectedType}'{nodeInfo}. Value: '{value}'", ex);
                }
            }

            // public static implicit operator T(VariableType<T> variable) => variable.Value;
            public static implicit operator Func<T>(VariableType<T> variable) => variable.Fn;
            // public static implicit operator VariableType<T>(Func<T> variable) => Variable(variable);
        }

        //********************************************************************************************

        public static VariableType<T> Variable<T>(T value = default, Func<T> enterValue = null, Func<T> exitValue = null)
        {
            var node = CurrentNode;

            if (node != null)
            {
                string[] lines = null;

#if UNITY_EDITOR
                // Capture the stack trace lines for later processing
                lines = StackTraceUtility.ExtractStackTrace().Split("\n", StringSplitOptions.RemoveEmptyEntries);

                // for (var i = 0; i < lines.Length; i++)
                // {
                //     if (lines[i].StartsWith("ClosureAI.AI:Variable<") || lines[i].StartsWith("ClosureAI.AI:Use"))
                //     {
                //         (filePath, _) = NodeEditorUtility.ExtractFileInfo(lines[i + 1]);
                //         break;
                //     }
                // }
#endif
                var variable = new VariableType<T>(value, node, lines);
                node.Variables ??= new();
                node.Variables.Add(variable);

                if (enterValue != null)
                    variable.OnInitialize(() => variable.Value = enterValue());

                if (exitValue != null)
                    OnExit(() => variable.Value = exitValue());

                return variable;
            }
            else
                throw new Exception("Variable must be inside a node");
        }

        public static VariableType<T> Variable<T>(Func<T> enterValue, Func<T> exitValue) => Variable(default, enterValue, exitValue);
        public static VariableType<T> Variable<T>(Func<T> getValue)
        {
            var variable = Variable<T>();
            variable.OnInitialize(() => variable.Value = getValue());
            return variable;
        }
    }
}

#endif