#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public partial class BT
    {
        public static YieldNode YieldSimple(string name, Func<Func<Node>> setup)
        {
            return YieldDynamic(name, controller =>
            {
                controller
                    .WithResetYieldedNodeOnNodeChange()
                    .WithResetYieldedNodeOnSelfExit();

                var getNode = setup();
                return _ => getNode();
            });
        }

        public static YieldNode YieldSimple(Func<Func<Node>> setup)
        {
            return YieldSimple("Yield Simple", setup);
        }
    }
}
#endif
