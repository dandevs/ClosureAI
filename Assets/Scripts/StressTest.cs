using System.Collections.Generic;
using UnityEngine;
using static ClosureAI.AI;

public class StressTest : MonoBehaviour
{
    public List<Node> Nodes;

    private void Awake()
    {
        for (var i = 0; i < 100; i++)
        {
            Nodes.Add(F());
        }
    }

    private Node F(int i = 0)
    {
        return Sequence(() =>
        {
            Wait(1f);
            Wait(1f);
            Wait(1f);

            if (i < 10)
                F(i + 1);
        });
    }

    private void Update()
    {
        foreach (var node in Nodes)
        {
            node.Tick();
        }
    }

    private void Onestroy()
    {
        foreach (var node in Nodes)
        {
            node.ResetImmediately();
        }
    }
}
