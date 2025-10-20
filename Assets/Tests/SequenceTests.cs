using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ClosureAI.AI;

namespace ClosureAI.Tests
{
    /// <summary>
    /// Tests for Sequence composite node behavior.
    /// Based on TEST_CONTEXT.md specifications:
    /// - Sequence executes children in order
    /// - Fails if any child fails
    /// - Succeeds when all children succeed
    /// - Returns Running while executing
    /// </summary>
    [TestFixture]
    public class SequenceTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up any nodes created during tests
        }

        /// <summary>
        /// Ticks a node until it completes (returns Success or Failure) or reaches max iterations.
        /// Useful for nodes that may need multiple ticks to resolve (e.g., with Running states).
        /// </summary>
        /// <param name="node">The node to tick</param>
        /// <param name="maxIterations">Maximum number of ticks to attempt (default: 100)</param>
        /// <returns>The final status of the node</returns>
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

        #region Basic Sequence Tests

        [Test]
        public void Sequence_WithNoChildren_ReturnsSuccess()
        {
            // Arrange
            var sequence = Sequence(() => { });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, status, "Empty sequence should return Success");
        }

        [Test]
        public void Sequence_WithSingleSuccessChild_ReturnsSuccess()
        {
            // Arrange
            var childExecuted = false;
            var sequence = Sequence(() =>
            {
                Do(() => childExecuted = true);
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, status, "Sequence should succeed when child succeeds");
            Assert.IsTrue(childExecuted, "Child node should have been executed");
        }

        [Test]
        public void Sequence_WithMultipleSuccessChildren_ExecutesInOrder()
        {
            // Arrange
            var executionOrder = new List<int>();
            var sequence = Sequence(() =>
            {
                Do(() => executionOrder.Add(1));
                Do(() => executionOrder.Add(2));
                Do(() => executionOrder.Add(3));
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, status, "Sequence should succeed when all children succeed");
            Assert.AreEqual(3, executionOrder.Count, "All three children should execute");
            Assert.AreEqual(new[] { 1, 2, 3 }, executionOrder.ToArray(), "Children should execute in order");
        }

        [Test]
        public void Sequence_WithFailingChild_StopsAndReturnFailure()
        {
            // Arrange
            var executionOrder = new List<int>();
            var sequence = Sequence(() =>
            {
                Do(() => executionOrder.Add(1));
                Condition(() =>
                {
                    executionOrder.Add(2);
                    return false; // Fails
                });
                Do(() => executionOrder.Add(3)); // Should not execute
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Failure, status, "Sequence should fail when a child fails");
            Assert.AreEqual(new[] { 1, 2 }, executionOrder.ToArray(), "Third child should not execute after failure");
        }

        [Test]
        public void Sequence_WithRunningChild_ReturnsRunning()
        {
            // Arrange
            var tickCount = 0;
            var sequence = Sequence(() =>
            {
                Do(() => tickCount++);
                Wait(1f); // Will return Running
                Do(() => tickCount++); // Should not execute yet
            });

            // Act - First tick
            var hasValue = sequence.Tick(out var status);

            // Assert
            Assert.IsFalse(hasValue, "Sequence with Wait should return Running");
            Assert.AreEqual(Status.Running, status, "Sequence should be Running when child is Running");
            Assert.AreEqual(1, tickCount, "Only first child should execute on first tick");
        }

        [Test]
        public void Sequence_ResumesFromRunningChild()
        {
            // Arrange
            var executionOrder = new List<int>();
            var waitTicks = 0;
            var sequence = Sequence(() =>
            {
                Do(() => executionOrder.Add(1));
                Leaf("Custom Running", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add(2);
                        waitTicks++;
                        return waitTicks >= 3 ? Status.Success : Status.Running;
                    });
                });
                Do(() => executionOrder.Add(3));
            });

            // Act
            var finalStatus = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, finalStatus, "Sequence should succeed after all children succeed");
            Assert.AreEqual(new[] { 1, 2, 2, 2, 3 }, executionOrder.ToArray(),
                "Sequence should resume Running child, not restart from beginning");
        }

        #endregion

        #region Nested Sequence Tests

        [Test]
        public void NestedSequence_AllSuccess_ReturnsSuccess()
        {
            // Arrange
            var executionOrder = new List<string>();
            var sequence = Sequence("Outer", () =>
            {
                Do(() => executionOrder.Add("outer-1"));

                Sequence("Inner", () =>
                {
                    Do(() => executionOrder.Add("inner-1"));
                    Do(() => executionOrder.Add("inner-2"));
                });

                Do(() => executionOrder.Add("outer-2"));
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, status, "Outer sequence should succeed");
            Assert.AreEqual(new[] { "outer-1", "inner-1", "inner-2", "outer-2" }, executionOrder.ToArray(),
                "Execution should proceed through nested sequence in order");
        }

        [Test]
        public void NestedSequence_InnerFails_OuterFails()
        {
            // Arrange
            var executionOrder = new List<string>();
            var sequence = Sequence("Outer", () =>
            {
                Do(() => executionOrder.Add("outer-1"));

                Sequence("Inner", () =>
                {
                    Do(() => executionOrder.Add("inner-1"));
                    Condition(() =>
                    {
                        executionOrder.Add("inner-2-fail");
                        return false;
                    });
                    Do(() => executionOrder.Add("inner-3")); // Should not execute
                });

                Do(() => executionOrder.Add("outer-2")); // Should not execute
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Failure, status, "Outer sequence should fail when inner fails");
            Assert.AreEqual(new[] { "outer-1", "inner-1", "inner-2-fail" }, executionOrder.ToArray(),
                "Execution should stop at inner failure");
        }

        [Test]
        public void NestedSequence_InnerRunning_OuterRunning()
        {
            // Arrange
            var executionOrder = new List<string>();
            var sequence = Sequence("Outer", () =>
            {
                Do(() => executionOrder.Add("outer-1"));

                Sequence("Inner", () =>
                {
                    Do(() => executionOrder.Add("inner-1"));
                    JustRunning("Always Running", () =>
                    {
                        OnTick(() =>
                        {
                            executionOrder.Add("inner-running");
                        });
                    });
                });

                Do(() => executionOrder.Add("outer-2")); // Should not execute
            });

            // Act - Tick multiple times
            for (int i = 0; i < 5; i++)
            {
                sequence.Tick(out var status);
                Assert.AreEqual(Status.Running, status, $"Sequence should be Running on tick {i + 1}");
            }

            // Assert - The first ticks go through Enabling/Entering lifecycle, then "inner-running" fires each tick
            // Expected: outer-1 executes, inner-1 executes, then inner-running ticks multiple times
            CollectionAssert.Contains(executionOrder, "outer-1");
            CollectionAssert.Contains(executionOrder, "inner-1");
            Assert.Greater(executionOrder.Count(s => s == "inner-running"), 0,
                "Inner running node should tick at least once");
            Assert.IsFalse(executionOrder.Contains("outer-2"),
                "Outer-2 should not execute while inner is running");
        }

        [Test]
        public void DeeplyNestedSequences_ExecuteInOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            var sequence = Sequence("Level1", () =>
            {
                Do(() => executionOrder.Add("L1-1"));

                Sequence("Level2", () =>
                {
                    Do(() => executionOrder.Add("L2-1"));

                    Sequence("Level3", () =>
                    {
                        Do(() => executionOrder.Add("L3-1"));
                        Do(() => executionOrder.Add("L3-2"));
                    });

                    Do(() => executionOrder.Add("L2-2"));
                });

                Do(() => executionOrder.Add("L1-2"));
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, status, "Deeply nested sequence should succeed");
            Assert.AreEqual(new[] { "L1-1", "L2-1", "L3-1", "L3-2", "L2-2", "L1-2" },
                executionOrder.ToArray(),
                "Execution should proceed depth-first through all levels");
        }

        [Test]
        public void MultipleNestedSequences_ExecuteInOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            var sequence = Sequence("Outer", () =>
            {
                Do(() => executionOrder.Add("outer-1"));

                Sequence("Inner-1", () =>
                {
                    Do(() => executionOrder.Add("inner1-1"));
                    Do(() => executionOrder.Add("inner1-2"));
                });

                Do(() => executionOrder.Add("outer-2"));

                Sequence("Inner-2", () =>
                {
                    Do(() => executionOrder.Add("inner2-1"));
                    Do(() => executionOrder.Add("inner2-2"));
                });

                Do(() => executionOrder.Add("outer-3"));
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, status, "Sequence with multiple nested sequences should succeed");
            Assert.AreEqual(new[] { "outer-1", "inner1-1", "inner1-2", "outer-2", "inner2-1", "inner2-2", "outer-3" },
                executionOrder.ToArray(),
                "All sequences should execute in order");
        }

        [Test]
        public void NestedSequence_WithRunningInnerNode_ResumesCorrectly()
        {
            // Arrange
            var executionOrder = new List<string>();
            var innerTicks = 0;

            var sequence = Sequence("Outer", () =>
            {
                Do(() => executionOrder.Add("outer-1"));

                Sequence("Inner", () =>
                {
                    Do(() => executionOrder.Add("inner-1"));

                    Leaf("Running Leaf", () =>
                    {
                        OnBaseTick(() =>
                        {
                            executionOrder.Add($"inner-tick-{++innerTicks}");
                            return innerTicks >= 3 ? Status.Success : Status.Running;
                        });
                    });

                    Do(() => executionOrder.Add("inner-3"));
                });

                Do(() => executionOrder.Add("outer-2"));
            });

            // Act
            var finalStatus = TickNode(sequence, 50);

            // Assert
            Assert.AreEqual(Status.Success, finalStatus, "Sequence should succeed");
            Assert.AreEqual(
                new[] { "outer-1", "inner-1", "inner-tick-1", "inner-tick-2", "inner-tick-3", "inner-3", "outer-2" },
                executionOrder.ToArray(),
                "Nested sequence should resume inner running node correctly");
        }

        #endregion

        #region Lifecycle Tests

        [Test]
        public void Sequence_LifecycleOrder_OnEnterBeforeTick()
        {
            // Arrange
            var events = new List<string>();
            var sequence = Sequence(() =>
            {
                Leaf("Test", () =>
                {
                    OnEnter(() => events.Add("enter"));
                    OnTick(() => events.Add("tick"));
                    OnBaseTick(() => Status.Success);
                });
            });

            // Act
            TickNode(sequence);

            // Assert
            Assert.AreEqual(new[] { "enter", "tick" }, events.ToArray(),
                "OnEnter should execute before OnTick");
        }

        [Test]
        public void Sequence_LifecycleOrder_OnExitAfterCompletion()
        {
            // Arrange
            var events = new List<string>();
            var sequence = Sequence(() =>
            {
                Leaf("Test", () =>
                {
                    OnEnter(() => events.Add("enter"));
                    OnBaseTick(() =>
                    {
                        events.Add("tick");
                        return Status.Success;
                    });
                    OnExit(() => events.Add("exit"));
                });
            });

            // Act
            TickNode(sequence);

            // Assert
            Assert.AreEqual(new[] { "enter", "tick", "exit" }, events.ToArray(),
                "OnExit should execute after node completes");
        }

        [Test]
        public void Sequence_OnSuccess_CalledWhenSucceeds()
        {
            // Arrange
            var successCalled = false;
            var failureCalled = false;
            var sequence = Sequence(() =>
            {
                Leaf("Test", () =>
                {
                    OnBaseTick(() => Status.Success);
                    OnSuccess(() => successCalled = true);
                    OnFailure(() => failureCalled = true);
                });
            });

            // Act
            var status = TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, status);
            Assert.IsTrue(successCalled, "OnSuccess should be called");
            Assert.IsFalse(failureCalled, "OnFailure should not be called");
        }

        #endregion

        #region Edge Cases

        [UnityTest]
        public IEnumerator Sequence_ResetBetweenTicks_StartsFromBeginning()
        {
            // Arrange
            var executionCount = 0;
            var sequence = Sequence(() =>
            {
                Do(() => executionCount++);
            });

            // Act
            TickNode(sequence);
            Assert.AreEqual(1, executionCount, "Should execute once on first tick");

            sequence.ResetImmediately();

            while (sequence.Resetting)
                yield return null;

            TickNode(sequence);

            // Assert
            Assert.AreEqual(2, executionCount, "Should execute again after reset");
        }

        [Test]
        public void Sequence_WithNamedNodes_MaintainsNames()
        {
            // Arrange
            var sequence = Sequence("Outer Sequence", () =>
            {
                Sequence("Inner Sequence", () =>
                {
                    Do(() => { });
                });
            });

            // Assert
            Assert.AreEqual("Outer Sequence", sequence.Name);
        }

        #endregion

        #region Advanced Sequence Tests

        [Test]
        public void Sequence_AllowReEnter_SkipsOnEnabled()
        {
            // Arrange
            var events = new List<string>();
            var sequence = Sequence(() =>
            {
                Leaf("Test", () =>
                {
                    OnEnabled(() => events.Add("enabled"));
                    OnEnter(() => events.Add("enter"));
                    OnBaseTick(() =>
                    {
                        events.Add("tick");
                        return Status.Success;
                    });
                    OnExit(() => events.Add("exit"));
                });
            });

            // Act - First run
            TickNode(sequence);
            Assert.AreEqual(new[] { "enabled", "enter", "tick", "exit" }, events.ToArray());

            events.Clear();

            // Act - Re-enter with allowReEnter
            sequence.Tick(out _, allowReEnter: true);
            TickNode(sequence);

            // Assert - OnEnabled should NOT be called again
            Assert.AreEqual(new[] { "enter", "tick", "exit" }, events.ToArray(),
                "allowReEnter should skip OnEnabled and go straight to OnEnter");
        }

        [Test]
        public void Sequence_Variables_PersistAcrossTicks()
        {
            // Arrange
            var tickCount = 0;
            var sequence = Sequence(() =>
            {
                var counter = Variable(() => 0);

                Leaf("Incrementer", () =>
                {
                    OnBaseTick(() =>
                    {
                        counter.Value++;
                        tickCount++;
                        return tickCount >= 3 ? Status.Success : Status.Running;
                    });
                });

                Leaf("Validator", () =>
                {
                    OnBaseTick(() =>
                    {
                        // Counter should be 3 from previous node
                        Assert.AreEqual(3, counter.Value, "Variable should persist across child nodes");
                        return Status.Success;
                    });
                });
            });

            // Act
            TickNode(sequence);

            // Assert
            Assert.AreEqual(Status.Success, sequence.Status);
        }

        [Test]
        public void Sequence_OnTick_FiresAfterBaseTick()
        {
            // Arrange
            var events = new List<string>();
            var sequence = Sequence(() =>
            {
                OnTick(() => events.Add("parent-tick"));

                Do(() => events.Add("child-1"));
                Do(() => events.Add("child-2"));
            });

            // Act
            TickNode(sequence);

            // Assert - Parent OnTick fires after BaseTick (which executes children)
            // Order: OnPreTick -> BaseTick (children execute) -> OnTick
            Assert.AreEqual(new[] { "child-1", "child-2", "parent-tick" }, events.ToArray(),
                "Parent OnTick should fire after children execute (after BaseTick)");
        }

        [Test]
        public void Sequence_OnPreTick_FiresBeforeBaseTick()
        {
            // Arrange
            var events = new List<string>();
            var sequence = Sequence(() =>
            {
                Leaf("Test", () =>
                {
                    OnPreTick(() => events.Add("pre-tick"));
                    OnBaseTick(() =>
                    {
                        events.Add("base-tick");
                        return Status.Success;
                    });
                });
            });

            // Act
            TickNode(sequence);

            // Assert
            Assert.AreEqual(new[] { "pre-tick", "base-tick" }, events.ToArray(),
                "OnPreTick should execute before OnBaseTick");
        }

        [Test]
        public void Sequence_MixedRunningAtDifferentDepths_ResolvesCorrectly()
        {
            // Arrange
            var executionOrder = new List<string>();
            var level1Ticks = 0;
            var level3Ticks = 0;

            var sequence = Sequence("Root", () =>
            {
                Do(() => executionOrder.Add("root-1"));

                Sequence("Level1", () =>
                {
                    Do(() => executionOrder.Add("L1-1"));

                    Leaf("L1-Running", () =>
                    {
                        OnBaseTick(() =>
                        {
                            executionOrder.Add("L1-running");
                            level1Ticks++;
                            return level1Ticks >= 2 ? Status.Success : Status.Running;
                        });
                    });

                    Sequence("Level2", () =>
                    {
                        Do(() => executionOrder.Add("L2-1"));

                        Sequence("Level3", () =>
                        {
                            Leaf("L3-Running", () =>
                            {
                                OnBaseTick(() =>
                                {
                                    executionOrder.Add("L3-running");
                                    level3Ticks++;
                                    return level3Ticks >= 2 ? Status.Success : Status.Running;
                                });
                            });
                        });
                    });
                });

                Do(() => executionOrder.Add("root-2"));
            });

            // Act
            TickNode(sequence, 50);

            // Assert
            Assert.AreEqual(Status.Success, sequence.Status);
            // Should execute: root-1, L1-1, L1-running (2x), L2-1, L3-running (2x), root-2
            CollectionAssert.Contains(executionOrder, "root-1");
            CollectionAssert.Contains(executionOrder, "L1-running");
            CollectionAssert.Contains(executionOrder, "L3-running");
            CollectionAssert.Contains(executionOrder, "root-2");
        }

        [Test]
        public void Sequence_PartialCompletion_ThenReset_StartsFromBeginning()
        {
            // Arrange
            var executionCounts = new Dictionary<string, int>
            {
                ["child-1"] = 0,
                ["child-2"] = 0,
                ["child-3"] = 0
            };

            var sequence = Sequence(() =>
            {
                Do(() => executionCounts["child-1"]++);

                Leaf("Running", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionCounts["child-2"]++;
                        // Never completes
                        return Status.Running;
                    });
                });

                Do(() => executionCounts["child-3"]++);
            });

            // Act - Run until stuck on running child
            for (int i = 0; i < 5; i++)
                sequence.Tick();

            Assert.AreEqual(1, executionCounts["child-1"], "First child should execute once");
            Assert.AreEqual(5, executionCounts["child-2"], "Running child should tick multiple times");
            Assert.AreEqual(0, executionCounts["child-3"], "Third child should not execute yet");

            // Reset
            sequence.ResetImmediately();

            // Act - Run again
            for (int i = 0; i < 3; i++)
                sequence.Tick();

            // Assert - Should start from beginning
            Assert.AreEqual(2, executionCounts["child-1"], "First child should execute again after reset");
            Assert.AreEqual(8, executionCounts["child-2"], "Running child should continue from reset");
        }

        [Test]
        public void Sequence_BlockReEnter_PreventsSameFrameReExecution()
        {
            // Arrange
            var executionCount = 0;
            var sequence = Sequence(() =>
            {
                Do(() => executionCount++);
            });

            // Act - First tick completes
            var done = sequence.Tick(out var status);
            Assert.IsTrue(done);
            Assert.AreEqual(Status.Success, status);
            Assert.AreEqual(1, executionCount);

            // Try to tick again immediately (should be blocked)
            done = sequence.Tick(out status);
            Assert.IsTrue(done);
            Assert.AreEqual(Status.Success, status);
            Assert.AreEqual(1, executionCount, "BlockReEnter should prevent immediate re-execution");

            // Now with allowReEnter, it should work
            done = sequence.Tick(out status, allowReEnter: true);
            TickNode(sequence);
            Assert.AreEqual(2, executionCount, "allowReEnter should bypass BlockReEnter");
        }

        [Test]
        public void Sequence_VariablesReinitialize_OnAllowReEnter()
        {
            // Arrange
            var initCount = 0;
            var sequence = Sequence(() =>
            {
                var value = Variable(() =>
                {
                    initCount++;
                    return 42;
                });

                Leaf("Check", () =>
                {
                    OnEnabled(() =>
                    {
                        Assert.AreEqual(42, value.Value);
                    });

                    OnBaseTick(() =>
                    {
                        value.Value = 99; // Modify it
                        return Status.Success;
                    });
                });
            });

            // Act - First run
            TickNode(sequence);
            Assert.AreEqual(1, initCount, "Variable should initialize once");

            // Act - Re-enter
            sequence.Tick(out _, allowReEnter: true);
            TickNode(sequence);

            // Assert - Variables do NOT reinitialize on allowReEnter (only when Status==None)
            Assert.AreEqual(1, initCount, "Variable should NOT reinitialize on allowReEnter");
        }

        [Test]
        public void Sequence_MultipleRunningChildren_InDifferentBranches()
        {
            // Arrange
            var executionOrder = new List<string>();
            var branch1Ticks = 0;
            var branch2Ticks = 0;

            var sequence = Sequence(() =>
            {
                Sequence("Branch1", () =>
                {
                    Do(() => executionOrder.Add("b1-start"));

                    Leaf("B1-Running", () =>
                    {
                        OnBaseTick(() =>
                        {
                            executionOrder.Add("b1-running");
                            branch1Ticks++;
                            return branch1Ticks >= 2 ? Status.Success : Status.Running;
                        });
                    });

                    Do(() => executionOrder.Add("b1-end"));
                });

                Sequence("Branch2", () =>
                {
                    Do(() => executionOrder.Add("b2-start"));

                    Leaf("B2-Running", () =>
                    {
                        OnBaseTick(() =>
                        {
                            executionOrder.Add("b2-running");
                            branch2Ticks++;
                            return branch2Ticks >= 3 ? Status.Success : Status.Running;
                        });
                    });

                    Do(() => executionOrder.Add("b2-end"));
                });
            });

            // Act
            TickNode(sequence, 50);

            // Assert
            Assert.AreEqual(Status.Success, sequence.Status);
            Assert.AreEqual(2, branch1Ticks, "Branch1 running node should tick twice");
            Assert.AreEqual(3, branch2Ticks, "Branch2 running node should tick three times");
            CollectionAssert.Contains(executionOrder, "b1-end");
            CollectionAssert.Contains(executionOrder, "b2-end");
        }

        #endregion
    }
}
