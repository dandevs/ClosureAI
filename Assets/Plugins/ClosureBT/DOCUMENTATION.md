# ClosureBT Documentation

Complete offline documentation for ClosureBT - A modern behavior tree framework for Unity that uses C# closures and async patterns.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Crash Course](#crash-course)
3. [Core Concepts](#core-concepts)
   - [Nodes](#nodes)
   - [Sequence](#sequence)
   - [Selector](#selector)
   - [Reactive Pattern](#reactive-pattern)
   - [Lifecycle Hooks](#lifecycle-hooks)
   - [Variables](#variables)
4. [Node Reference](#node-reference)
   - [Composite Nodes](#composite-nodes)
   - [Leaf Nodes](#leaf-nodes)
   - [Decorator Nodes](#decorator-nodes)
   - [Yield Node](#yield-node)
5. [Advanced](#advanced)
   - [Reactive Variables](#reactive-variables)
6. [Advanced Features](#advanced-features)
   - [HFSM+BT Hybrid](#hfsm-bt-hybrid)

---

## Getting Started

### Introduction to ClosureBT

ClosureBT is a modern behavior tree framework for Unity that uses C# closures and async patterns to create intuitive, maintainable AI systems.

### Quick Start

For the best experience with ClosureBT, start by importing the static AI class:

```csharp
using static ClosureBT.BT;
```

This gives you direct access to all ClosureBT nodes and functions without the `BT.` prefix.

### Basic Setup

Here's the basic setup for using ClosureBT:

```csharp
using static ClosureBT.BT;

public class YourNPC : MonoBehaviour
{
    public Node AI; // This will expose a "Open Node Graph" button in the inspector

    private void Awake() => AI = Sequence("NPC AI", () =>
    {
        Do(() => Debug.Log("Hello World"));
        Wait(1);
    });

    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();
}
```

> **Cleanup Tip**: You should probably call `ResetImmediately()` on your nodes in `OnDestroy` to ensure proper cleanup.

### Key Features

- **Embedded DSL** — Clean and simple DSL for declaring nodes
- **Composition based API** — Compose behaviors through functions
- **Time Travel Debugging** — Visual debugger with the ability to scrub through execution history
- **Async Support** — Lifecycle methods with synchronous and asynchronous paths
- **Graceful Interruptions** — Handle interruptions predictably and reliably with lifecycle methods
- **Parameterized Nodes** — Create nodes that can accept parameters and return values
- **Recursion** — Nodes can be recursive, enabling things such as Task Decomposition

### Example Behavior Tree

Create your first behavior tree in just a few lines of code:

```csharp
AI = Sequence("NPC", () =>
{
    Wait(1);
    Do(() => Debug.Log("Hello"));

    Selector("Make A Choice", () =>
    {
        Sequence(() =>
        {
            Condition("Is Angry", () => IsAngry);
            Pickup("Egg");
            ThrowItemAt(() => Target);
        });

        SomeOtherAction();
    });

    Dance();
    MoveTo("Bed");
});
```

---

## Crash Course

### Quick Overview

If you are new to behavior trees, it's recommended to read [Behavior Trees for AI: How They Work](https://www.gamedeveloper.com/programming/behavior-trees-for-ai-how-they-work) first. These docs focus more on ClosureBT usage rather than behavior tree fundamentals.

### Sequence - The AND Logic

Sequences are like `A() && B() && C()` in normal code. They run children in order and succeed only if all children succeed.

```csharp
Node Chase(Action lifecycle = null) => Sequence("Chase", () =>
{
    Condition("Target Too Far", () => Vector3.Distance(transform.position, target.position) > 5f);
    Leaf("Move Towards", () =>
    {
        OnEnter(() => Debug.Log("Begun to chase target!"));

        OnBaseTick(() =>
        {
            var distance = Vector3.Distance(transform.position, target.position);

            if (distance > 3f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
                return Status.Running;
            }
            else
                return Status.Success;
        });
    });

    // The second parameter allows us to attach lifecycle
    Wait(1f, () =>
    {
        OnSuccess(() => Debug.Log("Okay we're close enough now!"));
    });

    lifecycle?.Invoke();
});
```

**Key Takeaways:**
- Lifecycle methods like OnEnter and OnExit attach functionality to nodes
- Most functions have overloads where the first parameter is the node name
- Use a lifecycle parameter in custom nodes to add more functionality easily

### Selector - The OR Logic

Selectors are like `A() || B() || C()` in normal code. They try children sequentially until one succeeds.

```csharp
Node SomeAbstractChoices() => Selector("Choices!!!", () =>
{
    D.Condition(() => Foo == true);
    Sequence(() =>
    {
        Wait(1, () => OnSuccess(() => Debug.Log("Cool!")));
        Chase(() =>
        {
            OnSuccess(() => Debug.Log("We reached the target"));
            OnFailure(() => Debug.Log("We gave up chasing the target"));
        });
    });

    D.Condition(() => Bar == true);
    Wait("Pretend to do something", 1);

    // A final node that will run if Foo and Bar are both false
    Wait("Final", 1);
});
```

In terms of standard code, this might look like:

```csharp
void SomeAbstractChoices()
{
    if (Foo == true)
    {
        if (Wait(...) && Chase())
            return Status.Success;
    }
    else if (Bar == true)
    {
        if (Wait(...))
            return Status.Success;
    }
    else
    {
        if (Wait(...))
            return Status.Success;
    }

    return Status.Failure;
}
```

The real convenience comes from lifecycle methods that allow us to add non instaneous entry and exit methods.

### Why Lifecycle Methods Matter

Lifecycle methods like `OnEnter`, `OnExit`, `OnTick`, `OnSuccess`, and `OnFailure` make it incredibly easy to initialize and cleanup state without cluttering your behavior tree logic. You can:
- Attach setup code to any node with OnEnter
- Ensure cleanup always happens with OnExit
- React to specific outcomes with OnSuccess and OnFailure
- Do all this without modifying the core node structure
- Supports async operations

---

## Core Concepts

### Nodes

Nodes are the fundamental building blocks of behavior trees in ClosureBT.

#### Node Status

Every node in ClosureBT has three possible statuses during execution:

- **Running** - The node is currently executing and hasn't finished yet
- **Success** - The node completed successfully
- **Failure** - The node failed to complete

#### Creating Nodes

Nodes are created using static methods from ClosureBT. The simplest way to create a node is with a name and a setup action:

```csharp
// Create from an existing node
Node MySequence() => Selector("Foo", () =>
{
    Wait(1);
    // ...
});

// A custom leaf node
Node Baz() => Leaf("Baz", () =>
{
    OnBaseTick(() =>
    {
        return SomeCalculation() ? Status.Success : Status.Running;
    });
});
```

#### Resetting Nodes

You can reset a node to its initial state at any time:

```csharp
// Reset immediately (synchronous)
AI.ResetImmediately();

// Reset gracefully (may take multiple ticks for async cleanup)
AI.ResetGracefully();

// Common use: clean up when GameObject is destroyed
private void OnDestroy() => AI.ResetImmediately();
```

#### Composite Nodes

Composite nodes are special nodes that contain child nodes. They define how their children are executed:

```csharp
Node Root() => Selector("Root", () =>
{
    // Children added implicitly in order they appear
    Sequence("Patrol", () =>
    {
        MoveAroundPoints(() => PatrolPoints, () =>
        {
            OnEnter(() => Debug.Log("Began Patrolling"));
            OnExit(() => Debug.Log("Stopped Patrolling")
        });

        Wait(2f);
    });

    Attack();
});
```

#### Parameters and Return Values

Nodes can have parameters and return values by passing them around as `Func<T>`:

```csharp
Node Patrol(Func<List<T>> points, out Func<GameObject> getTargetSeen)
{
    VariableType<GameObject> targetSeen; // Variables must be made inside a node!
    targetSeen = () => targetSeen.Value;

    return Leaf("Patrol", () =>
    {
        targetSeen = new();

        OnBaseTick(() =>
        {
            // Patrol logic here
            // If target seen, set _targetSeen and return Success

            if (Sight.HasTargetInSight(out var target))
                targetSeen.Value = target;

            return targetSeen.Value ? Status.Success : Status.Running;
        });
    });
}
```

Now it can be used like this:

```csharp
Sequence(() =>
{
    Patrol(() => PatrolPoints, out var seenTarget);
    Attack(seenTarget);
});
```

---

### Sequence

Execute child nodes in order. Fails if any child fails. Like a logical AND operation.

#### Introduction

A Sequence tries its children in order. All children must succeed for the Sequence to succeed. If any child fails, the entire Sequence fails immediately.

Think of it like the logical AND operator (`&&`) in code:

```
Sequence("Task") → A() && B() && C()
```

All must succeed for the sequence to succeed.

#### Usage

Children are executed in the order they appear.

```csharp
Sequence("Move To", () =>
{
    // Must complete in order
    NavigateToTarget();
    Wait(1f);
    Celebrate();

    // If any fails, sequence fails
});
```

---

### Selector

Execute child nodes sequentially until one succeeds. Like a logical OR operation.

#### Introduction

A Selector tries its children in order. As soon as a child returns `Success`, the Selector also succeeds and stops trying other children. If all children fail, the Selector fails.

Think of it like the logical OR operator (`||`) in code:

```
Selector("Choose") → A() || B() || C()
```

Returns success as soon as one child succeeds.

#### Usage

```csharp
Selector(() =>
{
    // Try to attack first
    D.Condition(() => EnemyVisible);
    AttackEnemy();

    // If attack fails, try to chase
    D.Condition(() => EnemyTooFar());
    ChaseEnemy();

    // If chase fails, try to patrol
    Patrol();

    // If all fail, the selector fails
});
```

Children are executed in the order they appear. The first child to succeed ends the selector.

---

### Reactive Pattern

Implement reactive behavior that responds to condition changes and gracefully invalidates state.

#### Overview

A reactive node monitors for condition changes and gracefully resets its children when conditions invalidate. This is useful for behaviors that should respond immediately to state changes.

#### Making a Node Reactive

Use the `Reactive` keyword with the `*` operator to make a node reactive:

```csharp
void MyNode() => Reactive * Sequence(() =>
{
    // We add a "_ =", a discard operation, otherwise we'll get an
    // Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    _ = Reactive * Selector(() =>
    {
        // ...
    })
});
```

In the case of a reactive sequence, we check previous nodes if any have invalidated. Here's a complete example:

```csharp
using UnityEngine;
using static ClosureBT.BT;

public class Example : MonoBehaviour
{
    public Node AI;

    public bool foo;
    public bool bar;
    public bool baz;

    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();

    private void ExampleReactiveSequence() => Reactive * Sequence("Example", () =>
    {
        Condition(() => foo);
        Condition(() => bar);
        Condition(() => baz);

        Wait(5, () =>
        {
            OnEnter(() => Debug.Log("We reached the wait node!"));
            OnExit(() => Debug.Log("We exitted out of the wait node!"));
            OnSuccess(() => Debug.Log("Wait node success!"));
        });
    });
}
```

Now toggle `foo`, `bar`, or `baz` on and off. You'll see that if we're at the Wait node and any of the 3 conditions become false, the Wait node will exit!

#### Invalidation Check

Let's look at how `Condition` implements invalidation:

```csharp
Node Condition(string name, Func<bool> condition, Action lifecycle = null) => Leaf("Condition", () =>
{
    var _previous = Variable(false);

    SetNodeName(name);
    OnInvalidCheck(() => condition() != _previous.Value);

    OnBaseTick(() =>
    {
        _previous.Value = condition();
        return _previous.Value ? Status.Success : Status.Failure;
    });

    lifecycle?.Invoke();
});
```

In the previous example, when we reach a `Wait` node, the `Sequence` nodes marked with the `Reactive` flag will check their previously ran children using the provided `OnInvalidCheck`. For conditions, it's simple: has the value changed from when it attempted to run in `OnBaseTick`?

Let's write a `Follow` node:

```csharp
Node Follow(Action lifecycle = null) => Reactive * Selector("Follow", () =>
{
    var stoppingDistance = 2f;
    var chaseAgainDistance = 3f;

    OnInvalidCheck(() => // We return true if it is invalid
    {
        var distance = Vector3.Distance(transform.position, target.transform.position);
        return distance >= chaseAgainDistance;
    });

    OnBaseTick(() =>
    {
        var distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance >= stoppingDistance)
        {
            MoveTowards(target.transform.position);
            return Status.Running;
        }
        else
            return Status.Success;
    });

    lifecycle?.Invoke();
});
```

Here, we have a `Follow` node that will chase a target until within a stopping distance. It will be "Done" until the target moves beyond the `chaseAgainDistance`, at which point it will invalidate and re-run the logic.

The `Action lifecycle = null` parameter is nice to have since you can now attach lifecycle methods to your custom node:

```csharp
Follow(() =>
{
    OnTick(() => Debug.Log("Following the target..."));
    OnSuccess(() => Debug.Log("We reached the target!"));
    OnFailure(() => Debug.Log("We gave up following the target!"));
    // etc lifecycle methods
});
```

#### Important Notes

- Invalidation checks run every tick
- Use for frequently-changing conditions
- Consider caching expensive checks for performance

---

### Lifecycle Hooks

Define behavior at different execution phases of a node.

#### Execution Phases

Every node execution follows this sequence:

**First-Time Activation (Only on first tick)**
1. **OnEnabled** - Called once when the node is first activated (after variable initialization)

**Normal Execution (Every tick while running)**
2. **OnEnter** - Called once when the node starts or re-enters execution
3. **OnBaseTick** - Defines the core behavior and returns the node's status (Success/Failure/Running)
4. **OnTick** - Side-effect hook for logging, observations, and external systems (no status)
5. **OnSuccess / OnFailure** - Called when the node finishes with a specific status (Success or Failure)
6. **OnExit** - Called when the node finishes (Success or Failure)

**Reset/Cleanup (Only during reset operations)**
7. **OnDisabled** - Called ONLY when the node is reset (NOT part of normal success/failure flow)

#### Using Lifecycle Hooks

These lifecycle methods must be called inside a node's definition. Here's an example:

```csharp
Sequence(() =>
{
    OnEnter(() => Debug.Log("Entered Sequence Node!"));
    OnExit(() => Debug.Log("Exited Sequence Node!"));

    Wait(1f);
    // Rest of your nodes...
});
```

#### OnEnabled Hook

Called **once** when the node is first activated (transitions from `None` to `Enabling` state). This happens:
- On the **first tick** of the node
- **After** variable initializers run
- **Before** OnEnter is called
- Sets the node's `Active` flag to `true`

OnEnabled marks the node as "alive" in the behavior tree and is perfect for **one-time setup** that persists across re-entries.

**OnEnabled runs:**
- First tick of the node
- When tree is first started
- After `ResetImmediately()` or `ResetGracefully()` is called and node is ticked again

**OnEnabled does NOT run:**
- During re-entry (when reactive invalidation causes the node to restart)
- When `Tick(allowReEnter: true)` is called on a Done node
- After OnExit when the node completes normally

#### OnEnabled vs OnEnter

| OnEnabled | OnEnter |
|-----------|---------|
| Called **once** on first activation | Called **every time** node enters |
| Runs before OnEnter | Runs after OnEnabled |
| For persistent setup | For per-execution initialization |
| Sets Active = true | Node already active |
| Not called on re-entry | **IS** called on re-entry |

**Use OnEnabled For:**
- Event subscriptions that should persist across re-entries
- Resource allocation (NavMeshAgent setup, animation controller references)
- System registration (registering with game managers)
- One-time expensive setup that shouldn't repeat on re-entry

#### OnDisabled Hook

Called **only during reset operations** - this is the key difference from other lifecycle hooks. OnDisabled is **NOT** part of the normal success/failure flow.

**OnDisabled runs when:**
- `ResetImmediately()` is called on the node or its parent
- `ResetGracefully()` is called on the node or its parent
- During reactive invalidation when subsequent nodes need to be reset
- When the tree is destroyed (e.g., `OnDestroy()` calls `Tree.ResetImmediately()`)

**OnDisabled does NOT run when:**
- Node completes normally with Success or Failure
- OnExit is called at the end of execution
- Node transitions from Running to Done state
- Reactive re-entry occurs (node restarts via invalidation)

#### Why OnDisabled is Separate

ClosureBT separates **normal completion cleanup** (OnExit) from **reset/destroy cleanup** (OnDisabled) to distinguish between two fundamentally different scenarios:
- **OnExit** → "I finished my task, clean up this execution"
- **OnDisabled** → "I'm being removed from the tree, clean up all resources"

**Use OnDisabled For:**
- Unsubscribing from events registered in OnEnabled
- Releasing allocated resources (object pools, NavMeshAgents)
- Disconnecting from systems (event buses, game managers)
- Final cleanup that should only happen when the node is truly done (reset/destroyed)

#### OnEnterExitPair

Combines OnEnter and OnExit with matched actions:

```csharp
OnEnterExitPair(
    onEnter: () => Debug.Log("Starting"),
    onExit: () => Debug.Log("Done")
);
```

The OnExit() will only run if its OnEnter() counterpart has run. During interruptions/cancellations, it's possible not all OnEnters() will run due to cancellation. Use this to ensure that is OnEnter() counterpart has run for its OnExit() pair.

#### Async Support

Both OnEnabled and OnDisabled support async/await with UniTask and receive CancellationToken parameters for proper cleanup:

```csharp
OnEnabled(async ct =>
{
    await LoadResourcesAsync(ct);
    Debug.Log("Resources loaded");
});

OnDisabled(async ct =>
{
    await SaveStateAsync(ct);
    await UnloadResourcesAsync(ct);
    Debug.Log("Cleanup complete");
});
```

---

### Variables

Type-safe local state storage for nodes using the generic Variable system.

#### Overview

Variables are the way to store node-local state. Each variable is strongly typed and scoped to a node:

```csharp
Node MyNode() => Sequence("MyNode", () =>
{
    // Create variables - they are local to this node
    var counter = Variable(() => 0);
    var targetPos = Variable(() => Vector3.zero);
    var isMoving = Variable(() => false);

    OnTick(() =>
    {
        counter.Value++;
        Debug.Log($"Count: {counter.Value}");
    });
});
```

#### Creating Variables

Variables are created with the `Variable<T>` method:

```csharp
// Create variable with initial value
var health = Variable(() => 100);

// Type is inferred from the lambda return type
var position = Variable(() => Vector3.zero);

// Works with any type
var items = Variable(() => new List<Item>());
var target = Variable(() => (Transform)null);
```

The lambda is called once during node initialization to set the initial value.

#### Reading and Writing Values

Access variables via the `.Value` property:

```csharp
var counter = Variable(() => 0);

// Read the value
int current = counter.Value;
Debug.Log($"Current count: {current}");

// Write the value
counter.Value = 10;

// Modify the value
counter.Value++;
```

#### Using Variables as Functions

Variables support implicit conversion to `Func<T>`, so you can pass them directly to methods that expect a function:

```csharp
var counter = Variable(() => 0);

void LogValue(Func<int> getValue) => Debug.Log(getValue());

// Pass variable directly - implicit conversion
LogValue(counter);
```

The `.Fn` property provides access to the cached `Func<T>` delegate if you need to store it separately:

```csharp
var counter = Variable(() => 0);

// Get the cached function delegate
Func<int> getValue = counter.Fn;
int current = getValue();
```

---

## Node Reference

### Composite Nodes

Composite nodes orchestrate the execution of child nodes, defining control flow patterns like sequences, fallbacks, and parallel execution.

#### Overview

Composite nodes are the structural backbone of behavior trees. Unlike leaf nodes that perform actions, composites define **how** and **when** their children execute. They control the flow of execution through the tree, implementing patterns like sequential execution, fallback chains, and concurrent operations.

ClosureBT provides five core composite types, each with distinct execution semantics that solve different design problems.

#### Quick Comparison

| Composite | Execution | Success When | Failure When | Short-circuits |
|-----------|-----------|--------------|--------------|---------------|
| Sequence | Sequential | All succeed | Any fails | ✓ (on fail) |
| Selector | Sequential | Any succeeds | All fail | ✓ (on success) |
| Parallel | Simultaneous | All complete | N/A | ✗ |
| Race | Simultaneous | Any succeeds | All fail | ✓ (on success) |
| SequenceAlways | Sequential | All succeed | All fail | ✗ |

#### Sequence(string, Action) / Sequence(Action)

Executes children sequentially until one fails or all succeed. This is an "all must succeed" pattern that short-circuits on the first failure.

**Behavior:**
- Returns `Success` only if all children succeed
- Returns `Failure` as soon as any child fails (short-circuits)
- Returns `Running` while the current child is running
- Skips remaining children after a failure
- Returns Success immediately if no children exist

```csharp
Reactive * Sequence("Attack Enemy", () =>
{
    Condition("Has Target", () => target != null);
    Condition("In Range", () => Vector3.Distance(transform.position, target.position) < attackRange);
    Do("Perform Attack", () => Attack(target));
    Wait("Attack Cooldown", 1.5f);
});
// All steps must succeed in order. If target becomes null or moves out of range,
// the reactive system will reset and restart from the invalidated condition.
```

**Common Use Cases**: Sequential tasks (e.g., "open door → enter room → close door"), multi-step actions, guarded behaviors, initialization sequences.

#### SequenceAlways(string, Action) / SequenceAlways(Action)

Executes all children sequentially regardless of their success or failure. Unlike regular Sequence, this node always continues to the next child even if one fails.

**Behavior:**
- **ALWAYS** continues to the next child, even if one fails
- Returns `Success` only if ALL children succeeded
- Returns `Running` while children are still executing
- Does NOT short-circuit on failure (key difference from Sequence)

```csharp
SequenceAlways("Cleanup", () =>
{
    Do("Stop Movement", () => StopMoving());        // Runs even if this fails
    Do("Clear Target", () => target = null);        // Runs even if previous failed
    Do("Reset Animation", () => ResetAnimator());   // Runs even if previous failed
    Do("Play Idle", () => PlayIdleAnimation());     // Runs even if previous failed
});
// All cleanup steps execute regardless of individual failures
```

#### Selector(string, Action) / Selector(Action)

Executes children sequentially until one succeeds. This is a "try until success" pattern that short-circuits on the first successful child.

**Behavior:**
- Returns `Success` as soon as any child succeeds (short-circuits)
- Returns `Failure` only if all children fail
- Returns `Running` while the current child is running
- Skips remaining children after a success

```csharp
Selector("Acquire Item", () =>
{
    Sequence("Pick Up", () =>
    {
        Condition(() => itemNearby);
        Do(() => PickUpItem());
    });

    Sequence("Craft", () =>
    {
        Condition(() => canCraft);
        Do(() => CraftItem());
    });

    Do("Buy", () => BuyItem()); // Last resort
});
```

**Common Use Cases**: Fallback chains (e.g., "try melee OR ranged OR flee"), priority selection, conditional branching, error recovery.

#### Parallel(string, Action) / Parallel(Action)

Executes all child nodes simultaneously every tick. Returns `Success` only when all children have completed successfully.

**Behavior:**
- Ticks all children every tick, regardless of their status
- Re-enters children that have completed but are now invalid (reactive)
- Returns `Running` while any child is still running
- Returns `Success` only when all children are Done
- Exits all children in parallel when the node exits

```csharp
// Succeeds only when position reached AND animation finished AND 2 seconds elapsed
Parallel(() =>
{
    WaitUntil("Reached Position", () => Vector3.Distance(transform.position, target) < 0.1f);
    WaitUntil("Finished Animation", () => !animator.IsPlaying("Move"));
    Wait("Minimum Duration", 2f);
});
```

**Common Use Cases**: Running multiple independent behaviors simultaneously (e.g., "move AND rotate AND play animation"), waiting for multiple conditions to all be true, coordinating concurrent tasks.

#### Race(string, Action) / Race(Action)

Executes all children in parallel and succeeds as soon as any child succeeds. This is a "first-to-succeed wins" pattern.

**Behavior:**
- Ticks all children every tick simultaneously
- Returns `Success` as soon as any child succeeds (wins the race)
- Returns `Failure` only when all children have failed
- Returns `Running` while at least one child is still running
- Re-enters children that completed but are now invalid

```csharp
Race(() =>
{
    Sequence("Complete Mission", () =>
    {
        MoveTo(() => objective);
        Do(() => CompleteObjective());
    });
    WaitUntil("Enemy Spotted", () => enemyDetected); // Wins if enemy spotted first
    Wait("Timeout", 30f); // Wins after 30 seconds
});
// Succeeds with whichever child succeeds first
```

**Common Use Cases**: Alternative win conditions (e.g., "reach goal OR timer expires"), interrupt patterns (e.g., "patrol UNTIL enemy spotted"), timeout alternatives, first-response patterns.

#### Reactive Behavior

When marked with the `Reactive` decorator, composite nodes gain the ability to dynamically respond to changing conditions:

- Checks all previously completed children for invalidation each tick
- If a previous child invalidates, resets all subsequent children gracefully
- Restarts execution from the invalidated child (with allowReEnter=true)
- Enables truly dynamic behaviors that adapt to world state changes

```csharp
// Without Reactive: Locks in conditions once checked
Sequence("Attack", () =>
{
    WaitUntil("In Range", () => InRange(target));
    Do("Fire", () => Fire());
    Wait(1f);
});
// If target moves out of range after "In Range" succeeds, continues anyway

// With Reactive: Re-evaluates conditions continuously
Reactive * Sequence("Attack", () =>
{
    WaitUntil("In Range", () => InRange(target));
    Do("Fire", () => Fire());
    Wait(1f);
});
// If target moves out of range during Fire or Wait, invalidates and restarts
```

#### Choosing the Right Composite

Use this decision tree to select the appropriate composite:

**Do children need to run at the same time?**
- **Yes →**
  - **All must complete?** → Use `Parallel`
  - **First to succeed wins?** → Use `Race`
- **No →** Run sequentially
  - **All must succeed?**
    - **Stop on first failure?** → Use `Sequence`
    - **Run all regardless?** → Use `SequenceAlways`
  - **Any can succeed?** → Use `Selector`

---

### Leaf Nodes

Leaf nodes are the fundamental building blocks that perform actions, check conditions, and control timing in your behavior trees.

#### Overview

Leaf nodes are the atomic units of behavior trees—they have no children and perform specific tasks like evaluating conditions, executing actions, or waiting for events. Every behavior tree is ultimately composed of leaf nodes orchestrated by composites and decorators.

This section covers all the core leaf nodes available in ClosureBT, organized by their primary purpose.

#### Quick Reference

| Node | Returns | Primary Use Case |
|------|---------|------------------|
| Condition | Success/Failure | Evaluate boolean conditions |
| Do | Success | Execute actions that always succeed |
| Wait | Running → Success | Wait for a time duration |
| WaitUntil | Running → Success | Wait until condition becomes true |
| WaitWhile | Running → Success | Wait while condition is true |
| JustRunning | Running (forever) | Placeholder, continuous invalidation |
| JustSuccess | Success | Unconditional success, fallback |
| JustFailure | Failure | Force failure, testing |
| JustOnTick | Running (forever) | Continuous per-tick updates |

#### Condition(string, Func<bool>) / Condition(Func<bool>)

Evaluates a boolean condition and returns `Success` if true, `Failure` if false. This node is reactive-aware and will invalidate when the condition value changes.

```csharp
Sequence(() =>
{
    Condition("Enemy in Range", () => Vector3.Distance(enemy.position, transform.position) < 10f);
    Do("Attack", () => AttackEnemy());
});
```

When used in a reactive tree, Condition nodes signal invalidation when the condition changes, causing parent sequences/selectors to re-evaluate.

#### Do(string, Action) / Do(Action)

Executes an action and immediately returns `Success`. Completes in a single tick and always succeeds.

```csharp
Sequence(() =>
{
    Do("Log Start", () => Debug.Log("Starting sequence"));
    Do("Set Flag", () => isActive = true);
    Do("Play Sound", () => audioSource.Play());
});
```

**Note**: If you need an action that can fail, use a custom `Leaf` node with custom OnBaseTick logic instead.

#### Wait(string, float) / Wait(string, Func<float>)

Waits for a specified duration before succeeding. The duration can be a constant or dynamically evaluated each tick.

```csharp
// Fixed duration
Sequence(() =>
{
    Do("Fire Weapon", () => Fire());
    Wait("Cooldown", 2f);
    Do("Ready", () => isReady = true);
});

// Dynamic duration
Sequence(() =>
{
    Do("Start Spell", () => CastSpell());
    Wait("Cast Time", () => spellData.castTime); // Duration can change
    Do("Complete Spell", () => CompleteSpell());
});
```

#### WaitUntil(string, Func<bool>) / WaitUntil(Func<bool>)

Waits until a condition becomes true, then succeeds. Returns `Running` while the condition is false, `Success` once it becomes true.

```csharp
Sequence(() =>
{
    Do("Start Animation", () => animator.Play("Attack"));
    WaitUntil("Animation Done", () => !animator.IsPlaying("Attack"));
    Do("Finish", () => CompleteAttack());
});
```

When used in a reactive tree, WaitUntil will signal invalidation if the condition becomes false after succeeding.

#### WaitWhile(string, Func<bool>) / WaitWhile(Func<bool>)

Waits while a condition remains true, then succeeds when it becomes false. This is the inverse of WaitUntil.

```csharp
Sequence(() =>
{
    Do("Engage Shield", () => shield.Activate());
    WaitWhile("Shield Active", () => shield.IsActive);
    Do("Shield Depleted", () => OnShieldDepleted());
});
```

**Difference from WaitUntil**: WaitUntil succeeds when condition becomes true, WaitWhile succeeds when condition becomes false.

#### JustRunning(string) / JustRunning()

Always returns `Running` and marks itself as always invalid, forcing re-entry in reactive trees each tick. Never completes on its own.

```csharp
// Keep a race running indefinitely
Race(() =>
{
    JustRunning(); // Keeps the race running
    WaitUntil(() => shouldStop); // Wins when condition becomes true
});

// Placeholder for ongoing behavior
Sequence(() =>
{
    Do("Setup", () => Initialize());
    JustRunning("Patrol"); // Placeholder until patrol is implemented
});
```

#### JustSuccess(string) / JustSuccess()

Always returns `Success` immediately. Completes in a single tick and does not perform any actions.

```csharp
// Optional task in sequence
Selector(() =>
{
    Condition("Has Weapon", () => hasWeapon);
    JustSuccess(); // Succeed anyway if no weapon (optional weapon system)
});

// Fallback success in selector
Selector(() =>
{
    Sequence("Try Primary", () => { /* ... */ });
    Sequence("Try Secondary", () => { /* ... */ });
    JustSuccess(); // Always succeed as last resort
});
```

#### JustFailure(string) / JustFailure()

Always returns `Failure` immediately. Completes in a single tick and does not perform any actions.

```csharp
// Force trying next option in selector
Selector(() =>
{
    JustFailure(); // Force trying next option
    Do("Fallback Action", () => PerformFallback());
});

// Testing failure paths
Sequence(() =>
{
    Do("Setup", () => Initialize());
    JustFailure(); // Force sequence to fail for testing
    Do("Never Runs", () => UnreachableCode());
});
```

#### JustOnTick(string, Action) / JustOnTick(Action)

Shorthand for `JustRunning` with an `OnTick` callback. Executes an action every tick and always returns `Running`. Never completes unless externally reset or interrupted.

```csharp
Sequence(() =>
{
    WaitUntil(() => enemySpotted);
    JustOnTick("Track Enemy", () =>
    {
        transform.LookAt(enemy.position);
        distanceToEnemy = Vector3.Distance(transform.position, enemy.position);
    });
});

// Continuous monitoring with termination condition
D.Until(() => targetLost);
JustOnTick("Update Tracking", () =>
{
    aimPosition = PredictTargetPosition(target);
    UpdateCrosshair(aimPosition);
});
```

---

### Decorator Nodes

Decorators modify or wrap child behavior with additional logic like repetition, inversion, or timing.

#### Overview

Decorators are utility nodes that wrap other nodes and modify their behavior. They use a **push-then-pop stack pattern**: you call a `D.*()` function to push a decorator, then immediately create a node which pops the decorator and wraps itself.

**Critical Rule**: Decorators must come BEFORE the node they decorate.

```csharp
Reactive * Sequence("Guard Dog", () =>
{
    var distance = Variable(() => 0f);
    OnTick(() => distance.Value = DistanceToPlayer());

    D.ConditionLatch("Too Close", () => distance.Value < 3f);
    D.Until("Far Enough", () => distance.Value > 5f);
    Chase(() => Player, () =>
    {
        OnEnter(() => Debug.Log("Guard Dog began chasing player!"));
        OnExit(() => Debug.Log("Guard Dog stopped chasing player."));
    });

    Sequence(() =>
    {
        WaitUntil("Return To Post", () => MoveTo(Post.transform.position));
        JustRunning("Idle");
    });
});
```

#### Available Decorators

**D.Condition(Func<bool>) / D.Condition(string, Func<bool>)**
Only runs child when condition is true.

**D.ConditionLatch(Func<bool>) / D.ConditionLatch(string, Func<bool>)**
Latches once the condition becomes true, continuing to run until completion even if condition becomes false.

**D.While(Func<bool>) / D.While(string, Func<bool>)**
Runs child while a condition remains true, failing when the condition becomes false.

**D.Repeat()**
Repeat child infinitely until interrupted.

**D.RepeatCount(int) / D.RepeatCount(Func<int>)**
Repeats its child node a specified number of times.

**D.Until(Status, Action) / D.Until(Func<bool>, Action)**
Repeats child until it returns a target status, or runs child until a condition becomes true.

**D.ForEach<T>(IEnumerable, out T) / D.ForEach<T, R>(IEnumerable, Func, out R)**
Runs child for each item in a list, with optional mapping function.

**D.Invert()**
Flips Success ↔ Failure.

**D.AlwaysFail()**
Always returns Failure, regardless of the child node's actual status.

**D.AlwaysSucceed()**
Always returns Success, regardless of the child node's actual status.

**D.Cooldown(float) / D.Cooldown(Func<float>)**
Enforces a cooldown period on its child node, preventing execution until duration has elapsed after completion.

**D.Timeout(float)**
Fails if its child doesn't complete within the specified timeout duration.

**D.Latched()**
Blocks invalidation signals from propagating upward from its child to parent nodes.

**D.ResetOnEnter()**
Resets its child node on both entry and exit, ensuring the child always starts fresh.

**D.ValueChanged<T>(Func<T>)**
Invalidates when a monitored value changes, enabling reactive behavior based on value changes.

---

### Yield Node

Master yield operations for complex control flow.

#### Introduction

Yield defers node creation until runtime, enabling patterns that aren't possible with static trees. This includes recursive task decomposition, dynamic behavior subtree swapping, and state machines that adapt to game state.

Unlike static composites where all children are created upfront, yielded nodes are created and managed during execution. This deferred execution allows nodes to call themselves recursively, which is essential for goal-oriented planning systems.

Yield enables creating reusable behavior functions that compose together. Complex behaviors can emerge from simple, testable components.

#### YieldSimpleCached

`YieldSimpleCached()` executes a dynamically created node. The node is created once, cached, and reused across ticks. Perfect for recursion and parameterized behaviors.

**Recursive Task Decomposition**

Yield enables recursive task decomposition where nodes can call themselves to resolve complex dependencies. This allows high-level goals to be automatically broken down into smaller steps.

Here's a crafting system where acquiring an item might require crafting it, which requires acquiring ingredients, which may themselves need to be crafted:

```csharp
Node AcquireItem(Func<string> getItemID) => Selector(() =>
{
    var itemID = Variable(getItemID);

    // Already have it?
    Condition(() => Inventory.Contains(itemID.Value));

    // Can collect it from world?
    D.Condition(() => ItemExists(itemID.Value));
    YieldSimpleCached(() => CollectItem(itemID));

    // Need to craft it? Recursively acquire ingredients
    D.Condition(() => IsCraftable(itemID.Value));
    YieldSimpleCached(() => CraftItem(itemID));
});

Node CraftItem(Func<string> getItemID) => Sequence(() =>
{
    var recipe = Variable(() => GetRecipe(getItemID()));

    // Acquire each ingredient recursively!
    D.ForEach(() => recipe.Value.Ingredients, out var item);
    YieldSimpleCached(() => AcquireItem(item));

    Wait(() => recipe.Value.CraftTime);
});
```

The tree handles arbitrarily deep dependency chains. For example, acquiring a sword would resolve to needing iron ore and wood, which might require crafting from iron nuggets, which requires mining—all executed in the correct order from a single high-level goal.

This works because `AcquireItem` calls `CraftItem`, which calls `AcquireItem` again for each ingredient. This recursive pattern is only possible because `YieldSimpleCached` defers node creation until runtime.

#### YieldDynamic

`YieldDynamic()` allows swapping entire behavior subtrees based on conditions. The function is called every tick to determine the active node, enabling state machines and dynamic behavior switching.

This provides full control over what node yields. Useful for scenarios like enemies that switch between patrol, chase, and attack behaviors, or any system that needs to change its behavior tree at runtime.

**State Machine Example**

Here's an enemy AI that switches between patrol, chase, and attack behaviors based on game state:

```csharp
enum State { Patrol, Chase, Attack }

YieldDynamic("Enemy AI", controller =>
{
    controller
        .WithResetYieldedNodeOnNodeChange()
        .WithResetYieldedNodeOnSelfExit();

    Node patrolNode = null;
    Node chaseNode = null;
    Node attackNode = null;

    return _ => currentState switch
    {
        State.Patrol => patrolNode ??= PatrolBehavior(),
        State.Chase => chaseNode ??= ChaseBehavior(),
        State.Attack => attackNode ??= AttackBehavior(),
        _ => IdleBehavior()
    };
});
```

> **Node Caching**: It's very important to not load the node eagerly! You'll most likely want to cache your nodes somehow, otherwise you'll be making a new node per tick.

**YieldController Configuration**
- `WithResetYieldedNodeOnNodeChange()` - Reset old node when switching (recommended)
- `WithResetYieldedNodeOnSelfExit()` - Reset node when yield exits
- `WithConsumeTickOnStateChange()` - Control immediate vs next-tick transitions

#### YieldSimpleCached vs YieldDynamic

Both yield types defer node creation, but they're optimized for different use cases:

| Feature | YieldSimpleCached | YieldDynamic |
|---------|------------------|--------------|
| Nodes managed | Single node | Multiple nodes |
| Evaluation | Once (cached) | Every tick |
| Configuration | None (automatic) | YieldController API |
| Best for | Recursion, simple yields | State machines, behavior switching |
| Boilerplate | Minimal | More verbose |

**Use YieldSimpleCached** when you need to call a single behavior function—especially for recursion and parameterized nodes. **Use YieldDynamic** when you need to switch between multiple different behaviors based on runtime conditions.

#### Summary

Yield enables several important patterns by deferring node creation until runtime:
- **Recursive task decomposition** — Nodes can call themselves to break down complex goals into smaller steps
- **Dynamic behavior switching** — Swap entire subtrees based on runtime conditions
- **Modular composition** — Create reusable behavior functions that compose together
- **State machines** — Implement adaptive AI that responds to changing game state

**Performance Tips:**
- **Always cache nodes:** Use the `node ??= CreateNode()` pattern to avoid recreating nodes every tick
- **Watch recursion depth:** Very deep hierarchies can impact performance—consider iterative alternatives for extreme cases
- **Choose the right tool:** YieldSimpleCached for single behaviors and recursion, YieldDynamic for state machines and behavior switching

---

## Advanced

### Reactive Variables

Learn about Functional Reactive Programming and composable transformation pipelines.

#### Functional Reactive Programming (FRP)

ClosureBT embraces **Functional Reactive Programming** principles through reactive variables and transformation pipelines. Variables are signal-based data streams that automatically propagate changes through a series of transformations.

> **What is FRP?**
> FRP treats variables as **streams of values over time**. When a source variable changes, that change flows through a pipeline of transformations automatically, creating a declarative data flow. Think of it like Excel formulas: when a cell changes, all dependent cells recalculate automatically.

#### UsePipe: Composing Transformations

`UsePipe` chains multiple transformation operations on a reactive variable, creating a functional pipeline where data flows from left to right through each stage:

```csharp
var mousePos = UseEveryTick(MousePlaneCastPosition);

// Create a pipeline: throttle → buffer → transform
var mousePosAverage = UsePipe(mousePos,
    v => UseThrottle(0.025f, v),          // Stage 1: Sample every 25ms
    v => UseRollingBuffer(15, false, v),  // Stage 2: Keep last 15 values
    v => UseSelect(v, buffer =>           // Stage 3: Calculate average
    {
        var avg = Vector3.zero;
        foreach (var pos in buffer)
            avg += pos;
        return avg / buffer.Count;
    }));

// Result: mousePosAverage automatically updates with smoothed mouse position
```

Each stage in the pipeline transforms the incoming data stream:
- **Source**: The origin of data (e.g., mouse position, sensor input)
- **Throttle**: Limits update frequency to prevent excessive computations
- **Buffer**: Collects multiple values for aggregate operations
- **Select/Transform**: Maps values to new forms (like LINQ's Select)

#### Use Methods: Reactive Operators

ClosureBT provides a library of `Use*` methods for building reactive pipelines. These are similar to RxJS operators or LINQ methods, designed for composing reactive data flows:

**UseEveryTick**
Samples a function or variable every tick, creating a reactive stream.
```csharp
var mousePos = UseEveryTick(MouseWorldPosition);
var health = UseEveryTick(() => player.health);
```

**UseThrottle**
Limits signal propagation to once per time period (in seconds).
```csharp
var mousePos = UseEveryTick(MousePosition);
var throttled = UseThrottle(0.1f, mousePos); // Max once per 100ms
```

**UseRollingBuffer**
Maintains a sliding window of the last N values.
```csharp
var positions = UseRollingBuffer(10, false, mousePos);
// positions.Value is a List<Vector3> with last 10 positions
```

**UseSelect**
Transforms each value through a projection function (like LINQ Select).
```csharp
var health = UseEveryTick(() => player.health);
var healthPercent = UseSelect(health, h => h / 100f);
```

**UseValueDidChange**
Returns true for one tick when the source variable signals a change.
```csharp
var target = UseEveryTick(() => currentTarget);
var targetChanged = UseValueDidChange(target);

OnTick(() => {
    if (targetChanged.Value)
        Debug.Log("New target acquired!");
});
```

**UseTimePredicateElapsed**
Tracks how long (in seconds) a condition has been true.
```csharp
var isMoving = UseEveryTick(() => velocity.magnitude > 0.1f);
var movingDuration = UseTimePredicateElapsed(isMoving, _ => isMoving.Value);
// movingDuration.Value = seconds the entity has been moving
```

#### Complete Use Methods Reference

All available reactive operators in ClosureBT:
- `UseEveryTick`
- `UseThrottle`
- `UseDebounce`
- `UseRollingBuffer`
- `UseSelect`
- `UseWhere`
- `UseScan`
- `UseValueDidChange`
- `UseDistinctUntilChanged`
- `UseCountChanged`
- `UseTimePredicateElapsed`
- `UseTicksElapsed`
- `UseElapsed`
- `UseWindowTime`
- `UsePipe`

---

## Advanced Features

### HFSM+BT Hybrid

Combine Hierarchical FSMs with Behavior Trees for the best of both paradigms.

#### The Hybrid Architecture

ClosureBT enables a **Hierarchical Finite State Machine + Behavior Tree (HFSM+BT)** hybrid architecture—a pattern recognized in academic research as combining the best of both paradigms.

> **Used in AAA Games**
> **Alan Wake 2** (Remedy Entertainment, 2023) uses this exact HFSM+BT hybrid architecture for its enemy AI. Watch the technical breakdown: [AI Architecture in Alan Wake 2](https://www.youtube.com/watch?v=NEKXGMLMjQE)
>
> The approach is also validated academically. The term "HFSMBTH" was coined by Zutell, Conner, and Schillinger in their 2022 IROS paper [*"Flexible Behavior Trees: In search of the mythical HFSMBTH"*](https://arxiv.org/abs/2203.05389), which argues that hybrid FSM+BT architectures leverage the strengths of both paradigms.

#### Why Hybrid?

**Pure FSM Limitations**
- **State explosion** — Adding conditions multiplies states exponentially
- **Rigid transitions** — Hard to add reactive interrupts
- **Code duplication** — Similar behaviors in different states
- **Maintenance burden** — Changes ripple across many states

**Pure BT Limitations**
- **No cyclic flow** — BTs are acyclic (DAG) by design
- **Tick overhead** — Re-evaluating entire tree every frame
- **Mode confusion** — Harder to reason about "current state"
- **Memory issues** — Stateless design can require workarounds

**The Key Insight**
FSMs and BTs solve *different problems*. FSMs answer **"What should I be doing?"** while BTs answer **"How do I do it?"**. Using both together—FSM for strategic decisions, BT for tactical execution—yields cleaner, more maintainable AI.

#### Implementation in ClosureBT

Use `YieldSimple` as your state machine. Each state returns a behavior tree node that handles the details of that mode.

**Basic Pattern with ??= Caching**

```csharp
Node idleNode = null, combatNode = null, fleeNode = null;

Node AI = YieldSimple("Enemy AI", () =>
{
    var state = Variable(() => 0); // 0=Idle, 1=Combat, 2=Flee
    var target = Variable<Entity>(() => null);

    // Global interrupt — checked every tick from any state
    OnTick(() => { if (Health < 20) state.Value = 2; });

    return () => state.Value switch
    {
        0 => idleNode ??= IdlePatrol(() =>
        {
            OnTick(() =>
            {
                target.Value = FindNearestThreat();
                if (target.Value != null) state.Value = 1;
            });
        }),

        1 => combatNode ??= CombatBehavior(() => target.Value, () =>
        {
            OnTick(() => { if (target.Value == null || !target.Value.IsAlive) state.Value = 0; });
            OnSuccess(() => state.Value = 0);  // Target eliminated
            OnFailure(() => state.Value = 2);  // Can't win → flee
        }),

        2 => fleeNode ??= FleeBehavior(() =>
        {
            OnSuccess(() => state.Value = 0);  // Escaped safely
        }),
        _ => JustRunning()
    };
});
```

**State Behaviors Accept Lifecycle**

```csharp
// Each state accepts lifecycle for transition hooks
Node IdlePatrol(Action lifecycle = null) => Reactive * Sequence(() =>
{
    PatrolWaypoints();
    D.Condition(() => CanSeeEnemy);  // Succeeds when enemy spotted
    lifecycle?.Invoke();
});

Node CombatBehavior(Action lifecycle = null) => Reactive * Selector(() =>
{
    D.Condition(() => Ammo == 0); Reload();
    AttackTarget();
    lifecycle?.Invoke();
});
```

#### Hierarchical State Machines

For complex AI, nest `YieldSimple` nodes to create hierarchical state machines. Each sub-FSM manages its own state space.

```csharp
Node peacefulFSM = null, combatFSM = null;

Node RootFSM() => YieldSimple("Root", () =>
{
    var mode = Variable(() => 0);
    OnTick(() => { if (ThreatDetected) mode.Value = 1; });

    return () => mode.Value switch
    {
        0 => peacefulFSM ??= PeacefulFSM(() => OnSuccess(() => mode.Value = 1)),
        1 => combatFSM ??= CombatFSM(() => OnSuccess(() => mode.Value = 0)),
        _ => JustRunning()
    };
});
```

**Architecture Overview**

```
Root FSM
    Peaceful ↔ Combat
        ↓
Sub-FSMs
    [Idle, Patrol, Gather] [Engage, Flee, Heal]
        ↓
Behavior Trees
    Reactive Sequences, Selectors, Conditions...
```

#### Global vs Local Transitions

**Two Types of Transitions**
- **Global** (in `OnTick`): Priority interrupts checked every tick—"flee when health < 20"
- **Local** (via lifecycle): Outcome-based transitions—"return to idle when combat succeeds"

The `??=` pattern ensures each state node is created once and cached. Lifecycle callbacks attached at creation time define how that state transitions to others.

#### Benefits of the Hybrid Approach

- **Clear Mental Model** - Designers think in states naturally. "What mode is the AI in?" is intuitive. BTs handle the complexity within each mode.
- **Maintainability** - Add new states without touching existing ones. Add behaviors within a state without affecting the state machine structure.
- **Reusability** - BT behaviors can be shared across states. The same `MoveTo()` works in Patrol, Combat, and Flee states.
- **Testability** - Test state transitions separately from behaviors. Test behaviors in isolation. Compose tested components into full AI.
- **Debuggability** - Current state is always clear. BT debugger shows exactly what's happening within that state. Time travel through both.
- **Performance** - Only tick the active state's BT. State transitions are O(1). No full-tree re-evaluation unless needed.

#### Further Reading

- **Video**: [AI Architecture in Alan Wake 2](https://www.youtube.com/watch?v=NEKXGMLMjQE) - Remedy's AI team explains their HFSM+BT hybrid approach for enemy AI.
- **Paper**: [Flexible Behavior Trees: In search of the mythical HFSMBTH](https://arxiv.org/abs/2203.05389) - Zutell, Conner, Schillinger (IROS 2022) — Academic foundation for HFSM+BT hybrids.
- **Docs**: [YieldSimple Documentation](/docs/yield) - Deep dive into ClosureBT's YieldSimple node that enables FSM patterns.

---

*This documentation was generated from the ClosureBT official documentation site.*
