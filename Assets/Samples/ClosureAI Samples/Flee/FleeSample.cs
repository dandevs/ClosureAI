#if UNITASK_INSTALLED
using static ClosureAI.AI;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ClosureAI.Samples.Flee
{
    public class FleeSample : MonoBehaviour
    {
        public GameObject Buddy;
        public Node BuddyAI;

        private void Awake() => BuddyAI = CreateBuddyAI();
        private void Update() => BuddyAI.Tick();
        private void OnDestroy() => BuddyAI.ResetImmediately();

        private Node CreateBuddyAI() => Reactive * SequenceAlways("Buddy", () =>
        {
            // VARIABLE: Stores data that persists across tree ticks
            // This variable will hold the calculated distance to the player's mouse
            var distanceToPlayer = Variable(() => 0f);

            OnTick(() =>
            {
                distanceToPlayer.Value = Vector3.Distance(Buddy.transform.position, PlaneCastMousePosition());
            });

            // Why a ConditionLatch + Until?
            // hysteresis - we want to avoid rapid toggling between states
            // We do not want to constantly switch between "Flee" and "Not Flee" if the player is hovering around the threshold distance
            // The ConditionLatch ensures that once we start fleeing, we won't stop until the player is sufficiently far away
            // This creates a more stable and natural behavior
            D.ConditionLatch("Too Close!", () => distanceToPlayer.Value < 2f);
            D.Until(() => distanceToPlayer.Value > 5f);
            JustRunning("Flee!", () =>
            {
                OnEnter(() => Debug.Log("We're running away!"));
                OnExit(() => Debug.Log("Safe now."));

                OnTick(() =>
                {
                    if (distanceToPlayer.Value < 3.5f)
                    {
                        var directionAway = (Buddy.transform.position - PlaneCastMousePosition()).normalized;
                        MoveBuddy(Buddy.transform.position + directionAway * 3f);
                    }
                });
            });

            // Once the "Flee" behavior completes (player is far enough away),
            // the Sequence moves to the next child: return to center
            Sequence(() =>
            {
                WaitUntil(() => MoveBuddy(Vector3.zero));
                Wait(0.25f);

                JustRunning("Move in a circle", () =>
                {
                    OnEnter(() => Debug.Log("Moving to center."));

                    OnTick(() =>
                    {
                        var circlePosition = new Vector3
                        {
                            x = Mathf.Cos(Time.time) * 1f,
                            z = Mathf.Sin(Time.time) * 1f,
                        };

                        MoveBuddy(circlePosition);
                    });
                });
            });
        });

        private bool MoveBuddy(Vector3 position)
        {
            Buddy.transform.position = Vector3.Lerp(Buddy.transform.position, position, Time.deltaTime * 2f);
            return Vector3.Distance(Buddy.transform.position, position) < 0.1f;
        }

        private Vector3 PlaneCastMousePosition()
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : Vector3.zero;
        }
    }
}
#endif
