# ClosureAI Behavior Tree - Context for LLMs

## Table of Contents

1. [Overview](#overview)
2. [How to Construct Trees Properly](#how-to-construct-trees-properly)
3. [Node Lifecycle States](#node-lifecycle-states)
4. [The Tick() Lifecycle Flow](#the-tick-lifecycle-flow)
5. [OnDisabled - The Exception](#ondisabled---the-exception)
6. [Example Usage Patterns](#example-usage-patterns)
7. [Key Concepts](#key-concepts)
   - [Variables](#variables)
   - [Lifecycle Methods](#lifecycle-methods)
   - [Reactive Trees and Invalidation](#reactive-trees-and-invalidation)
   - [Async Support](#async-support)
   - [Active Flag](#active-flag)
   - [Done Property](#done-property)
   - [Resetting Flags](#resetting-flags)
8. [Common Patterns](#common-patterns)
   - [SequenceAlways vs Sequence](#sequencealways-vs-sequence)
   - [Yield - Dynamic Node Insertion and Recursion](#yield---dynamic-node-insertion-and-recursion)
   - [D.* Decorators](#d-decorators)
9. [Reset Behaviors](#reset-behaviors)
10. [Integration Points](#integration-points)
11. [Status Transitions Summary](#status-transitions-summary)
12. [Important Notes for LLMs](#important-notes-for-llms)
13. [Debugging Tips](#debugging-tips)

## Overview

ClosureAI is a behavior tree library for Unity that uses a declarative C# API to define AI behaviors. This document explains the node lifecycle, status flow, and key concepts to help LLMs understand how to work with this system.

### Key Distinguishing Features

1. **Detailed Lifecycle States**: Nodes have both Status (None/Running/Success/Failure) and SubStatus (None/Enabling/Entering/Running/Succeeding/Failing/Exiting/Disabling/Done) for fine-grained control
2. **Async-First Design**: All lifecycle callbacks support UniTask async/await with proper cancellation token support
3. **Reactive Invalidation**: Nodes can invalidate when conditions change, causing automatic re-entry with graceful cleanup of subsequent nodes
4. **Separated OnDisabled**: Unlike traditional BTs, OnDisabled is NOT part of the normal tick flow - it only fires during resets
5. **Re-entry via allowReEnter**: Nodes can be re-entered (Done → Entering) without going through OnEnabled again

## How to Construct Trees Properly

This section is **critical** for understanding how to write ClosureAI behavior trees. Trees are constructed using a declarative, fluent API with closures. Getting the structure right is essential.

### The Fundamental Pattern

**Every ClosureAI tree follows this structure:**

```csharp
using static ClosureAI.AI;  // REQUIRED: Imports all node creation functions
using UnityEngine;

public class MyAI : MonoBehaviour
{
    public Node AI;  // Store the tree root
    
    private void Awake() => AI = CreateAI();  // Build tree in Awake
    private void Update() => AI.Tick();       // Tick tree every frame
    private void OnDestroy() => AI.ResetImmediately();  // Cleanup on destroy
    
    private Node CreateAI()
    {
        // Return a node from here - this is your tree root
        return Sequence("Root", () =>
        {
            // Children go inside the lambda
        });
    }
}
```

**Key points:**
1. `using static ClosureAI.AI;` - This is **required** to access node creation functions
2. Store the tree in a `Node` field (public for inspector visualization)
3. Build the tree once (usually in `Awake()`)
4. Tick the tree every frame (usually in `Update()`)
5. Reset the tree on cleanup (usually in `OnDestroy()`)

### The Lambda Closure Pattern

**All composite nodes (Sequence, Selector, etc.) take a lambda where you define children:**

```csharp
Sequence("My Sequence", () =>
{
    // Children are declared INSIDE this lambda
    Wait(1f);
    Wait(2f);
    Wait(3f);
});
```

**❌ WRONG - This doesn't work:**
```csharp
// This tries to pass children as parameters - NOT HOW IT WORKS
Sequence("My Sequence", Wait(1f), Wait(2f), Wait(3f));
```

**The lambda pattern is how children are added.** When the Sequence node is created, it executes your lambda, and each child node created inside that lambda gets added to the parent.

### Leaf Nodes vs Composite Nodes

**Leaf Nodes** - Do work, have no children:
- `Leaf()` - Custom leaf node with `OnBaseTick`
- `JustRunning()` - Always returns `Status.Running`
- `Wait()` - Waits for a duration
- `WaitUntil()` - Waits until a condition is true
- `Condition()` - Succeeds if condition is true, fails otherwise

**Composite Nodes** - Have children, control flow:
- `Sequence()` - Runs children in order, fails on first failure
- `SequenceAlways()` - Runs all children regardless of failures
- `Selector()` - Tries children in order, succeeds on first success
- `Parallel()` - Runs all children simultaneously

**Example with both:**
```csharp
Sequence("Parent Sequence", () =>
{
    Leaf("Custom Work", () =>
    {
        OnBaseTick(() =>
        {
            DoSomeWork();
            return Status.Success;
        });
    });
    
    Wait(1f);  // Leaf node - no lambda needed
    
    Selector("Try Options", () =>  // Composite - needs lambda for children
    {
        Condition(() => OptionA());
        Condition(() => OptionB());
    });
});
```

### Decorators - The D.* Pattern

**Decorators modify the next node created.** They use a "push then pop" pattern:

```csharp
Sequence(() =>
{
    // Decorator goes BEFORE the node it decorates
    D.Condition(() => playerInRange);
    Leaf("Attack", () =>
    {
        OnBaseTick(() => AttackPlayer());
    });
    
    // Multiple decorators stack
    D.Condition(() => hasAmmo);
    D.Invert();  // Inverts the result
    Wait(1f);    // Only waits if we DON'T have ammo
});
```

**How it works:**
1. `D.Condition()` pushes a decorator onto a stack
2. `Leaf()` (or any node) pops decorators and wraps itself
3. The decorator now controls when/how the leaf runs

**Common decorators:**
- `D.Condition(Func<bool>)` - Only runs child when condition is true
- `D.Invert()` - Flips Success ↔ Failure
- `D.Until(Status)` - Repeats child until it returns the target status
- `D.Repeat()` - Repeats child infinitely
- `D.ForEach(list, out item)` - Runs child for each item in list

### Variables - State Inside Nodes

**Variables store data that persists across ticks:**

```csharp
Sequence(() =>
{
    var targetEnemy = Variable<GameObject>(() => null);
    var health = Variable(() => 100f);
    
    OnTick(() =>
    {
        // Access via .Value
        targetEnemy.Value = FindNearestEnemy();
        health.Value -= Time.deltaTime * 10f;
    });
    
    Leaf("Attack", () =>
    {
        OnBaseTick(() =>
        {
            if (targetEnemy.Value != null)
                Attack(targetEnemy.Value);
            return Status.Success;
        });
    });
});
```

**Key rules:**
- Variables must be declared **inside** node setup lambdas
- Access values via `.Value` property
- Variables are initialized during the Enabling phase
- Use `Variable(() => initialValue)` or `Variable<T>(() => initialValue)`

**Critical: Variable Initialization Pattern**

The `Variable(() => initFunction)` pattern is special: **the passed function is called exactly once, right before the node's OnEnabled phase begins**. This means:

```csharp
Sequence(() =>
{
    // This function is called once during Enabling, before OnEnabled callbacks fire
    var currentPosition = Variable(() => transform.position);
    
    OnEnabled(() =>
    {
        // At this point, currentPosition.Value is already set to transform.position
        Debug.Log($"Started at: {currentPosition.Value}");
    });
});
```

**Why this matters:**
1. **Initial value is captured**: The function runs once to initialize the variable
2. **Happens during Enabling**: This occurs before any `OnEnabled` callbacks
3. **Lazy evaluation**: The function isn't called until the node is first ticked
4. **Fresh init on re-entry**: If the node is re-entered (via reactive invalidation), the function runs again

**Example - Capturing position at node start:**
```csharp
var startingPosition = Variable(() => transform.position);  // Captured when Enabling phase starts

OnEnter(() =>
{
    // startingPosition.Value is already set here
    distanceTraveled = 0f;
});

OnTick(() =>
{
    distanceTraveled = Vector3.Distance(transform.position, startingPosition.Value);
});
```

This is fundamentally different from setting `.Value` in a callback - the lambda you pass to `Variable()` is the initial **definition** of the variable's value, evaluated fresh each time the node enters.

### Reactive Trees - The Reactive Multiplier

**Mark nodes as reactive to enable automatic re-evaluation:**

```csharp
// Mark the root as reactive
AI = Reactive * SequenceAlways("Root", () =>
{
    Condition("Player in range", () => playerInRange);
    AttackPlayer();
    
    // If playerInRange changes while AttackPlayer is running,
    // the tree will reset AttackPlayer and re-check the condition
});
```

**Nested reactive nodes:**
```csharp
Tree = Reactive * SequenceAlways("Root", () =>
{
    WaitUntil(() => gameStarted);
    
    // Inner reactive subtree
    _ = Reactive * Selector("Behavior", () =>
    {
        D.Condition(() => isAggressive);
        AttackBehavior();
        
        PatrolBehavior();
    });
});
```

**Why `_ = Reactive * Node`?**
- The discard pattern `_ =` is required when not assigning to a variable
- Without it, C# won't allow the expression as a statement
- This marks the node as reactive without storing a reference to it

### Complete Examples

**Example 1: Flee and Return to Center (From FleeSample.cs)**

This example demonstrates reactive trees with hysteresis, decorators, and complex state management:

```csharp
using static ClosureAI.AI;
using UnityEngine;

namespace ClosureAI.Samples.RunAway
{
    public class FleeSample : MonoBehaviour
    {
        public GameObject Buddy;
        public Node BuddyAI;

        private void Awake() => BuddyAI = CreateBuddyAI();
        private void Update() => BuddyAI.Tick();
        private void OnDestroy() => BuddyAI.ResetImmediately();

        /// <summary>
        /// STRUCTURE BREAKDOWN:
        /// Reactive * SequenceAlways("Buddy")
        ///   ├─ Variable: distanceToPlayer
        ///   ├─ OnTick: Update distance each frame
        ///   ├─ D.ConditionLatch + D.Until
        ///   │   └─ JustRunning "Flee!" (run away until safe distance)
        ///   └─ Sequence (return to center)
        ///       ├─ WaitUntil: Move to center position
        ///       ├─ Wait: Brief pause
        ///       └─ JustRunning "Move in a circle" (circular animation)
        /// </summary>
        private Node CreateBuddyAI() => Reactive * SequenceAlways("Buddy", () =>
        {
            // VARIABLE: Stores data that persists across tree ticks
            var distanceToPlayer = Variable(() => 0f);

            OnTick(() =>
            {
                distanceToPlayer.Value = Vector3.Distance(Buddy.transform.position, PlaneCastMousePosition());
            });

            // Why a ConditionLatch + Until?
            // The answer is hysteresis - we want to avoid rapid toggling between states
            // The ConditionLatch ensures that once we start fleeing, we won't stop until safe
            D.ConditionLatch("Too Close!", () => distanceToPlayer.Value < 2f);
            D.Until(() => distanceToPlayer.Value > 5f);
            JustRunning("Flee!", () =>
            {
                OnEnter(() => Debug.Log("We're running away!"));
                OnExit(() => Debug.Log("Safe now."));

                OnTick(() =>
                {
                    if (distanceToPlayer.Value < 3.5f)
                    {
                        var directionAway = (Buddy.transform.position - PlaneCastMousePosition()).normalized;
                        MoveBuddy(Buddy.transform.position + directionAway * 3f);
                    }
                });
            });

            // Once flee completes, return to center
            Sequence(() =>
            {
                WaitUntil(() => MoveBuddy(Vector3.zero));
                Wait(0.25f);

                JustRunning("Move in a circle", () =>
                {
                    OnEnter(() => Debug.Log("Moving to center."));
                    OnTick(() =>
                    {
                        var circlePosition = new Vector3
                        {
                            x = Mathf.Cos(Time.time) * 1f,
                            z = Mathf.Sin(Time.time) * 1f,
                        };
                        MoveBuddy(circlePosition);
                    });
                });
            });
        });

        private bool MoveBuddy(Vector3 position)
        {
            Buddy.transform.position = Vector3.Lerp(Buddy.transform.position, position, Time.deltaTime * 2f);
            return Vector3.Distance(Buddy.transform.position, position) < 0.1f;
        }

        private Vector3 PlaneCastMousePosition()
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : Vector3.zero;
        }
    }
}
```

**Key Learning Points from FleeSample**:
1. **Variables for State**: `distanceToPlayer` persists across ticks and is updated every frame
2. **OnTick Lifecycle**: Parent nodes can use `OnTick()` to update shared state for children
3. **Decorator Composition**: `D.ConditionLatch` + `D.Until` create hysteresis behavior
4. **Reactive Re-entry**: The `Reactive` multiplier allows the tree to respond when conditions change
5. **Nested Sequences**: Complex behaviors are built from simpler composites
6. **Callbacks for Logic**: `OnEnter`, `OnExit`, and `OnTick` callbacks handle game logic

---

**Example 2: Simple Patrol AI**
```csharp
using static ClosureAI.AI;
using UnityEngine;

public class PatrolAI : MonoBehaviour
{
    public Node AI;
    public Transform[] waypoints;
    
    private void Awake() => AI = Reactive * SequenceAlways("Patrol", () =>
    {
        var waypointIndex = Variable(() => 0);
        
        D.Until(Status.Success);
        JustRunning("Move to Waypoint", () =>
        {
            OnTick(() =>
            {
                var target = waypoints[waypointIndex.Value].position;
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 2f);
            });
        });
        
        Leaf("Next Waypoint", () =>
        {
            OnBaseTick(() =>
            {
                waypointIndex.Value = (waypointIndex.Value + 1) % waypoints.Length;
                return Status.Success;
            });
        });
    });
    
    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();
}
```

**Example 2: Recursive Planning**
```csharp
public Node AcquireItem(Func<string> getItemID) => Selector("Acquire Item", () =>
{
    var itemID = Variable(getItemID);
    
    // Already have it
    Condition(() => inventory.Contains(itemID.Value));
    
    // Find and collect
    D.Condition(() => ItemExistsInWorld(itemID.Value));
    YieldSimpleCached(() => CollectItem(getItemID));
    
    // Craft it (may recursively call AcquireItem for ingredients)
    D.Condition(() => CanCraft(itemID.Value));
    YieldSimpleCached(() => CraftItem(getItemID));
});
```

### Common Mistakes to Avoid

**❌ Mistake 1: Creating nodes outside the tree structure**
```csharp
// WRONG - These nodes aren't part of any tree!
var wait1 = Wait(1f);
var wait2 = Wait(2f);
var seq = Sequence(() => { wait1; wait2; });
```

**✅ Correct:**
```csharp
var seq = Sequence(() =>
{
    Wait(1f);  // Created inside the lambda
    Wait(2f);
});
```

**❌ Mistake 2: Not using `using static ClosureAI.AI`**
```csharp
// WRONG - Can't find Sequence, Wait, etc.
public class MyAI : MonoBehaviour
{
    Node tree = Sequence(() => { });  // Compile error!
}
```

**✅ Correct:**
```csharp
using static ClosureAI.AI;  // Required!

public class MyAI : MonoBehaviour
{
    Node tree = Sequence(() => { });  // Works!
}
```

**❌ Mistake 3: Trying to pass children as parameters**
```csharp
// WRONG - Not how children are added
Sequence("Test", Wait(1f), Wait(2f));
```

**✅ Correct:**
```csharp
Sequence("Test", () =>
{
    Wait(1f);
    Wait(2f);
});
```

**❌ Mistake 4: Decorators after the node**
```csharp
// WRONG - Decorator must come BEFORE
Sequence(() =>
{
    Wait(1f);
    D.Condition(() => ready);  // This won't work as expected!
});
```

**✅ Correct:**
```csharp
Sequence(() =>
{
    D.Condition(() => ready);  // Before the node
    Wait(1f);
});
```

**❌ Mistake 5: Variables outside node setup**
```csharp
// WRONG - Variables must be inside setup lambdas
var myVar = Variable(() => 0);
Sequence(() =>
{
    Leaf(() => { /* use myVar */ });
});
```

**✅ Correct:**
```csharp
Sequence(() =>
{
    var myVar = Variable(() => 0);  // Inside the setup lambda
    Leaf(() => { /* use myVar */ });
});
```

### Quick Reference Template

```csharp
using static ClosureAI.AI;
using UnityEngine;

public class MyAI : MonoBehaviour
{
    public Node AI;
    
    private void Awake() => AI = Reactive * SequenceAlways("Root", () =>
    {
        // Variables
        var myVariable = Variable(() => initialValue);
        
        // Lifecycle callbacks
        OnTick(() => { /* every tick */ });
        
        // Decorated node
        D.Condition(() => someCondition);
        Leaf("DoWork", () =>
        {
            OnBaseTick(() =>
            {
                // Your logic here
                return Status.Success;
            });
        });
        
        // Nested composite
        Sequence("SubTree", () =>
        {
            Wait(1f);
            Condition(() => checkSomething);
        });
        
        // Yield for dynamic/recursive behavior
        YieldSimpleCached(() => SomeRecursiveNode());
        
        // Keep running
        JustRunning();
    });
    
    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();
}
```

## Node Lifecycle States

### Status Enum
Represents the overall outcome of a node:
- **None**: Node has not been initialized
- **Running**: Node is actively executing
- **Success**: Node completed successfully
- **Failure**: Node failed

### SubStatus Enum
Represents the detailed execution phase within a node:
- **None**: Not started
- **Enabling**: Executing OnEnabled callbacks
- **Entering**: Executing OnEnter callbacks
- **Running**: Executing main logic (BaseTick)
- **Succeeding**: Executing OnSuccess callbacks
- **Failing**: Executing OnFailure callbacks
- **Exiting**: Executing OnExit callbacks
- **Disabling**: Executing OnDisabled callbacks
- **Done**: Fully completed, ready to be reset

## The Tick() Lifecycle Flow

### Complete Flow Diagram
```
None -> Enabling -> Entering -> Running -> Success/Failure
  |         |          |           |              |
  |         v          v           v              v
  |    OnEnabled    OnEnter    BaseTick()  Succeeding/Failing
  |                                              |
  |                                              v
  |                                      OnSuccess/OnFailure
  |                                              |
  |                                              v
  |                                           Exiting
  |                                              |
  |                                              v
  |                                            OnExit
  |                                              |
  |                                              v
  |                                             Done
```

### Detailed Phase Breakdown

#### 1. **None → Enabling** (First Tick)
When `Tick()` is called on a node with `Status.None`:
- SubStatus becomes `SubStatus.Enabling`
- Status becomes `Status.Running`
- Variable initializers are invoked
- `OnEnabled` callbacks execute (async-capable)
- Node's `Active` flag is set to `true`

**Node.cs Reference**: Lines 255-276

#### 2. **Enabling → Entering**
After `OnEnabled` completes:
- SubStatus becomes `SubStatus.Entering`
- `OnEnter` callbacks execute (async-capable)
- If reset occurs during this phase, node exits gracefully

**Node.cs Reference**: Lines 280-308

#### 3. **Entering → Running**
After `OnEnter` completes:
- SubStatus becomes `SubStatus.Running`
- Status remains `Status.Running`
- Node is now ready for main execution logic

**Node.cs Reference**: Lines 301-303

#### 4. **Running → Execution**
While in `SubStatus.Running`:
- `OnPreTicks` callbacks execute
- `BaseTick()` is called (returns Status)
- `OnTicks` callbacks execute
- If BaseTick returns Success/Failure, transition begins

**Node.cs Reference**: Lines 344-361, 210-221

#### 5. **Running → Succeeding/Failing**
When BaseTick returns Success or Failure:

**On Success**:
- SubStatus becomes `SubStatus.Succeeding`
- Status temporarily becomes `Status.Running`
- `OnSuccess` callbacks execute (async-capable)
- Then SubStatus becomes `SubStatus.Exiting`

**On Failure**:
- SubStatus becomes `SubStatus.Failing`
- Status temporarily becomes `Status.Running`
- `OnFailure` callbacks execute (async-capable)
- Then SubStatus becomes `SubStatus.Exiting`

**Node.cs Reference**: Lines 363-422

#### 6. **Succeeding/Failing → Exiting**
After success/failure callbacks:
- SubStatus becomes `SubStatus.Exiting`
- `OnExit` callbacks execute (async-capable)
- Can handle cancellation tokens for cleanup

**Node.cs Reference**: Lines 372-389, 401-418

#### 7. **Exiting → Done**
After `OnExit` completes:
- SubStatus becomes `SubStatus.Done`
- Status becomes final `Status.Success` or `Status.Failure`
- `BlockReEnter` flag is set to prevent immediate re-entry
- Node stops ticking until reset

**Node.cs Reference**: Lines 384-386, 413-415

### Re-entry and AllowReEnter
If `Tick(allowReEnter: true)` is called on a Done node:
- SubStatus goes from `Done` → `Entering`
- Status becomes `Running`
- `OnEnter` callbacks execute again
- **Important**: `OnEnabled` is NOT called again (node is still Active)

**Node.cs Reference**: Lines 311-342

## OnDisabled - The Exception

**Critical**: `OnDisabled` callbacks are **NOT** called during the normal Tick() lifecycle. They are only invoked during reset operations:

### When OnDisabled IS Called
1. **ResetImmediately()**: Forces immediate cleanup
2. **ResetGracefully()**: Waits for async operations to complete
3. **During Enabling/Entering/Running if reset occurs**: Node unwinds gracefully

### Flow with Reset
```
[Any State] -> Disabling -> None
                   |
                   v
              OnDisabled
```

**Node.cs Reference**: Lines 475-554 (ResetImmediately), 556-662 (ResetGracefully)

## Example Usage Patterns

### Basic Leaf Node
```csharp
Leaf("MyAction", () =>
{
    OnEnabled(() => Debug.Log("Node activated"));
    OnEnter(() => Debug.Log("Starting action"));
    OnBaseTick(() => {
        // Main logic
        return Status.Success;
    });
    OnSuccess(() => Debug.Log("Action succeeded"));
    OnExit(() => Debug.Log("Cleaning up"));
    OnDisabled(() => Debug.Log("Node deactivated - only on reset"));
});
```

### Decorator Example - Until
The `Until` decorator continues ticking its child until a condition is met. Decorators are pushed onto a stack before creating the child node:
```csharp
D.Until(Status.Success);
Sequence("MyChild", () =>
{
    // Child node content
});
```
- `D.Until()` pushes a decorator onto the stack
- `Sequence()` pops decorators from the stack and wraps them
- Decorator calls child.Tick() repeatedly
- Returns Running until child returns the target status
- Has OnExit to properly exit the child when done

### Decorator Example - Condition
The `Condition` decorator only runs its child when a condition is true:
```csharp
D.Condition(() => A == true);
Sequence("MyChild", () =>
{
    // Child node content
});
```
- `D.Condition()` pushes a decorator onto the stack
- `Sequence()` wraps itself with the condition
- Ticks child when condition is true
- Exits child when condition becomes false
- Uses OnInvalidCheck to detect condition changes

### Decorator Stacking
Multiple decorators can be stacked before a single child node:
```csharp
D.Condition(() => HasTarget);
D.Invert();
D.Repeat();
Leaf("DoSomething", () =>
{
    OnBaseTick(() => DoWork());
});
```
- Each `D.*()` call pushes a decorator onto the stack
- When `Leaf()` is called, it pops all decorators in reverse order
- Creates a chain: Condition → Invert → Repeat → Leaf
- Each decorator wraps the next one in the chain

### Complex Example - MoveTo
```csharp
public Node MoveTo(Func<(Vector3, float, float)> getParams, Action lifecycle = null) => Leaf("Move To", () =>
{
    var stoppingPosition = Variable(() => Vector3.zero);
    
    OnInvalidCheck(() => Vector3.Distance(getParams().position, stoppingPosition.Value) > getParams().invalidateDistance);
    OnSuccess(() => stoppingPosition.Value = transform.position);
    OnExit(() => Agent.ResetPath());
    
    OnBaseTick(() => {
        return MoveTo(targetPosition, stoppingDistance) 
            ? Status.Success 
            : Status.Running;
    });
    
    lifecycle?.Invoke();
});
```

## Key Concepts

### Variables
Variables are initialized during the Enabling phase:
```csharp
var myVar = Variable(() => InitialValue);
```
Access via `myVar.Value`

**Node.cs Reference**: Lines 260-261

### Lifecycle Methods

Most node creation functions accept an optional `lifecycle` parameter (usually the last parameter). This allows you to add lifecycle callbacks directly to that specific node without needing to nest inside the setup closure.

#### Pattern
```csharp
NodeCreator(params..., lifecycle: () =>
{
    OnEnter(() => { /* called when node enters */ });
    OnExit(() => { /* called when node exits */ });
    OnSuccess(() => { /* called on success */ });
    OnFailure(() => { /* called on failure */ });
    OnEnabled(() => { /* called on first enable */ });
    OnDisabled(() => { /* called on reset */ });
});
```

#### Example with MoveTo
```csharp
MoveTo(() => targetPosition, () =>
{
    OnEnter(() => Debug.Log("Started moving"));
    OnExit(async ct => 
    {
        Debug.Log("Finished moving");
        await Agent.StopAsync(ct);
    });
    OnSuccess(() => Debug.Log("Reached destination"));
});
```

And allows you to easily add behavior to specific nodes without modifying their internal setup.

#### Example
```csharp
Wait("Collect", 1, () =>
{
    OnSuccess(() =>
    {
        Inventory.Add(itemID.Value);
        Destroy(item.Value.gameObject);
    });
});
```

The lifecycle parameter is **optional** - if you don't need lifecycle callbacks, you can omit it entirely.

### Parameters via Func

Due to the lambda nature of ClosureAI nodes, parameters must be passed as `Func<T>` rather than direct values. This is critical for keeping parameters up-to-date.

**Why?** When a node is created, it captures variables in closures. If you pass a value by copy, it gets "frozen" at creation time and never updates. By passing a `Func<T>`, the node can call the function every time it needs the current value.

**Example - MoveTo**:
```csharp
// ❌ WRONG - position is captured once and never updates
MoveTo(Vector3.zero);

// ✅ CORRECT - position is evaluated fresh every tick
MoveTo(() => targetPosition);

// ✅ CORRECT - complex parameters via tuple
MoveTo(() => (targetPosition, stoppingDistance, invalidateDistance));
```

In `MoveTo` (Pawn.ItemAcquisition.cs, lines 15-35):
```csharp
public Node MoveTo(Func<(Vector3 position, float stoppingDistance, float invalidateDistance)> getParams, Action lifecycle = null) => Leaf("Move To", () =>
{
    OnInvalidCheck(() => Vector3.Distance(getParams().position, stoppingPosition.Value) > getParams().invalidateDistance);
    OnSuccess(() => stoppingPosition.Value = transform.position);
    
    OnBaseTick(() =>
    {
        var (targetPosition, stoppingDistance, _) = getParams();  // Fresh call every tick!
        return MoveTo(targetPosition, stoppingDistance) 
            ? Status.Success 
            : Status.Running;
    });
    
    lifecycle?.Invoke();
});
```

Notice how `getParams()` is called inside `OnBaseTick()` and `OnInvalidCheck()` - it gets fresh values every evaluation, not a stale copy from creation time.

### Nodes with Return Values

Nodes can return values via `out` parameters. This allows parent nodes or callers to get results from a node after it completes.

**Pattern**:
```csharp
Node NodeWithReturn(int maxTicks, out Func<int> getValue)
{
    var _value = 0;
    getValue = () => _value;

    return Leaf("Example", () =>
    {
        var value = Variable(() => 0);

        OnBaseTick(() =>
        {
            value.Value++;
            _value = value.Value;
            return _value < maxTicks ? Status.Running : Status.Success;
        });
    });
}
```

**Key Points**:

1. **Why use `_value` (backing field)?** `Variable()` can only be declared inside node setup closures. You cannot pass `Variable` objects around or store them outside the node. The backing field `_value` bridges this gap, allowing external code to access the node's state.

2. **Variables stay inside nodes**: Variables must remain locally scoped within their node's setup closure:
   ```csharp
   // ❌ WRONG - Don't do this
   Variable myVar;
   Leaf("Node", () =>
   {
       myVar = Variable(() => 0);  // Can't escape the closure
   });

   // ✅ CORRECT - Keep variables inside their node
   Leaf("Node", () =>
   {
       var myVar = Variable(() => 0);  // Local to the node's setup
   });
   ```

3. **Return the value via Func**: The backing field is wrapped in a `Func<T>` so callers can query the value at any time:
   ```csharp
   var node = NodeWithReturn(5, out var getCurrentValue);
   // Later, after the node has ticked...
   int currentValue = getCurrentValue();
   ```

4. **Out parameters make nodes composable**: This pattern allows building complex behavior trees where parent nodes depend on child node state without breaking encapsulation.

### Wiring Values: Combining Func Parameters and Func Returns

The combination of Func parameters and Func returns creates a powerful pattern for wiring values between nodes. You can pass return values directly as parameters without wrapping them in a lambda:

```csharp
// Node A returns a value
var nodeA = NodeWithReturn(5, out var getValueFromA);

// Node B takes that value as a parameter - pass directly
var nodeB = NodeThatNeedsValue(getValueFromA);

// Now when B runs, it automatically gets the latest value from A
```

**Why this works**:
- **Parameters as Func**: Ensure nodes always read fresh values
- **Returns as Func**: Ensure external code always reads fresh values
- **Direct passing**: Pass one node's `Func<T>` return directly to another node's `Func<T>` parameter without lambda wrapping
- **Together**: Create a reactive pipeline where changes automatically propagate

**Practical Example**:
```csharp
Sequence("Main", () =>
{
    // Node that produces a value
    MoveTo(() => targetPosition, out var getFinalPosition);
    
    // Node that consumes that value - pass directly
    WaitAt(getFinalPosition);
});
```

This pattern is essential for keeping behavior trees responsive and ensuring all nodes operate on current data rather than stale snapshots. Direct function passing keeps the code clean and the data flow explicit.

### Reactive Trees and Invalidation

Reactive trees enable dynamic re-evaluation of behavior when conditions change. This is one of ClosureAI's most powerful features for creating responsive AI.

#### Marking a Node as Reactive
 

The `Reactive` struct uses C# operator overloading to mark nodes. There are several ways to mark a node as reactive:

**Method 1: Assign to Variable (Preferred when assigning to Tree)**
```csharp
Tree = Reactive * SequenceAlways("Root", () => { ... });
```

**Method 2: Discard Pattern (Preferred when not assigning)**
```csharp
_ = Reactive * SequenceAlways("Root", () => { ... });
```

The discard pattern (`_ =`) is necessary because C# doesn't allow expressions like `Reactive * Node()` as standalone statements. This pattern is **preferred for creating reactive nodes inline** without assigning them to a variable:

```csharp
Tree = Reactive * SequenceAlways("Hi", () =>
{
    WaitUntil("Hi", () => Zero, () => { /* ... */ });

    _ = Reactive * SequenceAlways("Root", () =>
    {
        Sequence("WHAT", () => { /* ... */ });
        Sequence("Hmm", () => { /* ... */ });
        JustRunning();
    });
});
```

**Method 3: Use NonReactive to explicitly disable reactivity**
```csharp
Tree = NonReactive * Sequence("Static", () => { ... });
```

**Method 4: Mark the current node during setup**
```csharp
Sequence("MySeq", () => {
    MarkAsReactive();
    // ... children
});
```

**Important**: The `IsReactive` flag is checked by nodes that implement their usages, like Sequence and Selector nodes. Child nodes don't automatically become reactive - the parent composite checks its own `IsReactive` flag to decide whether to perform invalidation checks.

#### How Invalidation Works

Each node can define an `OnInvalidCheck()` callback that returns `true` when the node needs to re-evaluate:

```csharp
OnInvalidCheck(() => Vector3.Distance(target, lastPosition) > maxDistance);
```

#### Reactive Sequence/SequenceAlways/Selector Behavior

When a reactive Sequence, SequenceAlways, or Selector is ticking, it checks **all previously completed children** for invalidation:

```csharp
// Pseudocode of the reactive invalidation check
for (var i = 0; i < currentIndex; i++)
{
    var child = children[i];
    
    if (child.IsInvalid() && child.Done)
    {
        // Found an invalidated node!
        // Reset all nodes AFTER the invalidated node
        for (var j = children.Count - 1; j > i; j--)
            children[j].ResetGracefully();
        
        // Jump back to the invalidated node
        currentIndex = i;
        break;
    }
}

// Now tick the current node with allowReEnter=true
children[currentIndex].Tick(out status, allowReEnter: true);
```

#### The Invalidation Flow Example

Given this tree structure:
```csharp
Tree = Reactive * SequenceAlways("Root", () =>
{
    Sequence("Foo", () =>
    {
        OnEnabled(() => Debug.Log("Enabled Foo"));
        OnEnter(() => Debug.Log("Enter Foo"));
        OnDisabled(() => Debug.Log("Disabled Foo"));
        
        D.Condition(() => A);
        Leaf("A Action", () =>
        {
            OnEnter(() => Debug.Log("Enter A"));
            OnExit(async ct =>
            {
                Debug.Log("Exit A");
                await UniTask.WaitForSeconds(1.5f, cancellationToken: ct);
            });
        });
        
        Wait(0.1f);
    });
    
    Sequence("Bar", () =>
    {
        D.Condition(() => B);
        Leaf("B Action", () => {});
        
        Wait(1f);
    });
    
    JustRunning();
});
```

**Scenario**: Sequence "Foo" completes, then "Bar" starts running, but then Condition "A" invalidates (because the condition changed).

**What Happens**:
1. Root SequenceAlways checks all previous children (index 0: "Foo")
2. Detects that Condition "A" inside "Foo" has invalidated
3. Calls `ResetGracefully()` on all nodes after "Foo" (resets "Bar", "JustRunning")
4. Sets currentIndex back to 0 (the "Foo" sequence)
5. Calls `Tick(allowReEnter: true)` on "Foo"
6. "Foo" sequence receives `allowReEnter=true`, transitions from Done → Entering
7. "Foo"'s `OnEnter()` callbacks execute: **"Enter Foo" is logged**
8. "Foo" then ticks its children, starting with Condition "A"
9. Condition "A" also re-enters, calling its `OnEnter()`: **"Enter A" is logged**

**Key Points**:
- **OnEnabled is NOT called** during re-entry (node is still Active)
- **OnEnter IS called** during re-entry (fresh entry into the node's logic)
- **OnDisabled is NOT called** on "Foo" (it never fully deactivated)
- Nodes after the invalidated one are reset gracefully
- The invalidated node and its parents re-enter cleanly

#### Condition Decorator and Invalidation

The `Condition` decorator has built-in invalidation logic:

```csharp
OnInvalidCheck(() =>
{
    var childIsInvalid = node.Child.IsInvalid();
    
    if (condition() && childIsInvalid)
        return true;
    
    // Invalidate if the condition changed from previous evaluation
    return condition() != previousValue;
});
```

This means:
- If the condition changes from `true` to `false` or vice versa, it invalidates
- If the condition is `true` and the child is invalid, it invalidates
- This allows reactive trees to respond to external state changes

#### Practical Example: Dynamic Patrol

```csharp
Reactive * Sequence("Patrol", () =>
{
    var waypoint = Variable(() => GetNextWaypoint());
    
    MoveTo(() => waypoint.Value, () =>
    {
        // If waypoint changes while moving, invalidate and re-plan
        OnInvalidCheck(() => waypoint.Value != GetCurrentTargetWaypoint());
    });
    
    Wait(2f); // Wait at waypoint
});
```

If the waypoint changes while waiting, the reactive system will:
1. Detect MoveTo invalidated
2. Reset the Wait gracefully
3. Re-enter MoveTo with the new waypoint
4. Start moving to the new location

#### Visual: Reactive Invalidation Flow

```
Reactive Sequence State:
[MoveTo: Done] -> [Wait: Running] -> [NextAction: None]
                       ^
                   currently here

Condition changes, MoveTo.IsInvalid() returns true!

Step 1: Detect invalidation
[MoveTo: Done, INVALID!] -> [Wait: Running] -> [NextAction: None]

Step 2: Reset nodes after invalidated node (j from Count-1 down to i+1)
[MoveTo: Done, INVALID!] -> [Wait: Exiting...] -> [NextAction: None]
                              └─> ResetGracefully()

Step 3: All subsequent nodes reset
[MoveTo: Done, INVALID!] -> [Wait: None] -> [NextAction: None]

Step 4: Set currentIndex back to invalidated node
currentIndex = 0 (MoveTo)

Step 5: Tick invalidated node with allowReEnter=true
[MoveTo: Entering] -> [Wait: None] -> [NextAction: None]
    └─> OnEnter() called! (NOT OnEnabled)

Step 6: Continue execution from re-entered node
[MoveTo: Running] -> [Wait: None] -> [NextAction: None]
```

**Key Insight**: The reactive system creates a "time travel" effect - jumping back to an earlier point in the sequence when conditions change, while properly cleaning up any work that was done after that point.

### Async Support
All lifecycle callbacks support async via UniTask:
```csharp
OnExit(async ct =>
{
    await UniTask.WaitForSeconds(1.5f, cancellationToken: ct);
});
```
The `ct` (CancellationToken) is cancelled when the node is reset.

### Active Flag
- Set to `true` just before Enabling
- Set to `false` right after Disabling
- Indicates whether the node is currently "alive" in the tree

**Node.cs Reference**: Lines 162, 265, 272

### Done Property
```csharp
public bool Done => SubStatus == SubStatus.Done && Status is not Status.None;
```
Returns true when node has completed and is in Done state.

**Node.cs Reference**: Line 77

### Resetting Flags
- **Resetting**: Node is being reset (either immediately or gracefully)
- **ResettingGracefully**: Node is being reset but allowing async operations to complete
- These flags control how CancellationTokens behave

**Node.cs Reference**: Lines 79-115

## Common Patterns

### SequenceAlways vs Sequence

**Sequence** (Lines 36-102 in Sequence.cs):
- Stops execution when a child **fails** and returns `Status.Failure`
- Only returns `Status.Success` when **all** children succeed
- Short-circuits on failure: doesn't execute remaining children after a failed child

**SequenceAlways** (Lines 36-94 in SequenceAlways.cs):
- **Always continues executing all children regardless of their status**
- Returns `Status.Success` only if **all** children succeeded
- Ignores child failures and keeps moving forward through the sequence
- Used when you need all children to run to completion, even if some fail

**Key Difference in Code**:
- **Sequence**: Checks `if (status == Status.Failure) return Status.Failure;` (lines 66-67, 89-90)
- **SequenceAlways**: Never returns early on failure; always continues to the next child

### Yield - Dynamic Node Insertion and Recursion

Yield nodes enable **runtime insertion of nodes** into the behavior tree. This is one of ClosureAI's most powerful features, allowing for:
- **Recursion**: Nodes can call themselves or other nodes recursively
- **Dynamic behavior switching**: Change which subtree is executing based on runtime conditions
- **Planning systems**: Build complex planning hierarchies like GOAP (Goal-Oriented Action Planning)

#### YieldSimpleCached - The Simple Approach

For most cases, use `YieldSimpleCached` to insert a single node that gets cached:

```csharp
YieldSimpleCached(() => AcquireItem(requiredItemID));
```

**How it works**:
- The lambda `() => Node` is evaluated once when the yield node first ticks
- The returned node is cached and reused for all subsequent ticks - **the same node instance is used every time**
- When the yield node exits, the cached node is reset and cleared
- When the yield node re-enters, the same cached node is reused (not recreated)

**Use when**: You want to insert a single node that should persist across multiple ticks but reset when the parent changes.

#### YieldSimpleCached Example - Recursion

The classic example is recursive item acquisition:

```csharp
public Node AcquireItem(Func<string> getItemID) => Selector("Acquire Item", () =>
{
    var itemID = Variable(getItemID);
    
    Condition("Already Have", () => Inventory.Contains(itemID.Value));
    
    D.Condition("Item In World", () => ItemExists(itemID.Value));
    YieldSimpleCached(() => CollectItem(getItemID));
    
    D.Condition("Craftable", () => IsCraftable(itemID.Value));
    YieldSimpleCached(() => CraftItem(getItemID));  // CraftItem calls AcquireItem recursively!
});

private Node CraftItem(Func<string> getItemID) => Sequence("Craft", () =>
{
    var recipe = Variable(() => GetRecipe(getItemID()));
    
    D.ForEach(() => recipe.Value.RequiredItems, out var requiredItemID);
    YieldSimpleCached(() => AcquireItem(requiredItemID));  // Recursion! Yields a subtree that may yield more subtrees
    
    Wait("Perform Crafting", () => recipe.Value.CraftTime);
});
```

**What happens during recursion**:
1. `AcquireItem("Sword")` runs
2. Sword is craftable, so it yields `CraftItem("Sword")`
3. `CraftItem` needs "Iron" and "Wood"
4. For "Iron", it yields `AcquireItem("Iron")` - **recursion!**
5. `AcquireItem("Iron")` finds iron in the world and collects it
6. When `AcquireItem("Iron")` succeeds, control returns to `CraftItem`
7. For "Wood", it yields `AcquireItem("Wood")` - **recursion again!**
8. Once all items are acquired, `CraftItem` completes and crafts the sword

This creates a **dynamic tree structure** that adapts to the current goal, building arbitrarily deep hierarchies based on crafting recipes.

#### YieldDynamic - The Advanced Approach

For complex scenarios requiring fine control, use `YieldDynamic`:

```csharp
YieldDynamic("Dynamic Behavior", controller =>
{
    controller
        .WithResetYieldedNodeOnNodeChange()
        .WithResetYieldedNodeOnSelfExit();
    
    return _ =>
    {
        // This lambda is called EVERY tick - you control when nodes change
        if (IsInCombat())
            return combatNode;
        else if (IsIdle())
            return idleNode;
        else
            return patrolNode;
    };
});
```

**How it works**:
- The setup function receives a `YieldController` for configuration
- Returns a function `Func<YieldController, Node>` that is **called every tick**
- You decide when to return a different node (causing a switch)
- The controller configuration determines what happens during switches

#### YieldController Policies

The `YieldController` gives you fine-grained control over yield behavior:

**1. NodeChangeResetPolicy** - What happens when switching between different nodes:
```csharp
controller.WithResetYieldedNodeOnNodeChange();  // Reset old node before switching (default for YieldSimpleCached)
controller.OnNodeChange(YieldResetPolicy.None); // Don't reset (abrupt switch)
```

**2. NodeExitResetPolicy** - What happens when the yield node exits:
```csharp
controller.WithResetYieldedNodeOnSelfExit();    // Reset child when yield exits (default for YieldSimpleCached)
controller.OnSelfExit(YieldResetPolicy.None);   // Don't reset child on exit
```

**3. NodeCompletedPolicy** - What happens when the yielded node completes:
```csharp
controller.OnSelfCompleted(NodeCompletedPolicy.Return);  // Return child's status (default)
controller.WithLooping();                                // Reset child and continue running
```

**4. ConsumeTickOnStateChange** - Whether state changes happen immediately or wait for next tick:
```csharp
controller.WithConsumeTickOnStateChange(true);   // State changes wait for next tick (default)
controller.WithConsumeTickOnStateChange(false);  // State changes happen immediately in same tick
```

#### YieldLoop - Continuous Execution

`YieldLoop` is a shorthand for a yield node that automatically resets and continues when children complete:

```csharp
YieldLoop(controller =>
{
    return _ => 
    {
        // This node will reset and run again when it succeeds/fails
        return DoWork();
    };
});
```

Equivalent to:
```csharp
YieldDynamic(controller =>
{
    controller.WithLooping();
    return _ => DoWork();
});
```

#### When to Use Each Variant

| Variant | Use When | Example |
|---------|----------|---------|
| **YieldSimpleCached** | Single node, cache across ticks, reset on parent change | Recursive planning (AcquireItem) |
| **YieldDynamic** | Need control over when nodes switch, complex state logic | State machine (Idle/Patrol/Combat) |
| **YieldLoop** | Node should continuously reset and run again | Infinite patrol loop |

#### Common Patterns

**Pattern 1: Recursive Hierarchies**
```csharp
YieldSimpleCached(() => RecursiveNode(param));
```
Yields a node that may yield more nodes of the same type.

**Pattern 2: Dynamic State Switching**
```csharp
YieldDynamic(controller =>
{
    controller.WithResetYieldedNodeOnNodeChange();
    return _ => SelectStateNode();  // Returns different nodes based on state
});
```

**Pattern 3: Infinite Loop**
```csharp
YieldLoop(_ => MyRepeatingBehavior());
```

**Pattern 4: Nested Yields**
```csharp
YieldSimpleCached(() => 
    Sequence("Plan", () =>
    {
        YieldSimpleCached(() => SubGoal1());
        YieldSimpleCached(() => SubGoal2());
    })
);
```
Yields can be nested arbitrarily deep, creating complex dynamic hierarchies.

#### Key Insights

1. **Yield enables recursion**: Without Yield, you can't have a node that creates instances of itself at runtime
2. **Cache vs Re-evaluate**: `YieldSimpleCached` caches the node; `YieldDynamic` re-evaluates every tick
3. **Graceful switching**: When configured to reset on change, old nodes clean up properly before switching
4. **Planning systems**: Yield is perfect for GOAP-like planning where goals dynamically create subgoals
5. **Tree structure changes**: Yielded nodes actually modify the tree's Children list at runtime

### D.* Decorators
All decorators in the `D` namespace modify child behavior:
- `D.Until(Status)`: Repeat child until status achieved
- `D.Condition(Func<bool>)`: Run child only when condition is true
- `D.Invert()`: Flip child's success/failure
- `D.Repeat()`: Run child infinitely
- `D.ForEach(list, out item)`: Run child for each item in list

**Decorators Directory**: Various files showing different patterns

## Reset Behaviors

### ResetImmediately()
- Cancels all async operations immediately
- Forces transitions through exit/disable phases
- Does not wait for callbacks to complete
- Suitable for emergency cleanup (e.g., OnDestroy)

### ResetGracefully()
- Allows async operations to complete
- Sets ResettingGracefully flag
- Cancellation tokens remain valid
- Suitable for normal tree resets

## Integration Points

### Tick in Update Loop
```csharp
void Update()
{
    Tree.Tick();
}
```

### Cleanup in OnDestroy
```csharp
void OnDestroy()
{
    Tree.ResetImmediately();
}
```

### Node Graph Visualization and Time Travel Debugging

When you expose a Node as a **public field** in a MonoBehaviour, the Unity Inspector displays an **"Open Node Graph"** button. This opens an interactive visual representation of your entire behavior tree structure.

**What the Node Graph shows**:
- Visual layout of all nodes in the tree hierarchy
- Current status of each node (Running, Success, Failure, etc.) with color coding
- All variables within each node and their current values
- Live updates as the tree executes

**Time Travel Debugging**:
The Node Graph is especially powerful for debugging because it supports **time travel debugging**:
- Step through the tree execution frame-by-frame
- Pause and inspect the exact state of all nodes at any point in time
- View variable values frozen at specific moments in execution
- Navigate backward/forward through the execution history

This makes it easy to understand what happened when a node succeeded/failed or why a condition evaluated unexpectedly. Instead of guessing through logs, you can visually inspect the exact state of the entire tree at any frame.

**Quick Tip**: Double-click any node in the visualizer to jump directly to that node's code in your IDE. This instantly opens the file and navigates to the exact line where that node is defined.

## Status Transitions Summary

| From State   | To State   | Trigger                             | Callbacks Executed      |
|--------------|------------|-------------------------------------|-------------------------|
| None         | Enabling   | First Tick()                        | Variables initialized   |
| Enabling     | Entering   | OnEnabled complete                  | OnEnabled               |
| Entering     | Running    | OnEnter complete                    | OnEnter                 |
| Running      | Succeeding | BaseTick returns Success            | -                       |
| Running      | Failing    | BaseTick returns Failure            | -                       |
| Succeeding   | Exiting    | OnSuccess complete                  | OnSuccess               |
| Failing      | Exiting    | OnFailure complete                  | OnFailure               |
| Exiting      | Done       | OnExit complete                     | OnExit                  |
| Done         | Entering   | Tick(allowReEnter: true)            | OnEnter (NOT OnEnabled) |
| Any          | Disabling  | ResetImmediately/ResetGracefully    | OnExit (if needed)      |
| Disabling    | None       | OnDisabled complete                 | OnDisabled              |

## Important Notes for LLMs

1. **OnDisabled is separate**: Don't expect it during normal success/failure flows - only during resets
2. **Async is pervasive**: Most lifecycle callbacks support async/await with UniTask
3. **CancellationTokens matter**: Always use the provided `ct` parameter in async callbacks
4. **SubStatus is detailed**: Don't conflate Status and SubStatus - they serve different purposes
5. **Re-entry is controlled**: `BlockReEnter` and `allowReEnter` manage when nodes can restart
6. **Decorators use a stack**: Decorators are pushed via `D.*()` calls and wrapped around the next node - they don't take child nodes as parameters
7. **Variables are scoped**: Each node has its own variable list, initialized during Enabling
8. **Reactive invalidation is powerful**: Reactive trees check completed nodes for invalidation, reset subsequent nodes gracefully, then re-enter invalidated nodes (calling OnEnter but NOT OnEnabled)
9. **allowReEnter triggers OnEnter**: When a Done node is re-entered via `Tick(allowReEnter: true)`, it goes Done → Entering, calling OnEnter callbacks again
10. **Invalidation resets forward nodes**: When an early node invalidates, all nodes after it reset gracefully before the invalidated node re-enters
11. **Yield enables dynamic trees**: Use `YieldSimpleCached` for recursive planning and `YieldDynamic` for state machines - yielded nodes are inserted into the tree at runtime
12. **YieldSimpleCached caches nodes**: The lambda is evaluated once per entry and cached; use `YieldDynamic` when you need to re-evaluate every tick

## Debugging Tips

When analyzing behavior tree issues:
1. Check SubStatus to see exactly where a node is stuck
2. Look for Resetting flags to see if cleanup is happening
3. Verify CancellationTokens are being passed and used correctly
4. Confirm OnExit callbacks are properly cleaning up child nodes
5. Check if OnInvalidCheck is causing unexpected invalidations
6. Ensure decorators are calling child.Tick() appropriately
7. For reactive trees: Check if `IsReactive` is set on the composite node (not just children)
8. For reactive trees: Verify `OnInvalidCheck` returns `false` when the node is still valid
9. Watch for OnEnter being called multiple times due to re-entry (this is expected in reactive trees)

---

This document reflects the implementation in Node.cs and associated files. For the most up-to-date behavior, always refer to the source code.
