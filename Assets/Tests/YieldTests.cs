using System.Collections.Generic;
using NUnit.Framework;
using static ClosureBT.BT;

namespace ClosureBT.Tests
{
    [TestFixture]
    public class YieldTests
    {
        private static Status TickNode(Node node, int maxIterations = 100)
        {
            Status status = Status.Running;
            for (var i = 0; i < maxIterations; i++)
            {
                if (node.Tick(out status) && status != Status.Running)
                    return status;
            }

            return status;
        }

        [Test]
        public void YieldSimpleCached_CachesChildAcrossTicksAndResets()
        {
            var creationCount = 0;
            var enterCount = 0;

            var yieldNode = YieldSimpleCached("Cache Test", () =>
            {
                creationCount++;
                return Leaf("Yielded Child", () =>
                {
                    OnEnter(() => enterCount++);
                    OnBaseTick(() => Status.Running);
                });
            });

            yieldNode.Tick();
            yieldNode.Tick();

            Assert.AreEqual(1, creationCount, "YieldSimpleCached should create its child once while running");
            Assert.AreEqual(1, enterCount, "Child should have entered exactly once before reset");

            yieldNode.ResetImmediately();

            Assert.IsFalse(yieldNode.Resetting, "Reset should complete synchronously for non-async child");

            yieldNode.Tick();

            Assert.AreEqual(1, creationCount, "Cached child should be reused after reset");
            Assert.AreEqual(2, enterCount, "Child should re-enter after reset without being recreated");
        }

        [Test]
        public void YieldSimpleCached_ResetInvokesChildOnDisabled()
        {
            var disabledCount = 0;

            var yieldNode = YieldSimpleCached("Disable Test", () =>
                Leaf("Yielded Child", () =>
                {
                    OnBaseTick(() => Status.Running);
                    OnDisabled(() => disabledCount++);
                }));

            yieldNode.Tick();
            yieldNode.ResetImmediately();

            Assert.AreEqual(1, disabledCount, "Child should receive OnDisabled when yield node resets");
        }

        [Test]
        public void YieldDynamic_SwitchesResetOldChildBeforeNewEntry()
        {
            var state = 0;
            var log = new List<string>();

            var first = Leaf("First", () =>
            {
                OnEnter(() => log.Add("first-enter"));
                OnDisabled(() => log.Add("first-disabled"));
                OnBaseTick(() => Status.Running);
            });

            var second = Leaf("Second", () =>
            {
                OnEnter(() => log.Add("second-enter"));
                OnBaseTick(() => Status.Success);
            });

            var yieldNode = YieldDynamic("Switcher", controller =>
            {
                controller
                    .WithResetYieldedNodeOnNodeChange()
                    .WithResetYieldedNodeOnSelfExit()
                    .WithConsumeTickOnStateChange(false);

                return _ => state == 0 ? first : second;
            });

            yieldNode.Tick();

            state = 1;
            var status = TickNode(yieldNode);

            Assert.AreEqual(Status.Success, status, "YieldDynamic should return the status of the new child once it completes");
            CollectionAssert.AreEqual(new[] { "first-enter", "first-disabled", "second-enter" }, log,
                "Old child should be disabled before new child enters when switching");
        }
    }
}
