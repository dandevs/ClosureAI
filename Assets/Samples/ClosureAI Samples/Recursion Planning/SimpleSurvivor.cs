#if UNITASK_INSTALLED
using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI.Samples.Shared
{
    /*
     * In this example, we want to create a "sword". The immediate requirements
     * for a sword is that it needs the items iron and stick.
     * sticks can be found on the ground, but iron requires a picaxe to harvest.
     * The picaxe can only be made in a crafting bench.
     *
     * By using Pawn.AcquireItem(() => "sword"), it will attempt to resolve the
     * dependency chain in order to obtain the sword. Please check Pawn.cs for
     * more implementation details.
     */

    public class SimpleSurvivor : MonoBehaviour
    {
        public Pawn Pawn;
        public Node AI;

        private void Awake()
        {
            AI = Pawn.AcquireItem(() => "sword");
        }

        private void Update() => AI.Tick();
        private void OnDestroy() => AI.ResetImmediately();

        private void OnValidate()
        {
            if (!Pawn)
                Pawn = GetComponent<Pawn>();
        }
    }
}
#endif
