#if UNITASK_INSTALLED
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT.Samples.RaceExample
{
    public class RaceExample : MonoBehaviour
    {
        public Node AI;
        public bool A;
        public bool B;
        public bool C;

        void Awake() => AI = Reactive * Sequence(() =>
        {
            WaitUntil("A", () => A);

            D.Until(Status.Success);
            Race(() =>
            {
                Sequence(() =>
                {
                    Condition("B", () => B);
                    Wait(1f);
                });

                Sequence(() =>
                {
                    Condition("C", () => C);
                    Wait(1f);
                });
            });

            JustRunning();
        });

        void Update() => AI.Tick();
        void OnDestroy() => AI.ResetImmediately();
    }
}
#endif
