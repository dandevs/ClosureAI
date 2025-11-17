using System.Collections.Generic;
using ClosureAI.Samples;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static ClosureAI.AI;

public class HeroAI_Dummy : MonoBehaviour
{
    public Node AI;

    public List<GameObject> MockTargets;

    protected virtual void Awake() => AI = Reactive * SequenceAlways("Hero AI", () =>
    {
        var targetInSight = Variable<GameObject>(null);
        var health = Variable(100f);
        var weapon = Variable("sword");
        var coins = Variable(0);
        var items = Variable(new List<string>()
        {
            "sword",
            "pebble",
            "golden-key"
        });

        Sequence("Fight Target", () =>
        {
            Condition("Has Target", () => targetInSight.Value != null);
            Wait("Chase Target", 1f);
            Wait("Fight", 1.5f, () =>
            {
                OnSuccess(() =>
                {
                    health.Value -= 20f;
                    coins.Value += 10;

                    switch (targetInSight.Value.name)
                    {
                        case "Wizard":
                            items.Value.Add("wizard-staff");
                            break;
                        case "Robber":
                            items.Value.Add("diamond-ring");
                            break;
                        case "Washing Machine":
                            items.Value.Add("wet-clothes");
                            break;
                        case "Evil Dog":
                            items.Value.Add("dog-bone");
                            break;
                    }

                    targetInSight.Value = null;
                });
            });
        });

        Sequence("Get Healed", () =>
        {
            Condition("Health Not Full", () => health.Value < 100f);
            Wait("Find Healer", 1f);
            Wait("Get Heals", 1f, () =>
            {
                OnSuccess(() => health.Value += 15f);
            });
        });

        JustRunning("Idle", () =>
        {
            OnEnter(async ct =>
            {
                var randBetween1_2 = Random.Range(2f, 3.5f);
                await UniTask.WaitForSeconds(randBetween1_2, cancellationToken: ct);
                targetInSight.Value = MockTargets.Count > 0 ? MockTargets[Random.Range(0, MockTargets.Count)] : null;
            });
        });
    });

    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();
}
