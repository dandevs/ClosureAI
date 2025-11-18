#if UNITASK_INSTALLED
using Unity.AI.Navigation;
using UnityEngine;

namespace ClosureBT.Samples
{
    [DefaultExecutionOrder(-99999)]
    public class AutoBakeNavMesh : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
#endif
