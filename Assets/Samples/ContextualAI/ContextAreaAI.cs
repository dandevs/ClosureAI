#if UNITASK_INSTALLED
using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI.Samples.ContextAI
{
    public abstract class ContextAreaAI : MonoBehaviour
    {
        protected Node _cachedAI;
        protected abstract Node CreateAI(ContextSampleNPC npc);
        public Node GetAI(ContextSampleNPC npc) => Node.IsInvalid(_cachedAI) ? _cachedAI = CreateAI(npc) : _cachedAI;
    }
}
#endif
