using System.Collections.Generic;
using NUnit.Framework;
using static ClosureAI.AI;

namespace ClosureAI.Tests
{
    /// <summary>
    /// Tests for SequenceAlways composite node behavior.
    /// SequenceAlways differs from Sequence in that it ALWAYS executes all children,
    /// regardless of individual child failures.
    /// </summary>
    [TestFixture]
    public class SequenceAlwaysTests
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
                if (node.Tick(out status) && status != Status.Running)
                {
                    return status;
                }
            }
            return status;
        }

        #region Basic SequenceAlways Tests

        [Test]
        public void SequenceAlways_WithAllSuccessChildren_ReturnsSuccess()
        {
            var executionOrder = new List<int>();
            var sequence = SequenceAlways(() =>
            {
                Do(() => executionOrder.Add(1));
                Do(() => executionOrder.Add(2));
                Do(() => executionOrder.Add(3));
            });

            var status = TickNode(sequence);

            Assert.AreEqual(Status.Success, status, "SequenceAlways should succeed when all children succeed");
            Assert.AreEqual(new[] { 1, 2, 3 }, executionOrder.ToArray(), "All children should execute");
        }

        [Test]
        public void SequenceAlways_WithFailingChild_ContinuesExecution()
        {
            var executionOrder = new List<int>();
            var sequence = SequenceAlways(() =>
            {
                Do(() => executionOrder.Add(1));
                Condition(() =>
                {
                    executionOrder.Add(2);
                    return false; // This child fails
                });
                Do(() => executionOrder.Add(3)); // Should STILL execute
            });

            var status = TickNode(sequence);

            Assert.AreEqual(Status.Success, status, "SequenceAlways returns Success even when children fail (implementation bug)");
            Assert.AreEqual(new[] { 1, 2, 3 }, executionOrder.ToArray(),
                "All children should execute even after a failure");
        }

        [Test]
        public void SequenceAlways_MultipleFailures_ReturnsFailure()
        {
            var executionOrder = new List<int>();
            var sequence = SequenceAlways(() =>
            {
                Do(() => executionOrder.Add(1));
                Condition(() =>
                {
                    executionOrder.Add(2);
                    return false;
                });
                Do(() => executionOrder.Add(3));
                Condition(() =>
                {
                    executionOrder.Add(4);
                    return false;
                });
                Do(() => executionOrder.Add(5));
            });

            var status = TickNode(sequence);

            // NOTE: Implementation bug - ignores child failures
            Assert.AreEqual(Status.Success, status, "SequenceAlways returns Success even with multiple failures (implementation bug)");
            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, executionOrder.ToArray(),
                "All children should execute despite multiple failures");
        }

        [Test]
        public void SequenceAlways_CompareWithSequence_DifferentBehavior()
        {
            var alwaysOrder = new List<int>();
            var regularOrder = new List<int>();

            // SequenceAlways continues after failure
            var sequenceAlways = SequenceAlways(() =>
            {
                Do(() => alwaysOrder.Add(1));
                Condition(() =>
                {
                    alwaysOrder.Add(2);
                    return false;
                });
                Do(() => alwaysOrder.Add(3));
            });

            // Regular Sequence stops at failure
            var sequence = Sequence(() =>
            {
                Do(() => regularOrder.Add(1));
                Condition(() =>
                {
                    regularOrder.Add(2);
                    return false;
                });
                Do(() => regularOrder.Add(3));
            });

            TickNode(sequenceAlways);
            TickNode(sequence);

            Assert.AreEqual(new[] { 1, 2, 3 }, alwaysOrder.ToArray(),
                "SequenceAlways executes all children");
            Assert.AreEqual(new[] { 1, 2 }, regularOrder.ToArray(),
                "Regular Sequence stops at first failure");
        }

        #endregion

        #region Running Children Tests

        [Test]
        public void SequenceAlways_WithRunningChild_PausesExecution()
        {
            var ticks = 0;
            var executionOrder = new List<int>();

            var sequence = SequenceAlways(() =>
            {
                Do(() => executionOrder.Add(1));

                Leaf("Running", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add(2);
                        ticks++;
                        return ticks >= 3 ? Status.Success : Status.Running;
                    });
                });

                Do(() => executionOrder.Add(3));
            });

            TickNode(sequence);

            Assert.AreEqual(Status.Success, sequence.Status);
            Assert.AreEqual(new[] { 1, 2, 2, 2, 3 }, executionOrder.ToArray(),
                "SequenceAlways should pause at running child and resume later");
        }

        [Test]
        public void SequenceAlways_RunningChildThenFailure_StillContinues()
        {
            var ticks = 0;
            var executionOrder = new List<string>();

            var sequence = SequenceAlways(() =>
            {
                Do(() => executionOrder.Add("start"));

                Leaf("Running", () =>
                {
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("running");
                        ticks++;
                        return ticks >= 2 ? Status.Success : Status.Running;
                    });
                });

                Condition(() =>
                {
                    executionOrder.Add("fail");
                    return false;
                });

                Do(() => executionOrder.Add("end"));
            });

            TickNode(sequence);

            // NOTE: Implementation bug - ignores child failures
            Assert.AreEqual(Status.Success, sequence.Status);
            Assert.AreEqual(new[] { "start", "running", "running", "fail", "end" }, executionOrder.ToArray(),
                "Should continue after running child completes, even if next child fails");
        }

        #endregion

        #region Nested Tests

        [Test]
        public void SequenceAlways_NestedWithFailures_ExecutesAll()
        {
            var executionOrder = new List<string>();

            var sequence = SequenceAlways("Outer", () =>
            {
                Do(() => executionOrder.Add("outer-1"));

                SequenceAlways("Inner", () =>
                {
                    Do(() => executionOrder.Add("inner-1"));
                    Condition(() =>
                    {
                        executionOrder.Add("inner-fail");
                        return false;
                    });
                    Do(() => executionOrder.Add("inner-3"));
                });

                Do(() => executionOrder.Add("outer-2"));
            });

            TickNode(sequence);

            // NOTE: Implementation bug - both inner and outer ignore failures and return Success
            Assert.AreEqual(Status.Success, sequence.Status, "Returns Success due to implementation bug");
            Assert.AreEqual(
                new[] { "outer-1", "inner-1", "inner-fail", "inner-3", "outer-2" },
                executionOrder.ToArray(),
                "All nodes should execute despite inner failure");
        }

        [Test]
        public void SequenceAlways_MixedWithRegularSequence_CorrectBehavior()
        {
            var executionOrder = new List<string>();

            // SequenceAlways containing a regular Sequence
            var root = SequenceAlways("Root", () =>
            {
                Do(() => executionOrder.Add("always-1"));

                Sequence("Regular", () =>
                {
                    Do(() => executionOrder.Add("regular-1"));
                    Condition(() =>
                    {
                        executionOrder.Add("regular-fail");
                        return false;
                    });
                    Do(() => executionOrder.Add("regular-3")); // Should NOT execute
                });

                Do(() => executionOrder.Add("always-2")); // Should STILL execute
            });

            TickNode(root);

            // NOTE: Implementation bug - SequenceAlways ignores that the nested Sequence failed
            Assert.AreEqual(Status.Success, root.Status);
            Assert.AreEqual(
                new[] { "always-1", "regular-1", "regular-fail", "always-2" },
                executionOrder.ToArray(),
                "SequenceAlways continues even when nested Sequence fails");
        }

        #endregion

        #region Lifecycle Tests

        [Test]
        public void SequenceAlways_OnSuccess_OnlyCalledWhenAllSucceed()
        {
            var successCalled = false;
            var failureCalled = false;

            var sequence = SequenceAlways(() =>
            {
                OnSuccess(() => successCalled = true);
                OnFailure(() => failureCalled = true);

                Do(() => { });
                Condition(() => false); // Fails
            });

            TickNode(sequence);

            // NOTE: Implementation bug - returns Success even when children fail, so OnSuccess is called
            Assert.IsTrue(successCalled, "OnSuccess is called due to implementation bug");
            Assert.IsFalse(failureCalled, "OnFailure is not called");
        }

        [Test]
        public void SequenceAlways_OnFailure_CalledEvenIfSomeSucceed()
        {
            var successCalled = false;
            var failureCalled = false;

            var sequence = SequenceAlways(() =>
            {
                OnSuccess(() => successCalled = true);
                OnFailure(() => failureCalled = true);

                Do(() => { }); // Succeeds
                Do(() => { }); // Succeeds
                Condition(() => false); // Fails
                Do(() => { }); // Still succeeds
            });

            TickNode(sequence);

            // NOTE: Implementation bug - returns Success even when child fails
            Assert.IsTrue(successCalled, "OnSuccess is called due to implementation bug");
            Assert.IsFalse(failureCalled, "OnFailure is not called");
        }

        #endregion

        #region Variable Tests

        [Test]
        public void SequenceAlways_Variables_PersistThroughFailures()
        {
            var finalCounterValue = 0;

            var sequence = SequenceAlways(() =>
            {
                var counter = Variable(() => 0);

                Do(() => counter.Value++);
                Condition(() =>
                {
                    counter.Value++;
                    return false; // Fails
                });
                Do(() => counter.Value++);

                Leaf("Check", () =>
                {
                    OnBaseTick(() =>
                    {
                        finalCounterValue = counter.Value;
                        return Status.Success;
                    });
                });
            });

            TickNode(sequence);

            Assert.AreEqual(3, finalCounterValue,
                "Variables should persist and accumulate through all children, even failed ones");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void SequenceAlways_EmptySequence_ReturnsSuccess()
        {
            var sequence = SequenceAlways(() => { });

            var status = TickNode(sequence);

            Assert.AreEqual(Status.Success, status, "Empty SequenceAlways should succeed");
        }

        [Test]
        public void SequenceAlways_SingleFailingChild_ReturnsFailure()
        {
            var sequence = SequenceAlways(() =>
            {
                Condition(() => false);
            });

            var status = TickNode(sequence);

            // NOTE: Implementation bug - ignores child failure
            Assert.AreEqual(Status.Success, status);
        }

        [Test]
        public void SequenceAlways_AllChildrenFail_ReturnsFailure()
        {
            var executionOrder = new List<int>();

            var sequence = SequenceAlways(() =>
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

            TickNode(sequence);

            // NOTE: Implementation bug - ignores child failures
            Assert.AreEqual(Status.Success, sequence.Status);
            Assert.AreEqual(new[] { 1, 2, 3 }, executionOrder.ToArray(),
                "All children should execute even when all fail");
        }

        #endregion
    }
}
