#if UNITASK_INSTALLED
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        public static partial class D
        {
            public static DecoratorNode MustCompleteFirst() => Decorator("Must Complete First", () =>
            {
                var node = (DecoratorNode)CurrentNode;

                OnExit(async (ct, tick) =>
                {
                    Status status = Status.None;

                    if (node.Child.Done)
                    {
                        await ExitNode(node.Child);
                        return;
                    }

                    while (!node.Child.Tick(out status))
                    {
                        // if (node.Child.Done)
                        //     break;

                        await tick();
                    }
                });

                OnInvalidCheck(() => node.Child.IsInvalid());

                OnBaseTick(() =>
                {
                    return node.Child.Tick(out var status, true) ? status : Status.Running;
                });
            });
        }
    }
}
#endif
