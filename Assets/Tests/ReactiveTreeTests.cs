using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ClosureAI.AI;

namespace ClosureAI.Tests
{
    /// <summary>
    /// Tests for Reactive tree behavior and invalidation.
    /// Reactive trees can detect when conditions change and re-evaluate portions of the tree.
    /// </summary>
    [TestFixture]
    public class ReactiveTreeTests
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

        #region Basic Reactive Tests

        [Test]
        public void Reactive_MarkingNodeAsReactive_SetsFlag()
        {
            var sequence = Reactive * Sequence(() =>
            {
                Do(() => { });
            });

            Assert.IsTrue(sequence.IsReactive, "Node marked with Reactive multiplier should have IsReactive flag set");
        }

        [Test]
        public void NonReactive_ExplicitlyDisablesReactivity()
        {
            var sequence = NonReactive * Sequence(() =>
            {
                Do(() => { });
            });

            Assert.IsFalse(sequence.IsReactive, "Node marked with NonReactive should not be reactive");
        }

        #endregion

        #region OnInvalidCheck Tests

        [Test]
        public void OnInvalidCheck_WhenConditionChanges_Invalidates()
        {
            var condition = true;
            var invalidateCalled = false;

            var node = Leaf("Test", () =>
            {
                OnInvalidCheck(() =>
                {
                    invalidateCalled = true;
                    return !condition; // Invalid when condition is false
                });

                OnBaseTick(() => Status.Success);
            });

            TickNode(node);
            Assert.IsFalse(invalidateCalled, "OnInvalidCheck shouldn't be called during normal execution");

            // Change condition
            condition = false;

            // Check if node is invalid
            var isInvalid = node.IsInvalid();

            Assert.IsTrue(invalidateCalled, "OnInvalidCheck should be called when checking invalidation");
            Assert.IsTrue(isInvalid, "Node should be invalid when condition changes");
        }

        [Test]
        public void OnInvalidCheck_ReturnsFalse_NodeStaysValid()
        {
            var node = Leaf("Test", () =>
            {
                OnInvalidCheck(() => false); // Always valid
                OnBaseTick(() => Status.Success);
            });

            TickNode(node);

            var isInvalid = node.IsInvalid();
            Assert.IsFalse(isInvalid, "Node should remain valid when OnInvalidCheck returns false");
        }

        #endregion

        #region Reactive Invalidation Tests

        [Test]
        public void ReactiveSequence_InvalidatedChild_ResetsSubsequentNodes()
        {
            var condition = true;
            var executionOrder = new List<string>();
            var child2ResetCalled = false;
            var child3ResetCalled = false;

            var sequence = Reactive * Sequence(() =>
            {
                Leaf("Child1", () =>
                {
                    OnInvalidCheck(() => !condition);
                    OnEnter(() => executionOrder.Add("child1-enter"));
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("child1-tick");
                        return Status.Success;
                    });
                });

                Leaf("Child2", () =>
                {
                    OnEnter(() => executionOrder.Add("child2-enter"));
                    OnExit(() =>
                    {
                        child2ResetCalled = true;
                        executionOrder.Add("child2-reset");
                    });
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("child2-tick");
                        return Status.Success;
                    });
                });

                Leaf("Child3", () =>
                {
                    OnEnter(() => executionOrder.Add("child3-enter"));
                    OnExit(() =>
                    {
                        child3ResetCalled = true;
                        executionOrder.Add("child3-reset");
                    });
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("child3-tick");
                        return Status.Running; // Stays running
                    });
                });
            });

            // First run - completes Child1, Child2, starts Child3
            sequence.Tick();
            sequence.Tick();

            executionOrder.Clear();

            // Invalidate Child1
            condition = false;

            // Next tick should detect invalidation
            sequence.Tick();

            Assert.IsTrue(child2ResetCalled, "Child2 should be reset when earlier node invalidates");
            Assert.IsTrue(child3ResetCalled, "Child3 should be reset when earlier node invalidates");
            CollectionAssert.Contains(executionOrder, "child1-enter",
                "Invalidated child should re-enter");
        }

        [Test]
        public void ReactiveSequence_InvalidatedChild_CallsOnEnterNotOnEnabled()
        {
            var condition = true;
            var events = new List<string>();

            var sequence = Reactive * Sequence(() =>
            {
                Leaf("Test", () =>
                {
                    OnInvalidCheck(() => !condition);
                    OnEnabled(() => events.Add("enabled"));
                    OnEnter(() => events.Add("enter"));
                    OnBaseTick(() => Status.Success);
                });

                // Add a Running child so the sequence stays Running
                JustRunning();
            });

            // First run - complete first child, second child keeps Running
            sequence.Tick();

            Assert.AreEqual(new[] { "enabled", "enter" }, events.ToArray());

            events.Clear();

            // Invalidate
            condition = false;

            // Next tick should detect invalidation (sequence is still Running)
            sequence.Tick();

            // Should call OnEnter but NOT OnEnabled (node is still Active)
            Assert.AreEqual(new[] { "enter" }, events.ToArray(),
                "Re-entry should call OnEnter but NOT OnEnabled");
        }

        [UnityTest]
        public IEnumerator ReactiveSequence_GracefulReset_AwaitsAsyncExit()
        {
            var condition = true;
            var events = new List<string>();
            var exitStarted = false;
            var exitCompleted = false;

            var sequence = Reactive * Sequence(() =>
            {
                Condition(() => condition);

                Leaf("Child2", () =>
                {
                    OnExit(async ct =>
                    {
                        exitStarted = true;
                        events.Add("exit-start");
                        try
                        {
                            // await Cysharp.Threading.Tasks.UniTask.Delay(100, cancellationToken: ct);
                            await UniTask.WaitForSeconds(0.1f, cancellationToken: ct);
                        }
                        finally
                        {
                            exitCompleted = true;
                            events.Add("exit-complete");
                        }
                    });

                    OnBaseTick(() => Status.Running);
                });
            });

            // Run until Child2 is running
            sequence.Tick();
            sequence.Tick();

            // Invalidate
            condition = false;

            // Trigger invalidation
            sequence.Tick();

            Assert.IsTrue(exitStarted, "Exit should have started");
            Assert.IsFalse(exitCompleted, "Exit should not complete immediately");

            // wait 0.2s
            var t = Time.realtimeSinceStartup;
            while ((Time.realtimeSinceStartup - t) < 0.2f)
                yield return null;

            Assert.IsTrue(exitCompleted, "Exit should complete after delay");
            CollectionAssert.Contains(events, "exit-complete");
        }

        #endregion

        #region Nested Reactive Tests

        [Test]
        public void ReactiveSequence_NestedInvalidation_PropagatesCorrectly()
        {
            var outerCondition = true;
            var innerCondition = true;
            var executionOrder = new List<string>();

            var sequence = Reactive * Sequence("Outer", () =>
            {
                Leaf("Outer1", () =>
                {
                    OnInvalidCheck(() => !outerCondition);
                    OnEnter(() => executionOrder.Add("outer1-enter"));
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("outer1-tick");
                        return Status.Success;
                    });
                });

                // Inner sequence must ALSO be Reactive to check its own children
                _ = Reactive * Sequence("Inner", () =>
                {
                    Leaf("Inner1", () =>
                    {
                        OnInvalidCheck(() => !innerCondition);
                        OnEnter(() => executionOrder.Add("inner1-enter"));
                        OnBaseTick(() =>
                        {
                            executionOrder.Add("inner1-tick");
                            return Status.Success;
                        });
                    });

                    Leaf("Inner2", () =>
                    {
                        OnEnter(() => executionOrder.Add("inner2-enter"));
                        OnBaseTick(() =>
                        {
                            executionOrder.Add("inner2-tick");
                            return Status.Running;
                        });
                    });
                });
            });

            // Complete Outer1, Inner1, start Inner2
            sequence.Tick();
            sequence.Tick();
            sequence.Tick();

            executionOrder.Clear();

            // Invalidate Inner1
            innerCondition = false;

            sequence.Tick();

            // Inner sequence (now Reactive) should detect invalidation and reset Inner2
            CollectionAssert.Contains(executionOrder, "inner1-enter",
                "Inner1 should re-enter after invalidation");
        }

        #endregion

        #region Reactive Selector Tests

        [Test]
        public void ReactiveSelector_InvalidatedChild_RetriesPreviousOptions()
        {
            var option1Available = false;
            var option2Available = true;
            var executionOrder = new List<string>();

            var selector = Reactive * Selector(() =>
            {
                D.Condition(() => option1Available);
                Leaf("Option1", () =>
                {
                    OnEnter(() => executionOrder.Add("option1-enter"));
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("option1-tick");
                        return Status.Success;
                    });
                });

                D.Condition(() => option2Available);
                Leaf("Option2", () =>
                {
                    OnEnter(() => executionOrder.Add("option2-enter"));
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("option2-tick");
                        return Status.Running;
                    });
                });
            });

            // First run - Option1 unavailable, tries Option1 decorator (fails), moves to Option2
            // Tick 1: Selector starts Option1 decorator (Enabling/Entering)
            selector.Tick();
            // Tick 2: Option1 decorator checks condition (false), tries to exit child, returns Failure
            //         Selector advances to Option2 decorator (Enabling/Entering)
            selector.Tick();
            // Tick 3: Option2 decorator checks condition (true), starts Option2 leaf (Enabling/Entering)
            selector.Tick();
            // Tick 4: Option2 leaf runs, calls OnEnter and OnBaseTick
            selector.Tick();

            Assert.Contains("option2-enter", executionOrder);
            Assert.Contains("option2-tick", executionOrder);

            executionOrder.Clear();

            // Make Option1 available
            option1Available = true;

            // Next tick should detect Option1 decorator is now valid
            selector.Tick();

            // Should reset Option2 and restart from Option1
            // Tick 5: Invalidation detected, Option2 reset, Option1 decorator re-entered
            // Tick 6: Option1 decorator checks condition (true), starts Option1 leaf
            selector.Tick();
            // Tick 7: Option1 leaf runs
            selector.Tick();

            // Should switch to Option1
            CollectionAssert.Contains(executionOrder, "option1-enter",
                "Selector should retry earlier options when they become available");
        }

        #endregion

        #region AllowReEnter Integration Tests

        [Test]
        public void ReactiveSequence_AllowReEnter_WorksWithInvalidation()
        {
            var condition = true;
            var enterCount = 0;
            var enabledCount = 0;

            var sequence = Reactive * Sequence(() =>
            {
                Leaf("Test", () =>
                {
                    OnInvalidCheck(() => !condition);
                    OnEnabled(() => enabledCount++);
                    OnEnter(() => enterCount++);
                    OnBaseTick(() => Status.Success);
                });

                // Keep sequence Running so reactive invalidation can trigger
                JustRunning();
            });

            // First run - complete first child, second child keeps Running
            sequence.Tick();
            sequence.Tick();

            Assert.AreEqual(1, enabledCount);
            Assert.AreEqual(1, enterCount);

            // Invalidate and tick
            condition = false;
            sequence.Tick();

            // Should re-enter via allowReEnter (OnEnter called, OnEnabled NOT called)
            Assert.AreEqual(1, enabledCount, "OnEnabled should not be called on invalidation re-entry");
            Assert.AreEqual(2, enterCount, "OnEnter should be called on invalidation re-entry");
        }

        #endregion

        #region Complex Scenarios

        [Test]
        public void ReactiveSequence_MultipleInvalidations_HandlesCorrectly()
        {
            var condition1 = true;
            var condition2 = true;
            var executionOrder = new List<string>();

            var sequence = Reactive * Sequence(() =>
            {
                Leaf("Node1", () =>
                {
                    OnInvalidCheck(() => !condition1);
                    OnEnter(() => executionOrder.Add("n1-enter"));
                    OnBaseTick(() => Status.Success);
                });

                Leaf("Node2", () =>
                {
                    OnInvalidCheck(() => !condition2);
                    OnEnter(() => executionOrder.Add("n2-enter"));
                    OnBaseTick(() => Status.Success);
                });

                Leaf("Node3", () =>
                {
                    OnEnter(() => executionOrder.Add("n3-enter"));
                    OnBaseTick(() => Status.Running);
                });
            });

            // Complete all nodes
            TickNode(sequence, 5);

            executionOrder.Clear();

            // Invalidate Node1
            condition1 = false;
            sequence.Tick();

            // Node1 should re-enter, Node2 and Node3 should reset
            CollectionAssert.Contains(executionOrder, "n1-enter");

            executionOrder.Clear();

            // Re-validate Node1, complete Node2, invalidate Node2
            condition1 = true;
            TickNode(sequence, 5);
            executionOrder.Clear();
            condition2 = false;
            sequence.Tick();

            // Node2 should re-enter, Node3 should reset
            CollectionAssert.Contains(executionOrder, "n2-enter");
        }

        [Test]
        public void ReactiveSequence_ConditionDecorator_InvalidatesCorrectly()
        {
            var hasAmmo = true;
            var executionOrder = new List<string>();

            var sequence = Reactive * Sequence(() =>
            {
                D.Condition(() => hasAmmo);
                Leaf("Shoot", () =>
                {
                    OnEnter(() => executionOrder.Add("shoot-enter"));
                    OnExit(() => executionOrder.Add("shoot-exit"));
                    OnBaseTick(() =>
                    {
                        executionOrder.Add("shoot-tick");
                        return Status.Running;
                    });
                });

                Leaf("Reload", () =>
                {
                    OnEnter(() => executionOrder.Add("reload-enter"));
                    OnBaseTick(() => Status.Success);
                });
            });

            // Start shooting
            sequence.Tick();
            Assert.AreEqual(new[] { "shoot-enter", "shoot-tick" }, executionOrder.ToArray());

            executionOrder.Clear();

            // Run out of ammo
            hasAmmo = false;

            // Should exit shoot and move to reload
            sequence.Tick();

            CollectionAssert.Contains(executionOrder, "shoot-exit",
                "Condition decorator invalidation should exit current node");
        }

        #endregion
    }
}
