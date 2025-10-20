using SingularityGroup.HotReload;
using UnityEngine;
using static ClosureAI.AI;

public class QuickoTest : MonoBehaviour
{
    public Node AI;
    private int i = 0;

    [InvokeOnHotReloadLocal]
    private void Awake()
    {
        AI?.ResetImmediately();
        AI = WaitNodeForever();
    }

    private Node WaitNodeForever() => Sequence("HEHEHEHE", () =>
    {
        Wait(0.25f, () =>
        {
        });

        if (i++ < 5)
            YieldSimpleCached(WaitNodeForever);
    });

    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetGracefully();
}
