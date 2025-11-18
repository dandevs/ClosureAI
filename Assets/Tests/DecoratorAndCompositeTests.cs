using NUnit.Framework;
using static ClosureBT.BT;

namespace ClosureBT.Tests
{
    [TestFixture]
    public class DecoratorAndCompositeTests
    {
        private static Status TickOnce(Node node)
        {
            node.Tick(out var status);
            return status;
        }

        [Test]
        public void ConditionLatch_AllowsChildToFinishAfterTrigger()
        {
            var trigger = true;
            var ticks = 0;

            var sequence = Sequence(() =>
            {
                D.ConditionLatch(() => trigger);
                Leaf("Latched", () =>
                {
                    OnBaseTick(() =>
                    {
                        ticks++;
                        return ticks >= 3 ? Status.Success : Status.Running;
                    });
                });
            });

            Assert.AreEqual(Status.Running, TickOnce(sequence), "Latch should engage when trigger starts true");
            Assert.AreEqual(1, ticks, "Child should tick once while running");

            trigger = false;
            Assert.AreEqual(Status.Running, TickOnce(sequence), "Latched child should keep running even after trigger resets");
            Assert.AreEqual(2, ticks, "Child should continue ticking while latched");

            Assert.AreEqual(Status.Success, TickOnce(sequence), "Latched child should complete after enough ticks");
            Assert.AreEqual(3, ticks, "Child should finish with expected tick count");

            while (!sequence.ResetGracefully()) { }
            trigger = false;
            Assert.AreEqual(Status.Failure, TickOnce(sequence), "Latch should reset once the child finishes");
        }

        [Test]
        public void Parallel_WaitsForAllChildrenToComplete()
        {
            var fastTicks = 0;
            var slowTicks = 0;

            var parallel = Parallel("Parallel Test", () =>
            {
                Leaf("Fast", () =>
                {
                    OnBaseTick(() =>
                    {
                        fastTicks++;
                        return fastTicks >= 2 ? Status.Success : Status.Running;
                    });
                });

                Leaf("Slow", () =>
                {
                    OnBaseTick(() =>
                    {
                        slowTicks++;
                        return slowTicks >= 3 ? Status.Success : Status.Running;
                    });
                });
            });

            Assert.AreEqual(Status.Running, TickOnce(parallel), "Parallel should be running while children are incomplete");
            Assert.AreEqual(1, fastTicks);
            Assert.AreEqual(1, slowTicks);

            Assert.AreEqual(Status.Running, TickOnce(parallel), "Parallel should wait for all children");
            Assert.AreEqual(2, fastTicks);
            Assert.AreEqual(2, slowTicks);

            Assert.AreEqual(Status.Success, TickOnce(parallel), "Parallel should succeed once the slowest child completes");
            Assert.AreEqual(2, fastTicks, "Completed child should not tick again after finishing");
            Assert.AreEqual(3, slowTicks, "Slow child should tick until completion");
        }

        [Test]
        public void Race_CompletesWhenAnyChildFinishes()
        {
            var slowTicks = 0;
            var fastTicks = 0;

            var race = Race("Race Test", () =>
            {
                Leaf("Slow", () =>
                {
                    OnBaseTick(() =>
                    {
                        slowTicks++;
                        return slowTicks >= 5 ? Status.Success : Status.Running;
                    });
                });

                Leaf("Fast", () =>
                {
                    OnBaseTick(() =>
                    {
                        fastTicks++;
                        return fastTicks >= 2 ? Status.Success : Status.Running;
                    });
                });
            });

            Assert.AreEqual(Status.Running, TickOnce(race), "Race should run while no child has finished");
            Assert.AreEqual(1, slowTicks);
            Assert.AreEqual(1, fastTicks);

            Assert.AreEqual(Status.Success, TickOnce(race), "Race should succeed as soon as a child completes");
            Assert.AreEqual(2, slowTicks, "Slow child should have ticked twice during the race");
            Assert.AreEqual(2, fastTicks, "Fast child should finish on the second tick");
        }

        [Test]
        public void WaitUntil_CompletesWhenConditionTrue()
        {
            var ready = false;
            var wait = WaitUntil(() => ready);

            Assert.AreEqual(Status.Running, TickOnce(wait), "WaitUntil should run while condition is false");

            ready = true;
            Assert.AreEqual(Status.Success, TickOnce(wait), "WaitUntil should complete immediately once condition turns true");
        }
    }
}
