using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ClosureAI.AI;

namespace ClosureAI.Tests
{
    [TestFixture]
    public class AsyncCancellationTests
    {
        [Test]
        public void AsyncLeaf_CompletesAfterAwaitedTicks()
        {
            var log = new List<string>();

            var leaf = Leaf("Async Leaf", () =>
            {
                OnEnter(() => log.Add("enter"));

                OnBaseTick(async (ct, tick) =>
                {
                    log.Add("tick-start");
                    await tick();
                    log.Add("tick-mid");
                    await tick();
                    log.Add("tick-end");
                    return Status.Success;
                });

                OnSuccess(() => log.Add("success"));
                OnExit(() => log.Add("exit"));
            });

            Assert.AreEqual(Status.Running, TickOnce(leaf), "First tick should start async work and remain running");
            CollectionAssert.AreEqual(new[] { "enter", "tick-start" }, log);

            Assert.AreEqual(Status.Running, TickOnce(leaf), "Awaited tick should resume but still be running");
            CollectionAssert.AreEqual(new[] { "enter", "tick-start", "tick-mid" }, log);

            Assert.AreEqual(Status.Success, TickOnce(leaf), "Second awaited tick should allow completion");
            CollectionAssert.AreEqual(new[] { "enter", "tick-start", "tick-mid", "tick-end", "success", "exit" }, log);
            Assert.AreEqual(Status.Success, leaf.Status, "Leaf should report success after completion");
            Assert.AreEqual(SubStatus.Done, leaf.SubStatus, "Leaf should be marked as done");
        }

        [UnityTest]
        public IEnumerator AsyncLeaf_ResetImmediatelyCancelsAwaitingTask()
        {
            var cancellationObserved = false;
            var exitStarted = false;
            var exitCompleted = false;

            var leaf = Leaf("Cancelable Leaf", () =>
            {
                OnBaseTick(async (ct, tick) =>
                {
                    try
                    {
                        while (true)
                            await tick();
                    }
                    catch (OperationCanceledException)
                    {
                        cancellationObserved = true;
                    }

                    return Status.Failure;
                });

                OnExit(async (ct) =>
                {
                    exitStarted = true;
                    try
                    {
                        await UniTask.WaitForSeconds(1f, cancellationToken: ct);
                    }
                    finally
                    {
                        exitCompleted = true;
                    }
                });
            });

            Assert.AreEqual(Status.Running, TickOnce(leaf), "Leaf should begin running on first tick");
            Assert.AreEqual(Status.Running, TickOnce(leaf), "Leaf remains running while awaiting ticks");

            leaf.ResetImmediately();

            var guard = 0;
            while (leaf.Resetting && guard++ < 20)
            {
                yield return null;
            }

            Assert.IsFalse(leaf.Resetting, "Leaf should finish resetting");
            Assert.IsTrue(cancellationObserved, "Cancellation token should propagate to async base tick");
            Assert.IsTrue(exitStarted, "OnExit coroutine should run during reset");
            Assert.IsTrue(exitCompleted, "OnExit coroutine should complete during reset");
            Assert.AreEqual(Status.None, leaf.Status, "After reset the node status should be cleared");
            Assert.AreEqual(SubStatus.None, leaf.SubStatus, "After reset the node sub-status should be cleared");
        }

        [UnityTest]
        public IEnumerator ReactiveInvalidation_CancelsAsyncChild()
        {
            var gate = true;
            var cancellationObserved = false;
            var exitCompleted = false;

            var tree = Reactive * Sequence("Root", () =>
            {
                Condition("Gate", () => gate);

                Leaf("AsyncChild", () =>
                {
                    OnBaseTick(async (ct, tick) =>
                    {
                        try
                        {
                            while (true)
                                await tick();
                        }
                        catch (OperationCanceledException)
                        {
                            cancellationObserved = true;
                        }

                        return Status.Success;
                    });

                    OnExit(async ct =>
                    {
                        try
                        {
                            await UniTask.Delay(25, cancellationToken: ct);
                        }
                        catch (OperationCanceledException)
                        {
                            // Cancellation during exit is expected when the reactive parent invalidates
                        }
                        finally
                        {
                            exitCompleted = true;
                        }
                    });
                });
            });

            tree.Tick(out _);
            tree.Tick(out _);

            gate = false;

            var guard = 0;
            while (!cancellationObserved && guard++ < 50)
            {
                tree.Tick(out _);
                yield return null;
            }

            Assert.IsTrue(cancellationObserved, "Child should observe cancellation when upstream node invalidates");

            guard = 0;
            while (!exitCompleted && guard++ < 50)
            {
                tree.Tick(out _);
                yield return null;
            }

            Assert.IsTrue(exitCompleted, "Async OnExit should complete even when cancelled");
        }

        [UnityTest]
        public IEnumerator ConditionDecorator_CancelsChildWhenGateCloses()
        {
            var gate = true;
            var cancellationObserved = false;

            var sequence = Sequence("Conditioned Async", () =>
            {
                D.AlwaysSucceed();
                D.Condition(() => gate);
                Leaf("ConditionalAsync", () =>
                {
                    OnBaseTick(async (ct, tick) =>
                    {
                        try
                        {
                            while (true)
                            {
                                await tick();
                            }
                        }
                        finally
                        {
                            cancellationObserved = true;
                        }
                    });
                });
            });

            sequence.Tick(out _);
            gate = false;

            yield return null;
            yield return null;
            yield return null;

            var guard = 0;
            while (!cancellationObserved && guard++ < 50)
            {
                sequence.Tick(out _);
                yield return null;
            }

            Assert.IsTrue(cancellationObserved, "Decorator should cancel child when the gate closes");
        }

        private static Status TickOnce(Node node)
        {
            node.Tick(out var status);
            return status;
        }
    }
}
