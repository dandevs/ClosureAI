#if UNITASK_INSTALLED
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI.Samples.ContextAI
{
    public class MoveToRandomPointsContextAreaAI : ContextAreaAI
    {
        public List<Transform> Points;

        protected override Node CreateAI(ContextSampleNPC npc) => Leaf("Move Around Points", () =>
        {
            OnTick(async (ct, tick) =>
            {
                while (true)
                {
                    await UniTask.Yield(ct);
                    foreach (var point in Points)
                    {
                        try
                        {
                            while (!npc.Pawn.MoveTo(point.position))
                                await tick();
                        }
                        finally
                        {
                            npc.Pawn.Agent.ResetPath();
                        }

                        await UniTask.WaitForSeconds(0.3f, cancellationToken: ct);
                    }
                }
            });

            OnBaseTick(() => Status.Running);
        });
    }
}
#endif
