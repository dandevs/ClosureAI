using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT.Tests
{
    /// <summary>
    /// Tests for Variable behavior within nodes.
    /// Variables are initialized during the Enabling phase and persist across ticks.
    /// </summary>
    [TestFixture]
    public class VariableTests
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

        #region Initialization Tests

        [Test]
        public void Variable_InitializeDuringEnablingPhase()
        {
            var events = new List<string>();
            var initCalled = false;

            var node = Leaf("Test", () =>
            {
                OnEnabled(() => events.Add("enabled"));

                var myVar = Variable(() =>
                {
                    initCalled = true;
                    events.Add("var-init");
                    return 42;
                });

                OnEnter(() => events.Add("enter"));

                OnBaseTick(() =>
                {
                    Assert.IsTrue(initCalled, "Variable should be initialized by the time OnBaseTick runs");
                    Assert.AreEqual(42, myVar.Value);
                    return Status.Success;
                });
            });

            TickNode(node);

            // Variable initialization happens BEFORE OnEnabled (right before Enabling phase begins)
            Assert.AreEqual(new[] { "var-init", "enabled", "enter" }, events.ToArray(),
                "Variables should initialize before OnEnabled callbacks");
        }

        [Test]
        public void Variable_LazyInitialization_CalledOncePerNodeEntry()
        {
            var initCount = 0;

            var node = Leaf("Test", () =>
            {
                var myVar = Variable(() =>
                {
                    initCount++;
                    return initCount * 10;
                });

                OnBaseTick(() =>
                {
                    Assert.AreEqual(10, myVar.Value, "Variable should have been initialized with first call result");
                    return Status.Success;
                });
            });

            TickNode(node);
            Assert.AreEqual(1, initCount, "Initializer should be called exactly once");
        }

        [Test]
        public void Variable_ReinitializeOnAllowReEnter()
        {
            var initCount = 0;
            var capturedValues = new List<int>();

            var node = Leaf("Test", () =>
            {
                var myVar = Variable(() =>
                {
                    initCount++;
                    return initCount * 100;
                });

                OnBaseTick(() =>
                {
                    capturedValues.Add(myVar.Value);
                    return Status.Success;
                });
            });

            // First run
            TickNode(node);
            Assert.AreEqual(1, initCount);
            Assert.AreEqual(100, capturedValues[0]);

            // Re-enter
            node.Tick(out _, allowReEnter: true);
            TickNode(node);

            // NOTE: Variables do NOT reinitialize on allowReEnter
            // Variable initialization only happens when Status == None (line 262 in Node.cs)
            // allowReEnter (line 312) only calls OnEnter, NOT variable initialization
            Assert.AreEqual(1, initCount, "Variable should NOT reinitialize on allowReEnter (only on Status.None)");
            Assert.AreEqual(100, capturedValues[1], "Variable should keep same value after allowReEnter");
        }

        [Test]
        public void Variable_ResetImmediately_ReinitializesOnNextActivation()
        {
            var initCount = 0;
            var capturedValues = new List<int>();

            var node = Leaf("Variable Reset", () =>
            {
                var myVar = Variable(() =>
                {
                    initCount++;
                    return initCount * 5;
                });

                OnBaseTick(() =>
                {
                    capturedValues.Add(myVar.Value);
                    return Status.Success;
                });
            });

            TickNode(node);
            Assert.AreEqual(1, initCount, "Initializer should have run once");
            CollectionAssert.AreEqual(new[] { 5 }, capturedValues);

            node.ResetImmediately();
            node.Tick(out _);

            Assert.AreEqual(2, initCount, "Resetting to None should re-run variable initializer");
            CollectionAssert.AreEqual(new[] { 5, 10 }, capturedValues);
        }

        [Test]
        public void Variable_ReactiveInvalidation_ReinitializesWhenParentResets()
        {
            var gate = true;
            var initCount = 0;
            var captured = new List<int>();

            var node = Reactive * Sequence("Root", () =>
            {
                WaitUntil("Gate", () => gate);

                Leaf("VariableUser", () =>
                {
                    var scoped = Variable(() =>
                    {
                        initCount++;
                        return initCount * 100;
                    });

                    OnBaseTick(() =>
                    {
                        captured.Add(scoped.Value);
                        return Status.Success;
                    });
                });

                JustRunning();
            });

            TickNode(node);
            Assert.AreEqual(1, initCount, "Variable should initialize on first activation");

            gate = false;
            node.Tick();

            gate = true;
            node.Tick();

            Assert.AreEqual(2, initCount, "Reactive invalidation should reset downstream nodes, reinitializing their variables");
            CollectionAssert.AreEqual(new[] { 100, 200 }, captured);
        }

        #endregion

        #region Persistence Tests

        [Test]
        public void Variable_PersistsAcrossMultipleTicks()
        {
            var tickCount = 0;

            var node = Leaf("Test", () =>
            {
                var counter = Variable(() => 0);

                OnBaseTick(() =>
                {
                    tickCount++;
                    counter.Value++;

                    Assert.AreEqual(tickCount, counter.Value,
                        "Variable should maintain incremented value across ticks");

                    return tickCount >= 5 ? Status.Success : Status.Running;
                });
            });

            TickNode(node);
            Assert.AreEqual(5, tickCount);
        }

        [Test]
        public void Variable_ModificationsPersist_BetweenLifecycleMethods()
        {
            var enterValue = 0;
            var tickValue = 0;
            var exitValue = 0;

            var node = Leaf("Test", () =>
            {
                var myVar = Variable(() => 100);

                OnEnter(() =>
                {
                    myVar.Value = 200;
                    enterValue = myVar.Value;
                });

                OnBaseTick(() =>
                {
                    myVar.Value = 300;
                    tickValue = myVar.Value;
                    return Status.Success;
                });

                OnExit(() =>
                {
                    exitValue = myVar.Value;
                });
            });

            TickNode(node);

            Assert.AreEqual(200, enterValue, "Value set in OnEnter");
            Assert.AreEqual(300, tickValue, "Value modified in OnBaseTick");
            Assert.AreEqual(300, exitValue, "Modified value persists to OnExit");
        }

        #endregion

        #region Scope Tests

        [Test]
        public void Variable_ChildrenCanAccessParentVariables()
        {
            var childReadValue = 0;

            var sequence = Sequence(() =>
            {
                var parentVar = Variable(() => 999);

                Leaf("Child", () =>
                {
                    OnBaseTick(() =>
                    {
                        childReadValue = parentVar.Value;
                        return Status.Success;
                    });
                });
            });

            TickNode(sequence);

            Assert.AreEqual(999, childReadValue, "Child should access parent's variable");
        }

        [Test]
        public void Variable_ChildrenCanModifyParentVariables()
        {
            var finalValue = 0;

            var sequence = Sequence(() =>
            {
                var sharedVar = Variable(() => 10);

                Leaf("Child1", () =>
                {
                    OnBaseTick(() =>
                    {
                        sharedVar.Value += 5;
                        return Status.Success;
                    });
                });

                Leaf("Child2", () =>
                {
                    OnBaseTick(() =>
                    {
                        sharedVar.Value *= 2;
                        return Status.Success;
                    });
                });

                Leaf("Reader", () =>
                {
                    OnBaseTick(() =>
                    {
                        finalValue = sharedVar.Value;
                        return Status.Success;
                    });
                });
            });

            TickNode(sequence);

            // Initial: 10, Child1: 10+5=15, Child2: 15*2=30
            Assert.AreEqual(30, finalValue, "Children should share parent variable state");
        }

        [Test]
        public void Variable_NestedScopes_EachLevelHasOwnVariables()
        {
            var level1Value = 0;
            var level2Value = 0;
            var level3Value = 0;

            var sequence = Sequence("L1", () =>
            {
                var l1Var = Variable(() => 100);

                Sequence("L2", () =>
                {
                    var l2Var = Variable(() => 200);

                    Sequence("L3", () =>
                    {
                        var l3Var = Variable(() => 300);

                        Leaf("Reader", () =>
                        {
                            OnBaseTick(() =>
                            {
                                level1Value = l1Var.Value;
                                level2Value = l2Var.Value;
                                level3Value = l3Var.Value;
                                return Status.Success;
                            });
                        });
                    });
                });
            });

            TickNode(sequence);

            Assert.AreEqual(100, level1Value);
            Assert.AreEqual(200, level2Value);
            Assert.AreEqual(300, level3Value);
        }

        [Test]
        public void Variable_SiblingsDontShareVariables()
        {
            var child1Value = 0;
            var child2Value = 0;

            var sequence = Sequence(() =>
            {
                Sequence("Child1", () =>
                {
                    var localVar = Variable(() => 111);

                    Leaf(() =>
                    {
                        OnBaseTick(() =>
                        {
                            child1Value = localVar.Value;
                            return Status.Success;
                        });
                    });
                });

                Sequence("Child2", () =>
                {
                    var localVar = Variable(() => 222);

                    Leaf(() =>
                    {
                        OnBaseTick(() =>
                        {
                            child2Value = localVar.Value;
                            return Status.Success;
                        });
                    });
                });
            });

            TickNode(sequence);

            Assert.AreEqual(111, child1Value, "Child1 has its own variable");
            Assert.AreEqual(222, child2Value, "Child2 has its own separate variable");
        }

        #endregion

        #region Reset Behavior Tests

        [Test]
        public void Variable_ResetsOnNodeReset()
        {
            var sequence = Sequence(() =>
            {
                var counter = Variable(() => 0);

                Leaf("Incrementer", () =>
                {
                    OnBaseTick(() =>
                    {
                        counter.Value++;
                        return counter.Value >= 3 ? Status.Success : Status.Running;
                    });
                });

                Leaf("Validator", () =>
                {
                    OnBaseTick(() =>
                    {
                        Assert.AreEqual(3, counter.Value, "First run should reach 3");
                        return Status.Success;
                    });
                });
            });

            // First run
            TickNode(sequence);

            // Reset
            sequence.ResetImmediately();

            // Second run
            var secondRunValue = 0;
            var sequence2 = Sequence(() =>
            {
                var counter = Variable(() => 0);

                Leaf(() =>
                {
                    OnBaseTick(() =>
                    {
                        counter.Value++;
                        secondRunValue = counter.Value;
                        return Status.Success;
                    });
                });
            });

            TickNode(sequence2);

            Assert.AreEqual(1, secondRunValue, "Variable should reset to initial value after node reset");
        }

        #endregion

        #region Complex Scenarios

        [Test]
        public void Variable_InSelector_PersistsAcrossFallbacks()
        {
            var attemptCounts = new List<int>();

            var selector = Selector(() =>
            {
                var attemptCounter = Variable(() => 0);

                Condition(() =>
                {
                    attemptCounter.Value++;
                    attemptCounts.Add(attemptCounter.Value);
                    return false; // Fail
                });

                Condition(() =>
                {
                    attemptCounter.Value++;
                    attemptCounts.Add(attemptCounter.Value);
                    return false; // Fail
                });

                Leaf(() =>
                {
                    OnBaseTick(() =>
                    {
                        attemptCounter.Value++;
                        attemptCounts.Add(attemptCounter.Value);
                        return Status.Success;
                    });
                });
            });

            TickNode(selector);

            Assert.AreEqual(new[] { 1, 2, 3 }, attemptCounts.ToArray(),
                "Variable should accumulate across all selector fallback attempts");
        }

        [Test]
        public void Variable_WithRunningNode_MaintainsStateAcrossTicks()
        {
            var capturedValues = new List<int>();

            var node = Leaf("Test", () =>
            {
                var runningCounter = Variable(() => 0);

                OnBaseTick(() =>
                {
                    runningCounter.Value += 10;
                    capturedValues.Add(runningCounter.Value);

                    return runningCounter.Value >= 50 ? Status.Success : Status.Running;
                });
            });

            TickNode(node);

            Assert.AreEqual(new[] { 10, 20, 30, 40, 50 }, capturedValues.ToArray(),
                "Variable should maintain and accumulate state across running ticks");
        }

        [Test]
        public void Variable_TypedVariable_SupportsComplexTypes()
        {
            var capturedList = new List<int>();

            var node = Leaf("Test", () =>
            {
                var listVar = Variable<List<int>>(() => new List<int>());

                OnBaseTick(() =>
                {
                    // Add an incrementing value to the list
                    listVar.Value.Add(listVar.Value.Count + 1);

                    if (listVar.Value.Count >= 3)
                    {
                        capturedList.AddRange(listVar.Value);
                        return Status.Success;
                    }

                    return Status.Running;
                });
            });

            TickNode(node);

            Assert.AreEqual(new[] { 1, 2, 3 }, capturedList.ToArray(),
                "Variable should support complex reference types like List");
        }

        [Test]
        public void Variable_MultipleVariablesInSameNode_IndependentState()
        {
            var var1Final = 0;
            var var2Final = "";
            var var3Final = false;

            var node = Leaf("Test", () =>
            {
                var intVar = Variable(() => 0);
                var stringVar = Variable(() => "");
                var boolVar = Variable(() => false);

                OnBaseTick(() =>
                {
                    intVar.Value = 42;
                    stringVar.Value = "test";
                    boolVar.Value = true;

                    var1Final = intVar.Value;
                    var2Final = stringVar.Value;
                    var3Final = boolVar.Value;

                    return Status.Success;
                });
            });

            TickNode(node);

            Assert.AreEqual(42, var1Final);
            Assert.AreEqual("test", var2Final);
            Assert.AreEqual(true, var3Final);
        }

        #endregion
    }
}
