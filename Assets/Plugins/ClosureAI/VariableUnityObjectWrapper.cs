#if UNITASK_INSTALLED
using UnityEngine;

namespace ClosureAI
{
    public class VariableUnityObjectWrapper : ScriptableObject
    {
        [SerializeReference]
        public AI.VariableType Variable;

        public static VariableUnityObjectWrapper Get(AI.VariableType variable)
        {
            var wrapper = CreateInstance<VariableUnityObjectWrapper>();
            wrapper.Variable = variable;
            return wrapper;
        }
    }
}

#endif