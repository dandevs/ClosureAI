#if UNITASK_INSTALLED
using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI.Samples.SequenceAndSelector
{
    public class SequenceAndSelectorReactiveSample : MonoBehaviour
    {
        public Node AI;

        public bool A;
        public bool B;

        void Awake() => AI = Reactive * SequenceAlways(() =>
        {
            D.Until(Status.Success);
            Selector(() =>
            {
                Sequence(() =>
                {
                    Condition(() => A);
                    Wait(1);
                });

                Sequence(() =>
                {
                    Condition(() => B);
                    Wait(1);
                });
            });

            JustRunning(() =>
            {
                OnEnter(() => Debug.Log("We're waiting for changes here..."));
            });
        });

        void Update() => AI.Tick();
        void OnDestroy() => AI.ResetImmediately();
    }
}
#endif
