#if UNITASK_INSTALLED
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT.Samples.SequenceAndSelector
{
    public class SequenceAndSelectorSample : MonoBehaviour
    {
        public Node AI;

        public bool A;
        public bool B;

        void Awake() => AI = Sequence(() =>
        {
            D.Until(Status.Success);
            Selector(() =>
            {
                Sequence(() =>
                {
                    Condition("A True", () => A);
                    Wait(1);
                });

                Sequence(() =>
                {
                    Condition("B True", () => B);
                    Wait(1);
                });
            });

            WaitUntil("!A && !B", () => !A && !B);
            Do("Log \"Done\"", () => Debug.Log("Done"));
        });

        void Update() => AI.Tick();
        void OnDestroy() => AI.ResetImmediately();
    }
}
#endif
