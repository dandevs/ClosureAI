using UnityEngine;
using static ClosureBT.BT;

namespace GraphDemoHero
{
    public class GraphDemoHero : MonoBehaviour
    {
        public Node AI;
        public GameObject Target;

        private void Awake() => AI = Reactive * SequenceAlways(() =>
        {
            // D.ConditionLatch(() => Input.GetKeyDown(KeyCode.Space));
            // D.RepeatCount(3);
            Sequence(() =>
            {
                Condition(() => Input.GetKey(KeyCode.Space));
                D.RepeatCount(5);
                Sequence(() =>
                {
                    Wait(0.25f);
                    Wait(0.25f);
                });
            });

            D.Condition(() => Target);
            Sequence(() =>
            {
                var timeAttackElapsed = UseTimeElapsed();
                JustRunning();
            });

            JustRunning();
        });

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                Target = Target ? null : gameObject;
            }

            AI.Tick();
        }

        void OnDestroy() => AI.ResetImmediately();
    }
}
