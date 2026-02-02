using UnityEngine;
using static ClosureBT.BT;

public class AnotherSampleTest : MonoBehaviour
{
    public Node AI; // This will expose a "Open Node Graph" button in the inspector
    public bool A;
    public bool B;
    public bool C;

    private void Awake() => AI = Reactive * Sequence("NPC AI", () =>
    {
        WaitUntil("A Is True", () => A);
        WaitUntil("B Is True", () => B);
        WaitUntil("C Is True", () => C);

        Wait(1f, () =>
        {
            OnEnter(() => Debug.Log("Entering Wait"));
            OnExit(() => Debug.Log("Exiting Wait"));
        });

        Do(() => Debug.Log("Completed!"));
    });

    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();
}