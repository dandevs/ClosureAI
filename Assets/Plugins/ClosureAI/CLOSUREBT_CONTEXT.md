# ClosureBT Behavior Tree - Context for LLMs

## Table of Contents

1. [Overview](#overview)
2. [How to Construct Trees](#how-to-construct-trees)
3. [Node Lifecycle](#node-lifecycle)
4. [Key Concepts](#key-concepts)
5. [Common Patterns](#common-patterns)
6. [Important Notes for LLMs](#important-notes-for-llms)

## Overview

ClosureBT is a behavior tree library for Unity using declarative C# API to define AI behaviors.

### Key Distinguishing Features

1. **Detailed Lifecycle States**: Status (None/Running/Success/Failure) + SubStatus (None/Enabling/Entering/Running/Succeeding/Failing/Exiting/Disabling/Done)
2. **Async-First Design**: All lifecycle callbacks support UniTask async/await with cancellation tokens
3. **Reactive Invalidation**: Nodes can invalidate when conditions change, causing automatic re-entry with graceful cleanup
4. **Separated OnDisabled**: OnDisabled only fires during resets, NOT during normal tick flow
5. **Re-entry via allowReEnter**: Nodes can be re-entered (Done → Entering) without OnEnabled

## How to Construct Trees

### The Fundamental Pattern

```csharp
using static ClosureBT.BT;  // REQUIRED
using UnityEngine;

public class MyAI : MonoBehaviour
{
    public Node AI;  // Public for inspector visualization

    private void Awake() => AI = CreateAI();
    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();

    private Node CreateAI() => Sequence("Root", () =>
    {
        // Children go inside the lambda
    });
}
```

### The Lambda Closure Pattern

**All composite nodes take a lambda where you define children:**

```csharp
Sequence("My Sequence", () =>
{
    Wait(1f);
    Wait(2f);
});
```

**❌ WRONG:** `Sequence("My Sequence", Wait(1f), Wait(2f));`

### Node Types

**Leaf Nodes** (no children):
- `Leaf()` - Custom with `OnBaseTick`
- `JustRunning()` - Always returns Running
- `Wait()` / `WaitUntil()` - Timing
- `Condition()` - Boolean check

**Composite Nodes** (have children):
- `Sequence()` - Runs in order, fails on first failure
- `SequenceAlways()` - Runs all children regardless of failures
- `Selector()` - Succeeds on first success
- `Parallel()` - Runs all simultaneously

### Decorators - The D.* Pattern

**Decorators modify the next node created:**

```csharp
Sequence(() =>
{
    D.Condition(() => playerInRange);  // BEFORE the node
    Leaf("Attack", () => { /* ... */ });

    D.Condition(() => hasAmmo);
    D.Invert();
    Wait(1f);  // Only waits if we DON'T have ammo
});
```

Common decorators:
- `D.Condition(Func<bool>)` - Only runs child when true
- `D.Invert()` - Flips Success ↔ Failure
- `D.Until(Status)` - Repeats until target status
- `D.Repeat()` - Repeats infinitely
- `D.ForEach(list, out item)` - Runs child for each item

### Variables - State Inside Nodes

```csharp
Sequence(() =>
{
    var targetEnemy = Variable<GameObject>(() => null);
    var health = Variable(() => 100f);

    OnTick(() => {
        targetEnemy.Value = FindNearestEnemy();
        health.Value -= Time.deltaTime * 10f;
    });

    Leaf("Attack", () => {
        OnBaseTick(() => {
            if (targetEnemy.Value != null) Attack(targetEnemy.Value);
            return Status.Success;
        });
    });
});
```

**Critical**: The `Variable(() => initFunction)` lambda is called **once during Enabling** before OnEnabled callbacks fire. On re-entry, it runs again for fresh initialization.

### Reactive Trees

**Mark nodes as reactive for automatic re-evaluation:**

```csharp
AI = Reactive * SequenceAlways("Root", () =>
{
    Condition(() => playerInRange);
    AttackPlayer();
});

// Nested reactive nodes
Tree = Reactive * SequenceAlways("Root", () =>
{
    _ = Reactive * Selector("Behavior", () =>  // Use discard pattern when not assigning
    {
        D.Condition(() => isAggressive);
        AttackBehavior();
        PatrolBehavior();
    });
});
```

### Quick Reference Template

```csharp
using static ClosureBT.BT;
using UnityEngine;

public class MyAI : MonoBehaviour
{
    public Node AI;

    private void Awake() => AI = Reactive * SequenceAlways("Root", () =>
    {
        var myVariable = Variable(() => initialValue);
        OnTick(() => { /* every tick */ });

        D.Condition(() => someCondition);
        Leaf("DoWork", () => {
            OnBaseTick(() => {
                // Your logic here
                return Status.Success;
            });
        });

        Sequence("SubTree", () => {
            Wait(1f);
            Condition(() => checkSomething);
        });

        YieldSimpleCached(() => RecursiveNode());
        JustRunning();
    });

    private void Update() => AI.Tick();
    private void OnDestroy() => AI.ResetImmediately();
}
```

## Node Lifecycle

### Status Enum
- **None**: Not initialized
- **Running**: Actively executing
- **Success**: Completed successfully
- **Failure**: Failed

### SubStatus Enum
- **None** → **Enabling** → **Entering** → **Running** → **Succeeding/Failing** → **Exiting** → **Done**
- **Disabling**: Only during resets

### The Tick() Lifecycle Flow

```
None → Enabling → Entering → Running → Success/Failure
         |          |           |              |
         v          v           v              v
    OnEnabled    OnEnter    BaseTick()  Succeeding/Failing
                                              |
                                              v
                                      OnSuccess/OnFailure
                                              |
                                              v
                                           Exiting
                                              |
                                              v
                                            OnExit
                                              |
                                              v
                                             Done
```

**Key Phases:**
1. **None → Enabling**: Variables initialized, OnEnabled fires, Active = true
2. **Enabling → Entering**: OnEnter fires
3. **Entering → Running**: Ready for BaseTick
4. **Running**: OnPreTicks → BaseTick() → OnTicks
5. **Running → Succeeding/Failing**: OnSuccess/OnFailure fires
6. **Succeeding/Failing → Exiting**: OnExit fires
7. **Exiting → Done**: Final Status set, BlockReEnter set
8. **Done → Entering** (Re-entry): OnEnter fires again (NOT OnEnabled)

### OnDisabled - The Exception

**OnDisabled is NOT part of normal tick flow** - only called during:
1. `ResetImmediately()` - Immediate cleanup
2. `ResetGracefully()` - Waits for async operations
3. During Enabling/Entering/Running if reset occurs

### Status Transitions Table

| From       | To         | Trigger                  | Callbacks              |
|------------|------------|--------------------------|------------------------|
| None       | Enabling   | First Tick()             | Variables initialized  |
| Enabling   | Entering   | OnEnabled complete       | OnEnabled              |
| Entering   | Running    | OnEnter complete         | OnEnter                |
| Running    | Succeeding | BaseTick = Success       | -                      |
| Running    | Failing    | BaseTick = Failure       | -                      |
| Succeeding | Exiting    | OnSuccess complete       | OnSuccess              |
| Failing    | Exiting    | OnFailure complete       | OnFailure              |
| Exiting    | Done       | OnExit complete          | OnExit                 |
| Done       | Entering   | Tick(allowReEnter: true) | OnEnter (NOT OnEnabled)|
| Any        | Disabling  | Reset                    | OnExit (if needed)     |
| Disabling  | None       | OnDisabled complete      | OnDisabled             |

## Key Concepts

### Lifecycle Callbacks

Most node creators accept an optional `lifecycle` parameter:

```csharp
Wait("Collect", 1, () =>
{
    OnEnter(() => Debug.Log("Started"));
    OnExit(async ct => await Cleanup(ct));
    OnSuccess(() => CollectItem());
});
```

Available callbacks:
- `OnEnabled()` - First activation (once per Active session)
- `OnEnter()` - Each entry (including re-entry)
- `OnBaseTick()` - Main logic
- `OnTick()` - Every tick
- `OnSuccess()` / `OnFailure()` - On completion
- `OnExit()` - Cleanup
- `OnDisabled()` - Only on reset

### Parameters via Func

**Parameters must be `Func<T>` not direct values** to stay current:

```csharp
// ❌ WRONG - frozen at creation
MoveTo(targetPosition);

// ✅ CORRECT - fresh every tick
MoveTo(() => targetPosition);
MoveTo(() => (targetPosition, stoppingDistance, invalidateDistance));
```

### Nodes with Return Values

Use `out Func<T>` to return values:

```csharp
Node MoveTo(Func<Vector3> getTarget, out Func<Vector3> getFinalPosition)
{
    var _finalPosition = Vector3.zero;
    getFinalPosition = () => _finalPosition;

    return Leaf("Move", () =>
    {
        OnSuccess(() => _finalPosition = transform.position);
        OnBaseTick(() => MoveLogic());
    });
}

// Usage - pass return directly as parameter
MoveTo(() => target, out var getFinalPos);
WaitAt(getFinalPos);  // No lambda wrapping needed!
```

### Reactive Invalidation

**How it works:**
1. Reactive composites check all previously completed children for invalidation
2. When a child invalidates, all nodes AFTER it are reset gracefully
3. The invalidated node is re-entered with `allowReEnter=true`
4. OnEnter fires (NOT OnEnabled) during re-entry

```csharp
Reactive * Sequence("Root", () =>
{
    var distance = Variable(() => 0f);
    OnTick(() => distance.Value = DistanceToPlayer());

    D.ConditionLatch(() => distance.Value < 2f);
    D.Until(() => distance.Value > 5f);
    JustRunning("Flee");

    Sequence(() =>
    {
        WaitUntil(() => MoveToCenter());
        JustRunning("Idle");
    });
});
```

**Condition Decorator Invalidation:**
```csharp
OnInvalidCheck(() =>
{
    if (condition() && Child.IsInvalid()) return true;
    return condition() != previousValue;  // Invalidate on condition change
});
```

## Common Patterns

### SequenceAlways vs Sequence

- **Sequence**: Stops on first failure, returns Failure
- **SequenceAlways**: Continues through all children regardless of failures, returns Success only if all succeeded

### Yield - Dynamic Node Insertion

**YieldSimpleCached** - Cache single node, reuse across ticks:

```csharp
YieldSimpleCached(() => AcquireItem(requiredItemID));
```

**Recursive Planning Example:**
```csharp
public Node AcquireItem(Func<string> getItemID) => Selector("Acquire", () =>
{
    var itemID = Variable(getItemID);

    Condition("Have", () => Inventory.Contains(itemID.Value));

    D.Condition("In World", () => ItemExists(itemID.Value));
    YieldSimpleCached(() => CollectItem(getItemID));

    D.Condition("Craftable", () => IsCraftable(itemID.Value));
    YieldSimpleCached(() => CraftItem(getItemID));  // Recursion!
});

private Node CraftItem(Func<string> getItemID) => Sequence("Craft", () =>
{
    var recipe = Variable(() => GetRecipe(getItemID()));

    D.ForEach(() => recipe.Value.RequiredItems, out var requiredItemID);
    YieldSimpleCached(() => AcquireItem(requiredItemID));  // Calls itself recursively!

    Wait(() => recipe.Value.CraftTime);
});
```

**YieldDynamic** - Re-evaluate every tick with full control:

```csharp
YieldDynamic("State Machine", controller =>
{
    controller
        .WithResetYieldedNodeOnNodeChange()
        .WithResetYieldedNodeOnSelfExit();

    return _ =>  // Called EVERY tick
    {
        if (IsInCombat()) return combatNode;
        else if (IsIdle()) return idleNode;
        else return patrolNode;
    };
});
```

**YieldController Policies:**
- `WithResetYieldedNodeOnNodeChange()` - Reset old node when switching
- `WithResetYieldedNodeOnSelfExit()` - Reset child when yield exits
- `WithConsumeTickOnStateChange(bool)` - Control timing of state changes

### Node Graph Visualization

Expose Node as **public field** to access:
- **"Open Node Graph" button** in Inspector
- Visual tree hierarchy with live status updates
- **Time travel debugging** - step frame-by-frame, inspect historical state
- All variables and their values at any point in time
- **Double-click nodes** to jump to code

## Important Notes for LLMs

1. **OnDisabled is separate** - Only during resets, not normal flow
2. **Async is pervasive** - Use `async ct =>` in lifecycle callbacks
3. **CancellationTokens matter** - Always use provided `ct` parameter
4. **Re-entry triggers OnEnter** - `Tick(allowReEnter: true)` calls OnEnter, NOT OnEnabled
5. **Decorators use a stack** - `D.*()` pushes decorator, next node pops and wraps
6. **Variables are scoped** - Declare inside node setup lambdas
7. **Variable init timing** - `Variable(() => init)` runs during Enabling, before OnEnabled
8. **Reactive invalidation flow** - Check completed children → Reset forward nodes → Re-enter invalidated node
9. **Yield enables recursion** - `YieldSimpleCached` caches, `YieldDynamic` re-evaluates every tick
10. **Parameters as Func** - Always pass `Func<T>` not values to stay current
11. **Return values as Func** - Use `out Func<T>` pattern, can pass directly as parameters
12. **IsReactive flag** - Set on composite nodes, they check it to enable invalidation

### Debugging Checklist

1. Check **SubStatus** to see exact phase
2. Look for **Resetting flags** during cleanup
3. Verify **CancellationTokens** are passed correctly
4. Confirm **OnExit** properly cleans up children
5. Check **OnInvalidCheck** for unexpected invalidations
6. Ensure decorators call **child.Tick()** appropriately
7. For reactive trees: **IsReactive** set on composite (not just children)
8. For reactive trees: **OnInvalidCheck** returns false when valid
9. Watch for **OnEnter** called multiple times (expected in reactive trees)

---

This document reflects the ClosureBT implementation. For up-to-date behavior, refer to source code.
