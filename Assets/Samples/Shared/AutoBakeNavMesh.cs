using Unity.AI.Navigation;
using UnityEngine;

namespace ClosureAI.Samples
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
