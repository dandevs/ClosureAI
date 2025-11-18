#if UNITASK_INSTALLED
using UnityEngine;

namespace ClosureBT
{
    public class VariableUnityObjectWrapper : ScriptableObject
    {
        [SerializeReference]
        public BT.VariableType Variable;

        public static VariableUnityObjectWrapper Get(BT.VariableType variable)
        {
            var wrapper = CreateInstance<VariableUnityObjectWrapper>();
            wrapper.Variable = variable;
            return wrapper;
        }
    }
}

#endif
