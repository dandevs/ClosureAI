using System.Collections.Generic;
using NUnit.Framework;
using static ClosureAI.AI;

namespace ClosureAI.Tests
{
    [TestFixture]
    public class LifecycleOrderTests
    {
        private static Status TickNode(Node node, int maxIterations = 100)
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
        public void LeafLifecycle_SuccessInvokesCallbacksInOrder()
        {
            var events = new List<string>();

            var leaf = Leaf("Lifecycle Leaf", () =>
            {
                OnEnter(() => events.Add("enter"));
                OnBaseTick(() =>
                {
                    events.Add("base");
                    return Status.Success;
                });
                OnSuccess(() => events.Add("success"));
                OnExit(() => events.Add("exit"));
            });

            var status = TickNode(leaf);

            Assert.AreEqual(Status.Success, status, "Leaf should report success");
            CollectionAssert.AreEqual(new[] { "enter", "base", "success", "exit" }, events,
                "Lifecycle calls should occur in order: enter → base → success → exit");
        }

        [Test]
        public void LeafLifecycle_FailureInvokesFailureBeforeExit()
        {
            var events = new List<string>();

            var leaf = Leaf("Failure Leaf", () =>
            {
                OnEnter(() => events.Add("enter"));
                OnBaseTick(() =>
                {
                    events.Add("base");
                    return Status.Failure;
                });
                OnFailure(() => events.Add("failure"));
                OnExit(() => events.Add("exit"));
            });

            var status = TickNode(leaf);

            Assert.AreEqual(Status.Failure, status, "Leaf should report failure");
            CollectionAssert.AreEqual(new[] { "enter", "base", "failure", "exit" }, events,
                "Failure lifecycle should run before exit");
        }

        [Test]
        public void CompositeLifecycle_ChildExitsBeforeParent()
        {
            var events = new List<string>();

            var sequence = Sequence("Parent", () =>
            {
                OnEnter(() => events.Add("parent-enter"));
                OnSuccess(() => events.Add("parent-success"));
                OnExit(() => events.Add("parent-exit"));

                Leaf("Child", () =>
                {
                    OnEnter(() => events.Add("child-enter"));
                    OnBaseTick(() =>
                    {
                        events.Add("child-base");
                        return Status.Success;
                    });
                    OnSuccess(() => events.Add("child-success"));
                    OnExit(() => events.Add("child-exit"));
                });
            });

            var status = TickNode(sequence);

            Assert.AreEqual(Status.Success, status, "Sequence should succeed when its child succeeds");
            CollectionAssert.AreEqual(
                new[] { "parent-enter", "child-enter", "child-base", "child-success", "child-exit", "parent-success", "parent-exit" },
                events,
                "Child lifecycle should complete before parent success/exit");
            Assert.Less(events.IndexOf("child-exit"), events.IndexOf("parent-exit"),
                "Parent exit must happen after child exit");
        }

        [Test]
        public void CompositeLifecycle_FailurePropagatesAfterChildExit()
        {
            var events = new List<string>();

            var sequence = Sequence("Failure Parent", () =>
            {
                OnEnter(() => events.Add("parent-enter"));
                OnFailure(() => events.Add("parent-failure"));
                OnExit(() => events.Add("parent-exit"));

                Leaf("Failing Child", () =>
                {
                    OnEnter(() => events.Add("child-enter"));
                    OnBaseTick(() =>
                    {
                        events.Add("child-base");
                        return Status.Failure;
                    });
                    OnFailure(() => events.Add("child-failure"));
                    OnExit(() => events.Add("child-exit"));
                });
            });

            var status = TickNode(sequence);

            Assert.AreEqual(Status.Failure, status, "Sequence should fail when child fails");
            CollectionAssert.AreEqual(
                new[] { "parent-enter", "child-enter", "child-base", "child-failure", "child-exit", "parent-failure", "parent-exit" },
                events,
                "Parent failure and exit should run after child completes its cleanup");
            Assert.Less(events.IndexOf("child-exit"), events.IndexOf("parent-exit"),
                "Parent exit must occur after child exit on failure");
        }

        [Test]
        public void DeepSequenceLifecycle_AllLevelsOrdered()
        {
            var events = new List<string>();

            var root = Sequence("Root", () =>
            {
                OnEnter(() => events.Add("root-enter"));
                OnSuccess(() => events.Add("root-success"));
                OnExit(() => events.Add("root-exit"));

                Leaf("LeafA", () =>
                {
                    OnEnter(() => events.Add("leafA-enter"));
                    OnBaseTick(() =>
                    {
                        events.Add("leafA-base");
                        return Status.Success;
                    });
                    OnSuccess(() => events.Add("leafA-success"));
                    OnExit(() => events.Add("leafA-exit"));
                });

                Sequence("Inner", () =>
                {
                    OnEnter(() => events.Add("inner-enter"));
                    OnSuccess(() => events.Add("inner-success"));
                    OnExit(() => events.Add("inner-exit"));

                    Leaf("InnerLeaf1", () =>
                    {
                        OnEnter(() => events.Add("innerLeaf1-enter"));
                        OnBaseTick(() =>
                        {
                            events.Add("innerLeaf1-base");
                            return Status.Success;
                        });
                        OnSuccess(() => events.Add("innerLeaf1-success"));
                        OnExit(() => events.Add("innerLeaf1-exit"));
                    });

                    Leaf("InnerLeaf2", () =>
                    {
                        OnEnter(() => events.Add("innerLeaf2-enter"));
                        OnBaseTick(() =>
                        {
                            events.Add("innerLeaf2-base");
                            return Status.Success;
                        });
                        OnSuccess(() => events.Add("innerLeaf2-success"));
                        OnExit(() => events.Add("innerLeaf2-exit"));
                    });
                });
            });

            var status = TickNode(root);

            Assert.AreEqual(Status.Success, status, "Entire tree should succeed");
            CollectionAssert.AreEqual(
                new[]
                {
                    "root-enter",
                    "leafA-enter",
                    "leafA-base",
                    "leafA-success",
                    "leafA-exit",
                    "inner-enter",
                    "innerLeaf1-enter",
                    "innerLeaf1-base",
                    "innerLeaf1-success",
                    "innerLeaf1-exit",
                    "innerLeaf2-enter",
                    "innerLeaf2-base",
                    "innerLeaf2-success",
                    "innerLeaf2-exit",
                    "inner-success",
                    "inner-exit",
                    "root-success",
                    "root-exit"
                },
                events,
                "Nested children should complete fully before parent success and exit handlers");
        }

        [Test]
        public void DeepSequenceLifecycle_FailureBubblesUpAfterChildrenCleanUp()
        {
            var events = new List<string>();

            var root = Sequence("Root Failure", () =>
            {
                OnEnter(() => events.Add("root-enter"));
                OnFailure(() => events.Add("root-failure"));
                OnExit(() => events.Add("root-exit"));

                Leaf("LeafA", () =>
                {
                    OnEnter(() => events.Add("leafA-enter"));
                    OnBaseTick(() =>
                    {
                        events.Add("leafA-base");
                        return Status.Success;
                    });
                    OnSuccess(() => events.Add("leafA-success"));
                    OnExit(() => events.Add("leafA-exit"));
                });

                Sequence("FailingInner", () =>
                {
                    OnEnter(() => events.Add("inner-enter"));
                    OnFailure(() => events.Add("inner-failure"));
                    OnExit(() => events.Add("inner-exit"));

                    Leaf("InnerLeaf1", () =>
                    {
                        OnEnter(() => events.Add("innerLeaf1-enter"));
                        OnBaseTick(() =>
                        {
                            events.Add("innerLeaf1-base");
                            return Status.Success;
                        });
                        OnSuccess(() => events.Add("innerLeaf1-success"));
                        OnExit(() => events.Add("innerLeaf1-exit"));
                    });

                    Leaf("InnerLeaf2", () =>
                    {
                        OnEnter(() => events.Add("innerLeaf2-enter"));
                        OnBaseTick(() =>
                        {
                            events.Add("innerLeaf2-base");
                            return Status.Failure;
                        });
                        OnFailure(() => events.Add("innerLeaf2-failure"));
                        OnExit(() => events.Add("innerLeaf2-exit"));
                    });
                });
            });

            var status = TickNode(root);

            Assert.AreEqual(Status.Failure, status, "Tree should fail because the inner sequence fails");
            CollectionAssert.AreEqual(
                new[]
                {
                    "root-enter",
                    "leafA-enter",
                    "leafA-base",
                    "leafA-success",
                    "leafA-exit",
                    "inner-enter",
                    "innerLeaf1-enter",
                    "innerLeaf1-base",
                    "innerLeaf1-success",
                    "innerLeaf1-exit",
                    "innerLeaf2-enter",
                    "innerLeaf2-base",
                    "innerLeaf2-failure",
                    "innerLeaf2-exit",
                    "inner-failure",
                    "inner-exit",
                    "root-failure",
                    "root-exit"
                },
                events,
                "All child cleanup should complete before parent failure and exit fire");
            Assert.Less(events.IndexOf("innerLeaf2-exit"), events.IndexOf("inner-exit"),
                "Inner composite exit should follow child exit");
            Assert.Less(events.IndexOf("inner-exit"), events.IndexOf("root-exit"),
                "Root exit should be the final cleanup step");
        }

        [Test]
        public void SubStatus_TracksRunningDoneAndReset()
        {
            var tickCount = 0;

            var leaf = Leaf("SubStatus Leaf", () =>
            {
                OnBaseTick(() =>
                {
                    tickCount++;
                    return tickCount >= 2 ? Status.Success : Status.Running;
                });
            });

            leaf.Tick(out _);
            Assert.AreEqual(SubStatus.Running, leaf.SubStatus, "Leaf should report Running substatus while executing");

            leaf.Tick(out _);
            Assert.AreEqual(SubStatus.Done, leaf.SubStatus, "Leaf should be Done after finishing");

            leaf.ResetImmediately();
            Assert.AreEqual(SubStatus.None, leaf.SubStatus, "Reset should clear substatus to None");
        }

        [Test]
        public void AllowReEnter_ReplaysEnterWithoutOnEnabled()
        {
            var enabledCount = 0;
            var enterCount = 0;

            var leaf = Leaf("ReEnter Leaf", () =>
            {
                OnEnabled(() => enabledCount++);
                OnEnter(() => enterCount++);
                OnBaseTick(() => Status.Success);
            });

            leaf.Tick();
            Assert.AreEqual(1, enabledCount, "First activation should call OnEnabled once");
            Assert.AreEqual(1, enterCount, "First activation should call OnEnter once");

            leaf.Tick(out _, allowReEnter: true);

            Assert.AreEqual(1, enabledCount, "allowReEnter must not trigger OnEnabled again");
            Assert.AreEqual(2, enterCount, "allowReEnter should call OnEnter again");

            leaf.Tick(out _, allowReEnter: true);
            Assert.AreEqual(SubStatus.Done, leaf.SubStatus, "Leaf should complete normally after re-entry");
        }
    }
}
