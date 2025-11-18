#if UNITASK_INSTALLED
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT.Samples.ContextAI
{
    public class HappyJumpyContextAreaAI : ContextAreaAI
    {
        public Transform JumpOffPoint;
        public Transform TrampolinePoint;

        protected override Node CreateAI(ContextSampleNPC npc) => D.Repeat() + Sequence("Happy Jumpy", () =>
        {
            npc.Pawn.MoveTo(() => JumpOffPoint.position);

            WaitUntil(() => npc.Pawn.LookAtXZ(npc.transform.position + JumpOffPoint.forward));
            Wait(0.5f);
            npc.JumpTo(() => TrampolinePoint.position);
            npc.Jump(() => 6f);
            Wait(0.5f);
            npc.Spin(() => 1f);

            D.RepeatCount(3);
            npc.Jump();

            npc.Spin(() => 1f);

            D.RepeatCount(2);
            npc.Jump();
        });
    }
}
#endif
