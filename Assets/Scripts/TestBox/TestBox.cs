using UnityEngine;
using static ClosureAI.AI;
using SingularityGroup.HotReload;
using Cysharp.Threading.Tasks;
using System;
public class TestBox : MonoBehaviour
{
    public Node Tree;

    public bool Zero;
    public bool A;
    public bool B;
    public int Counter;

    Node NodeWithReturn(int maxTicks, out Func<int> getValue)
    {
        var _value = 0;
        getValue = () => _value;

        return Leaf("Example", () =>
        {
            var value = Variable(() => 0);

            OnBaseTick(() =>
            {
                value.Value++;
                _value = value.Value;
                return _value < maxTicks ? Status.Running : Status.Success;
            });
        });
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [InvokeOnHotReloadLocal]
    void Start()
    {
        Tree?.ResetImmediately();
        Tree = Reactive * SequenceAlways("Hi", () =>
        {
            D.While(() => Counter < 100);
            D.Reset();
            Sequence(() =>
            {
                Wait(0.01f);
                Do(() => Counter++);
            });

            WaitUntil("Hi", () => Zero, () =>
            {
                OnEnabled(() => Debug.Log("Enabled Hi"));
                OnDisabled(() => Debug.Log("Disabled Hi"));
            });

            _ = Reactive * SequenceAlways("Root", () =>
            {
                Sequence("WHAT", () =>
                {
                    OnEnabled(() => Debug.Log("Enabled WHAT"));
                    OnDisabled(() => Debug.Log("Disabled WHAT"));

                    Condition("A", () => A, () =>
                    {
                        OnExit(async ct =>
                        {
                            Debug.Log("OK");
                            await UniTask.WaitForSeconds(1.5f, cancellationToken: ct);
                            Debug.Log("DOPNE");
                        });
                    });
                    Wait(0.1f);
                });

                Sequence("Hmm", () =>
                {
                    Condition("B", () => B);
                    Wait(1f);
                });

                JustRunning();
            });
        });
    }

    void Update()
    {
        if (!Input.GetKey(KeyCode.Space))
            Tree.Tick();
        else
            Tree.ResetImmediately();
    }

    void OnDestroy()
    {
        Tree.ResetImmediately();
    }
}
