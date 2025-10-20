using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ClosureAI.AI;

namespace ClosureAI.Tests
{
    /// <summary>
    /// Tests for Selector composite node behavior.
    /// </summary>
    [TestFixture]
    public class SelectorTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up any nodes created during tests
        }

        private Status TickNode(Node node, int maxIterations = 100)
        {
            Status status = Status.Running;
            for (var i = 0; i < maxIterations; i++)
            {
                if (node.Tick(out status))
                {
                    return status;
                }
            }
            return status;
        }

        [Test]
        public void Selector_WithNoChildren_ReturnsFailure()
        {
            var selector = Selector(() => { });

            var status = TickNode(selector);

            Assert.AreEqual(Status.Failure, status, "Empty selector should return Failure");
        }

        [Test]
        public void Selector_WithSingleSuccessChild_ReturnsSuccess()
        {
            var childExecuted = false;
            var selector = Selector(() =>
            {
                Do(() => childExecuted = true);
            });

            var status = TickNode(selector);

            Assert.AreEqual(Status.Success, status, "Selector should succeed when child succeeds");
            Assert.IsTrue(childExecuted, "Child node should have been executed");
        }

        [Test]
        public void Selector_WithInitialFailure_ExecutesNextChild()
        {
            var executionOrder = new List<int>();
            var selector = Selector(() =>
            {
                Condition(() =>
                {
                    executionOrder.Add(1);
                    return false;
                });
                Do(() => executionOrder.Add(2));
                Do(() => executionOrder.Add(3));
            });

            var status = TickNode(selector);

            Assert.AreEqual(Status.Success, status, "Selector should stop after first success");
            Assert.AreEqual(new[] { 1, 2 }, executionOrder.ToArray(), "Selector should not execute children after success");
        }

        [Test]
        public void Selector_AllChildrenFail_ReturnsFailure()
        {
            var executionOrder = new List<int>();
            var selector = Selector(() =>
            {
                Condition(() =>
                {
                    executionOrder.Add(1);
                    return false;
                });
                Condition(() =>
                {
                    executionOrder.Add(2);
                    return false;
                });
                Condition(() =>
                {
                    executionOrder.Add(3);
                    return false;
                });
            });

            var status = TickNode(selector);

            Assert.AreEqual(Status.Failure, status, "Selector should fail when all children fail");
            Assert.AreEqual(new[] { 1, 2, 3 }, executionOrder.ToArray(), "Selector should evaluate all children when none succeed");
        }

        [Test]
        public void Selector_WithRunningChild_ReturnsRunning()
        {
            var executionOrder = new List<int>();
            var selector = Selector(() =>
            {
                Condition(() =>
                {
                    executionOrder.Add(1);
                    return false;
                });
                JustRunning("Running Child", () =>
                {
                    OnTick(() =>
                    {
                        if (!executionOrder.Contains(2))
                        {
                            executionOrder.Add(2);
                        }
                    });
                });
                Do(() => executionOrder.Add(3));
            });

            var hasValue = selector.Tick(out var status);

            for (var i = 0; i < 10; i++)
                hasValue = selector.Tick(out status);

            Assert.IsFalse(hasValue, "Selector should remain running when child is running");
            Assert.AreEqual(Status.Running, status, "Selector should be Running while child is Running");
            Assert.AreEqual(new[] { 1, 2 }, executionOrder.ToArray(), "Selector should not execute later children while running child is active");
        }

        [Test]
        public void Selector_ResumesRunningChildUntilCompletion()
        {
            var executionOrder = new List<int>();
            var runningTicks = 0;
            var selector = Selector(() =>
            {
                Condition(() =>
                {
                    executionOrder.Add(1);
                    return false;
                });
                Leaf("Running Leaf", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add(2);
                        runningTicks++;
                        return runningTicks >= 3 ? Status.Success : Status.Running;
                    });
                });
                Do(() => executionOrder.Add(3));
            });

            var finalStatus = TickNode(selector);

            Assert.AreEqual(Status.Success, finalStatus, "Selector should succeed when running child eventually succeeds");
            Assert.AreEqual(new[] { 1, 2, 2, 2 }, executionOrder.ToArray(), "Selector should resume running child without re-evaluating earlier failures");
        }

        [Test]
        public void NestedSelectors_EvaluateHierarchyCorrectly()
        {
            var executionOrder = new List<string>();
            var selector = Selector("Outer", () =>
            {
                Condition(() =>
                {
                    executionOrder.Add("outer-1-fail");
                    return false;
                });

                Selector("Inner", () =>
                {
                    Condition(() =>
                    {
                        executionOrder.Add("inner-1-fail");
                        return false;
                    });
                    Do(() => executionOrder.Add("inner-2-success"));
                    Do(() => executionOrder.Add("inner-3"));
                });

                Do(() => executionOrder.Add("outer-2"));
            });

            var status = TickNode(selector);

            Assert.AreEqual(Status.Success, status, "Outer selector should succeed when inner selector succeeds");
            Assert.AreEqual(new[] { "outer-1-fail", "inner-1-fail", "inner-2-success" }, executionOrder.ToArray(), "Selectors should stop evaluating once success is reached");
        }

        #region Advanced Selector Tests

        [Test]
        public void Selector_AllowReEnter_RetriesFailedChildren()
        {
            var events = new List<string>();
            var attempt = 0;

            var selector = Selector(() =>
            {
                Leaf("First Try", () =>
                {
                    OnEnter(() => events.Add($"enter-attempt-{++attempt}"));
                    OnBaseTick(() =>
                    {
                        events.Add("tick");
                        return attempt >= 2 ? Status.Success : Status.Failure;
                    });
                    OnExit(() => events.Add("exit"));
                });
            });

            // First run - should fail
            TickNode(selector);
            Assert.AreEqual(Status.Failure, selector.Status);
            Assert.AreEqual(1, attempt);

            events.Clear();

            // Re-enter - should try again and succeed
            selector.Tick(out _, allowReEnter: true);
            TickNode(selector);

            Assert.AreEqual(Status.Success, selector.Status);
            Assert.AreEqual(new[] { "enter-attempt-2", "tick", "exit" }, events.ToArray());
        }

        [Test]
        public void Selector_Variables_PersistAcrossFallbacks()
        {
            var selector = Selector(() =>
            {
                var attemptCount = Variable(() => 0);

                Condition(() =>
                {
                    attemptCount.Value++;
                    return false; // Always fail
                });

                Condition(() =>
                {
                    attemptCount.Value++;
                    return false; // Always fail
                });

                Leaf("Final", () =>
                {
                    OnBaseTick(() =>
                    {
                        attemptCount.Value++;
                        // Should be 3 (two conditions + this check)
                        Assert.AreEqual(3, attemptCount.Value, "Variable should persist across all selector children");
                        return Status.Success;
                    });
                });
            });

            TickNode(selector);
            Assert.AreEqual(Status.Success, selector.Status);
        }

        [Test]
        public void Selector_DeeplyNestedSelectors_CorrectFallbackHierarchy()
        {
            var executionOrder = new List<string>();

            var selector = Selector("L1", () =>
            {
                Condition(() =>
                {
                    executionOrder.Add("L1-C1-fail");
                    return false;
                });

                Selector("L2", () =>
                {
                    Condition(() =>
                    {
                        executionOrder.Add("L2-C1-fail");
                        return false;
                    });

                    Selector("L3", () =>
                    {
                        Condition(() =>
                        {
                            executionOrder.Add("L3-C1-fail");
                            return false;
                        });

                        Do(() => executionOrder.Add("L3-C2-success"));
                    });

                    Do(() => executionOrder.Add("L2-C2")); // Should not execute
                });

                Do(() => executionOrder.Add("L1-C2")); // Should not execute
            });

            TickNode(selector);

            Assert.AreEqual(Status.Success, selector.Status);
            Assert.AreEqual(
                new[] { "L1-C1-fail", "L2-C1-fail", "L3-C1-fail", "L3-C2-success" },
                executionOrder.ToArray(),
                "Should drill down to deepest level before succeeding");
        }

        [Test]
        public void Selector_RunningChildAtFirstPosition_ReturnsRunning()
        {
            var ticks = 0;
            var executionOrder = new List<int>();

            var selector = Selector(() =>
            {
                Leaf("First Running", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add(1);
                        ticks++;
                        return ticks >= 3 ? Status.Success : Status.Running;
                    });
                });

                Do(() => executionOrder.Add(2)); // Should not execute - Selector succeeds on first child
            });

            // Tick multiple times
            for (int i = 0; i < 5; i++)
            {
                selector.Tick(out var status);
                if (status != Status.Running)
                    break;
            }

            Assert.AreEqual(Status.Success, selector.Status);
            // Selector returns Success when first child succeeds, doesn't advance to second child
            Assert.AreEqual(new[] { 1, 1, 1 }, executionOrder.ToArray(),
                "Second child should NOT execute - Selector succeeds on first success");
        }

        [Test]
        public void Selector_RunningChildAtMiddlePosition_SkipsPreviousFailures()
        {
            var ticks = 0;
            var executionOrder = new List<string>();

            var selector = Selector(() =>
            {
                Condition(() =>
                {
                    executionOrder.Add("fail-1");
                    return false;
                });

                Condition(() =>
                {
                    executionOrder.Add("fail-2");
                    return false;
                });

                Leaf("Running", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("running");
                        ticks++;
                        return ticks >= 2 ? Status.Success : Status.Running;
                    });
                });

                Do(() => executionOrder.Add("should-not-execute"));
            });

            TickNode(selector);

            Assert.AreEqual(Status.Success, selector.Status);
            Assert.AreEqual(
                new[] { "fail-1", "fail-2", "running", "running" },
                executionOrder.ToArray(),
                "Selector should resume running child without re-evaluating failures");
        }

        [Test]
        public void Selector_AllChildrenRunningThenEventuallySucceed_CorrectBehavior()
        {
            var child1Ticks = 0;
            var child2Ticks = 0;
            var executionOrder = new List<string>();

            var selector = Selector(() =>
            {
                Leaf("Child1", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("c1");
                        child1Ticks++;
                        // Fails on third tick
                        if (child1Ticks >= 3)
                            return Status.Failure;
                        return Status.Running;
                    });
                });

                Leaf("Child2", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("c2");
                        child2Ticks++;
                        // Succeeds on second tick
                        return child2Ticks >= 2 ? Status.Success : Status.Running;
                    });
                });
            });

            TickNode(selector, 50);

            Assert.AreEqual(Status.Success, selector.Status);
            // Child1 runs 3 times (until it fails), then Child2 runs 2 times (until it succeeds)
            Assert.AreEqual(new[] { "c1", "c1", "c1", "c2", "c2" }, executionOrder.ToArray());
        }

        [Test]
        public void Selector_OnTick_FiresAfterBaseTick()
        {
            var events = new List<string>();

            var selector = Selector(() =>
            {
                OnTick(() => events.Add("parent-tick"));

                Condition(() =>
                {
                    events.Add("child-1");
                    return false;
                });

                Do(() => events.Add("child-2"));
            });

            TickNode(selector);

            Assert.AreEqual(Status.Success, selector.Status);
            // Order: OnPreTick -> BaseTick (children execute) -> OnTick
            Assert.AreEqual(new[] { "child-1", "child-2", "parent-tick" }, events.ToArray(),
                "Parent OnTick should fire after children execute (after BaseTick)");
        }

        [Test]
        public void Selector_MixedNestedSequenceAndSelector_ComplexFallback()
        {
            var executionOrder = new List<string>();

            var root = Selector("Root", () =>
            {
                // First option: Sequence that will fail
                Sequence("Seq1", () =>
                {
                    Do(() => executionOrder.Add("seq1-1"));
                    Condition(() =>
                    {
                        executionOrder.Add("seq1-2-fail");
                        return false;
                    });
                    Do(() => executionOrder.Add("seq1-3")); // Should not execute
                });

                // Second option: Another selector
                Selector("Sel2", () =>
                {
                    Condition(() =>
                    {
                        executionOrder.Add("sel2-1-fail");
                        return false;
                    });

                    // This sequence will succeed
                    Sequence("Seq2", () =>
                    {
                        Do(() => executionOrder.Add("seq2-1"));
                        Do(() => executionOrder.Add("seq2-2"));
                    });
                });
            });

            TickNode(root);

            Assert.AreEqual(Status.Success, root.Status);
            Assert.AreEqual(
                new[] { "seq1-1", "seq1-2-fail", "sel2-1-fail", "seq2-1", "seq2-2" },
                executionOrder.ToArray(),
                "Complex nested fallback should work correctly");
        }

        [Test]
        public void Selector_VariableScope_ChildrenDontShareParentVariables()
        {
            var parentVarValue = 0;
            var childVarValue = 0;

            var selector = Selector(() =>
            {
                var parentVar = Variable(() => 100);

                Leaf("Child", () =>
                {
                    var childVar = Variable(() => 200);

                    OnBaseTick(() =>
                    {
                        parentVarValue = parentVar.Value;
                        childVarValue = childVar.Value;
                        return Status.Success;
                    });
                });
            });

            TickNode(selector);

            Assert.AreEqual(100, parentVarValue, "Child should access parent variable");
            Assert.AreEqual(200, childVarValue, "Child should have its own variable");
        }

        #endregion
    }
}
