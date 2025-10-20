using System.Collections.Generic;
using NUnit.Framework;
using static ClosureAI.AI;

namespace ClosureAI.Tests
{
    [TestFixture]
    public class MixedNodeIntegrationTests
    {
        private Status TickNode(Node node, int maxIterations = 100)
        {
            Status status = Status.Running;
            for (var i = 0; i < maxIterations; i++)
            {
                if (node.Tick(out status) && status != Status.Running)
                {
                    return status;
                }
            }

            return status;
        }

        [Test]
        public void Selector_WithConditionDecorator_UsesFallbackWhenGateFalse()
        {
            var events = new List<string>();
            var gate = false;

            var selector = Selector(() =>
            {
                D.Condition(() => gate);
                Do(() => events.Add("gated"));

                Do(() => events.Add("fallback"));
            });

            var status = TickNode(selector);

            Assert.AreEqual(Status.Success, status, "Selector should succeed via fallback child");
            Assert.AreEqual(new[] { "fallback" }, events.ToArray(), "Fallback child should execute when gate is false");
        }

        [Test]
        public void Selector_WithConditionDecorator_StopsAfterGatedBranchWhenGateTrue()
        {
            var events = new List<string>();
            var gate = true;

            var selector = Selector(() =>
            {
                D.Condition(() => gate);
                Do(() => events.Add("gated"));

                Do(() => events.Add("fallback"));
            });

            var status = TickNode(selector);

            Assert.AreEqual(Status.Success, status, "Selector should succeed using gated branch");
            Assert.AreEqual(new[] { "gated" }, events.ToArray(), "Fallback child should not execute when gate is true");
        }

        [Test]
        public void Sequence_WithRepeatWhileDecorator_RepeatsChildUntilConditionFails()
        {
            var events = new List<string>();
            var counter = 0;

            var sequence = Sequence(() =>
            {
                D.While(() => counter < 2);
                D.Reset();
                Sequence(() =>
                {
                    Do(() =>
                    {
                        events.Add($"loop-{counter}");
                        counter++;
                    });
                });

                Do(() => events.Add("after"));
            });

            var status = TickNode(sequence);

            Assert.AreEqual(Status.Success, status, "Sequence should succeed once repeat condition fails");
            Assert.AreEqual(new[] { "loop-0", "loop-1", "after" }, events.ToArray(), "RepeatWhile should execute child until condition turns false");
        }

        [Test]
        public void SequenceAlways_WithRepeatUntilAndJustRunning_AdvancesToTailNode()
        {
            var events = new List<string>();
            var succeedAfter = 0;

            var sequenceAlways = SequenceAlways("Mixed Sequence", () =>
            {
                D.Until(Status.Success);
                Leaf("Delayed Success", () =>
                {
                    OnBaseTick(() =>
                    {
                        events.Add($"leaf-{succeedAfter}");
                        succeedAfter++;
                        return succeedAfter >= 3 ? Status.Success : Status.Running;
                    });
                });

                JustRunning("Tail", () =>
                {
                    OnTick(() => events.Add("tail"));
                });
            });

            sequenceAlways.Tick(out var status);
            Assert.AreEqual(Status.Running, status, "SequenceAlways should remain running while decorator child runs");
            CollectionAssert.AreEqual(new[] { "leaf-0" }, events, "First tick should only process the decorator child");

            sequenceAlways.Tick(out status);
            Assert.AreEqual(Status.Running, status, "SequenceAlways should still be running while decorator repeats");
            CollectionAssert.AreEqual(new[] { "leaf-0", "leaf-1" }, events, "Second tick should continue decorator child");

            sequenceAlways.Tick(out status);
            Assert.AreEqual(Status.Running, status, "SequenceAlways should stay running after advancing to tail child");
            CollectionAssert.AreEqual(new[] { "leaf-0", "leaf-1", "leaf-2", "tail" }, events, "Tail node should execute once decorator completes");

            sequenceAlways.Tick(out status);
            Assert.AreEqual(Status.Running, status, "SequenceAlways should keep running because of tail node");
            CollectionAssert.AreEqual(new[] { "leaf-0", "leaf-1", "leaf-2", "tail", "tail" }, events, "Tail node should tick every frame after activation");
        }
    }
}
