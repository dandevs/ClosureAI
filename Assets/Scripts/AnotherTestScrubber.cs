using System;
using UnityEngine;
using static ClosureAI.AI;

public class AnotherTestScrubber : MonoBehaviour
{
    public Node AI;
    public bool SwitchToA;

    private void Awake() => AI = YieldDynamic(controller =>
    {
        controller
            .WithResetYieldedNodeOnNodeChange()
            .WithResetYieldedNodeOnSelfExit();

        var ticksElapsed = UseTicksElapsed();
        var timeElapsed = UseTimeElapsed();

        Node a = null;
        Node b = null;

        return _ =>
        {
            if (SwitchToA)
                return a ??= A();

            return timeElapsed.Value < 1f
                ? a ??= A()
                : b ??= B();
        };
    });

    private Node A() => Sequence("A", () =>
    {
        Wait(0.25f);
        Wait(0.25f);
        Wait(0.25f);
        Wait(0.25f);
        JustRunning();
    });

    private Node B() => Sequence("B", () =>
    {
        Wait(0);
        Leaf("Hello", () => {});
        JustRunning();
    });

    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();
}
