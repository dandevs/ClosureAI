# ClosureBT Behavior Tree - Context for LLMs

Important: Read this entire file as it is important to know the whole as all features work in tandem with another.

## Table of Contents

1. [Overview](#overview)
2. [Architecture & Design Philosophy](#architecture--design-philosophy)
   - [Industry Context: The HFSMBTH Pattern](#industry-context-the-hfsmbth-pattern)
   - [The WHAT vs HOW Principle](#the-what-vs-how-principle)
   - [Cyclic vs Acyclic: A Key Structural Difference](#cyclic-vs-acyclic-a-key-structural-difference)
   - [Real-World Use Cases](#real-world-use-cases-from-remedys-experience)
   - [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
   - [Beyond HFSMBTH: Future Directions](#beyond-hfsmbth-future-directions)
3. [API Quick Reference](#api-quick-reference)
4. [How to Construct Trees](#how-to-construct-trees)
5. [Node Lifecycle](#node-lifecycle)
   - [Async Lifecycle Methods: Architectural Guidance](#async-lifecycle-methods-architectural-guidance)
6. [Key Concepts](#key-concepts)
7. [Use* Hooks (React-Style Reactive State)](#use-hooks-react-style-reactive-state)
8. [Common Patterns](#common-patterns)
9. [Useful Patterns](#useful-patterns)
10. [Important Notes for LLMs](#important-notes-for-llms)

## Overview

ClosureBT is a behavior tree library for Unity using declarative C# API to define AI behaviors.

### Key Distinguishing Features

1. **Detailed Lifecycle States**: Status (None/Running/Success/Failure) + SubStatus (None/Enabling/Entering/Running/Succeeding/Failing/Exiting/Disabling/Done)
2. **Async-First Design**: All lifecycle callbacks support UniTask async/await with cancellation tokens
3. **Reactive Invalidation**: Nodes can invalidate when conditions change, causing automatic re-entry with graceful cleanup
4. **Separated OnDisabled**: OnDisabled only fires during resets, NOT during normal tick flow
5. **Re-entry via allowReEnter**: Nodes can be re-entered (Done → Entering) without OnEnabled

## Architecture & Design Philosophy

### Industry Context: The HFSMBTH Pattern

ClosureBT's architecture is informed by the **Hierarchical Finite State Machine Behavior Tree Hybrid (HFSMBTH)**, a pattern used in AAA games like *Alan Wake 2* (2023) by Remedy Entertainment. Understanding why this hybrid emerged helps you use ClosureBT effectively.

#### The Problem with Pure Behavior Trees

Remedy's experience with pure behavior trees in *Control* revealed several pain points:

1. **Chaotic Control Flow**: Behavior trees evaluate top-down, left-to-right, allowing any branch to execute if conditions are met. But designers often wanted *constrained* flows—Idle should transition to Combat, which transitions to Searching, not Idle directly to Searching. They were essentially building state machines inside behavior trees without proper tooling.

2. **Cover System Nightmare**: Cover requires a rigid sequence: Find Cover → Enter Cover Animation → In Cover Actions → Exit Cover Animation. Pure behavior trees could "twitch" between branches mid-animation. Remedy had to write hacky C++ code to detect improper exits and inject cleanup—removing designer control entirely.

3. **Boss Phases**: A boss with "lights on" vs "lights off" phases shouldn't re-evaluate the lights-on phase once lights are off. But behavior trees always re-evaluate from the root, wasting cycles and risking incorrect transitions.

4. **Gigantic Unreadable Graphs**: Without state machine structure, behavior trees bloomed into massive, hard-to-debug graphs.

#### The Solution: Separation of Concerns

The HFSMBTH separates **control flow** (which state am I in?) from **behavioral execution** (how do I perform this state?):

| Concern | Tool | Responsibility |
|---------|------|----------------|
| **Control Flow** | Finite State Machine | "Where can I go from here?" |
| **Behavioral Execution** | Behavior Tree | "How do I do this task?" |

**Key insight from Remedy**: State machines in their HFSMBTH contain **NO execution logic**—they are purely state handling. States reference behavior tree graphs that do all the actual work. This is exactly how ClosureBT's `YieldSimple` is designed.

#### Alan Wake 2's Architecture

After implementing HFSMBTH, Alan Wake 2 achieved:

- **Combat States**: Simple, readable state graphs (Idle ↔ Moving ↔ Shooting ↔ Cover)
- **Boss Phases**: 3 phases as 3 states, with single-condition transitions
- **Segmented Behavior Trees**: Each state's BT was small and focused, not a massive monolith
- **Better Debugging**: Orange = active control nodes, Green = executing behavior. Clear hierarchy.

#### ClosureBT's Implementation

ClosureBT implements this pattern through `YieldSimple`:

```csharp
// YieldSimple = FSM (pure state selection)
// Returned nodes = BT (behavioral execution)
AI = YieldSimple("Combat FSM", () =>
{
    var state = Variable(() => 0);
    Node idleNode = null, shootNode = null, coverNode = null;

    return () => state.Value switch
    {
        0 => idleNode ??= IdleBehavior(() => OnTick(() => { if (EnemyVisible) state.Value = 1; })),
        1 => shootNode ??= ShootBehavior(() => OnTick(() => { if (NeedCover) state.Value = 2; })),
        2 => coverNode ??= CoverBehavior(() => OnExit(() => state.Value = 1)),
        _ => null
    };
});
```

**Benefits validated by Remedy:**
- **Readability**: State transitions are explicit and visible
- **Debuggability**: You can reason about "where can I go from here?"
- **Performance**: FSMs only evaluate outgoing transitions, not the entire tree
- **Designer Control**: No hacky runtime code needed for flow constraints

### The WHAT vs HOW Principle

When designing AI systems with ClosureBT, think in two layers:

| Layer | Pattern | Responsibility | Example |
|-------|---------|----------------|---------|
| **WHAT** | State Machine (`YieldSimple`) | Selects which state/behavior is active | "Am I in attack, flee, or patrol state?" |
| **HOW** | Behavior Tree (Nodes) | Implements ALL execution logic | "Move to target, wait for cooldown, deal damage" |

**Critical: State machines should contain NO execution logic.** They are purely responsible for:
- Selecting which state is currently active
- Defining state transitions (via lifecycle hooks)
- Holding state-level variables

**State Machines (pure state selection):**
- High-level state selection
- Mutually exclusive states
- State transition conditions
- "Which state am I in right now?"

**Behavior Trees (all execution):**
- ALL execution logic lives here
- Sequential/parallel execution of steps
- Conditional logic within an action
- Reactive re-evaluation of conditions
- "How do I accomplish this task?"

### Cyclic vs Acyclic: A Key Structural Difference

**State Machines are cyclic** - states can transition back to previous states, forming loops. This is ideal for ongoing behaviors where the AI naturally cycles between modes (Idle → Combat → Idle → Patrol → Idle...).

**Behavior Trees are acyclic (DAGs)** - execution flows from root to leaves without cycles. Each tick, the tree evaluates from the top down. This is ideal for task completion where you want clear start-to-finish execution.

| Aspect | State Machine (Cyclic) | Behavior Tree (Acyclic) |
|--------|------------------------|-------------------------|
| **Structure** | Graph with cycles | Directed Acyclic Graph (DAG) |
| **Flow** | Can loop back to previous states | Always flows root → leaves |
| **Best for** | Ongoing mode switching | Task completion, sequences |
| **Mental model** | "Where am I now?" | "What step am I on?" |

**Use State Machine when:**
- You need to return to previous states frequently
- States represent ongoing "modes" of behavior
- Transitions can happen in any direction

**Use Behavior Tree when:**
- You have a clear sequence of steps to complete
- Execution should flow forward (with possible reactive restarts)
- You want hierarchical decomposition of tasks

### Real-World Use Cases (from Remedy's Experience)

These examples are from *Alan Wake 2* illustrate when each approach shines:

#### Combat States → Use FSM

Combat typically involves mutually exclusive modes that cycle indefinitely:

```
Idle ↔ Moving & Shooting ↔ Standing & Shooting ↔ Taking Cover
```

- AI cycles between states based on tactical conditions
- Each state is a "mode" with its own behavior tree
- Transitions are constrained (can't jump from Idle directly to Taking Cover)

```csharp
// Combat state machine - pure state selection
AI = YieldSimple("Combat", () =>
{
    var state = Variable(() => 0);
    Node idleNode = null, moveShootNode = null, standShootNode = null, coverNode = null;

    return () => state.Value switch
    {
        0 => idleNode ??= IdleBehavior(() => OnTick(() => { if (EnemySpotted) state.Value = 1; })),
        1 => moveShootNode ??= MoveAndShoot(() => OnTick(() => { if (InPosition) state.Value = 2; if (TakingFire) state.Value = 3; })),
        2 => standShootNode ??= StandAndShoot(() => OnTick(() => { if (NeedReposition) state.Value = 1; if (TakingFire) state.Value = 3; })),
        3 => coverNode ??= TakeCover(() => OnExit(() => state.Value = 2)),
        _ => null
    };
});
```

#### Boss Phases → Use FSM

Boss encounters with distinct phases that don't revisit earlier phases:

```
Phase 1 (Lights On) → Phase 2 (Lights Off) → Phase 3 (Enraged)
```

- One-way transitions (no going back to Phase 1)
- Each phase has completely different behavior
- FSM prevents re-evaluating impossible phases

```csharp
AI = YieldSimple("Boss", () =>
{
    var phase = Variable(() => 1);
    Node phase1 = null, phase2 = null, phase3 = null;

    return () => phase.Value switch
    {
        1 => phase1 ??= Phase1Behavior(() => 
        {
            OnTick(() => 
            {
                if (LightsOff)
                    phase.Value = 2;
            });
        });

        2 => phase2 ??= Phase2Behavior(() => 
        {
            OnTick(() => 
            {
                if (HealthLow)
                    phase.Value = 3;
            });
        });

        3 => phase3 ??= Phase3Behavior(),  // Final phase, no transitions out

        _ => null
    };
});
```

#### Cover Mechanics → Use FSM with BT Sequences

Cover requires a rigid flow that pure BTs struggle with:

```
Find Cover → Move To Cover → Enter Cover Animation → In Cover → Exit Cover Animation → Continue
```

**The problem**: If a behavior tree "twitches" to a different branch while in cover, the exit animation never plays. Remedy had to write hacky C++ code to inject exit animations.

**The solution**: FSM enforces the flow, BT handles the execution:

```csharp
// Cover state machine - rigid flow control
CoverFSM = YieldSimple("Cover", () =>
{
    var state = Variable(() => 0);
    Node findNode = null, enterNode = null, inCoverNode = null, exitNode = null;

    return () => state.Value switch
    {
        // Must complete enter animation before doing anything in cover
        0 => findNode ??= FindCoverSpot(() => OnSuccess(() => state.Value = 1)),
        1 => enterNode ??= PlayEnterAnimation(() => OnSuccess(() => state.Value = 2)),
        2 => inCoverNode ??= InCoverBehavior(() => OnExit(() => state.Value = 3)),  // ANY exit goes to exit anim
        3 => exitNode ??= PlayExitAnimation(() => OnSuccess(() => state.Value = 0)),
        _ => null
    };
});
```

The FSM guarantees exit animation plays regardless of why we left cover.

#### Abilities / Attack Combos → Use BT Sequences

Linear sequences of actions that complete then return control:

```
Wind Up → Strike → Recovery → Done
```

- Clear start-to-finish flow
- Each step must complete before next
- No cycling back to earlier steps

```csharp
// Pure behavior tree - sequential execution
AttackCombo = Sequence("Melee Attack", () =>
{
    Do("Wind Up", () => PlayAnimation("WindUp"));
    Wait(0.3f);
    Do("Strike", () => {
        PlayAnimation("Strike");
        DealDamage(); 
    });
    Wait(0.2f);
    Do("Recovery", () => PlayAnimation("Recovery"));
    Wait(0.5f);
});
```

### Hierarchical State Machines

For complex AI, you can create **hierarchical state machines** by nesting `YieldSimple` nodes. Each sub-state machine manages its own set of states while the parent manages high-level mode switching.

**Important: State machines contain NO execution logic.** The state machine only:
1. Holds the state variable
2. Returns the appropriate behavior tree node for the current state
3. Defines state transitions via lifecycle hooks

All actual execution (movement, attacking, etc.) happens inside the behavior tree nodes.

Extract each sub-FSM into its own method for modularity:

```csharp
private void Awake() => AI = RootFSM();

// Root level: switches between high-level modes
// NOTE: No execution logic here - only state selection and transitions
private Node RootFSM() => YieldSimple("Root FSM", () =>
{
    var mode = Variable(() => 0);

    Node peacefulNode = null;
    Node combatNode = null;

    return () => mode.Value switch
    {
        // State machine ONLY selects which node to run
        // Transition logic is attached via lifecycle, but executes inside the BT node
        0 => peacefulNode ??= PeacefulFSM(() =>
        {
            // This OnTick runs inside the BT node, not the state machine
            OnTick(() =>
            {
                if (Pawn.Sight.VisibleEntities.Count > 0)
                    mode.Value = 1;
            });
        }),

        1 => combatNode ??= CombatFSM(() =>
        {
            OnTick(() =>
            {
                if (Pawn.Sight.VisibleEntities.Count == 0)
                    mode.Value = 0;
            });
        }),

        _ => null
    };
});

// Sub-FSM: Peaceful mode with Idle, Patrol, Gather states
private Node PeacefulFSM(Action lifecycle = null) => YieldSimple("Peaceful FSM", () =>
{
    var state = Variable(() => 0);

    Node idleNode = null;
    Node patrolNode = null;
    Node gatherNode = null;

    lifecycle?.Invoke();

    return () => state.Value switch
    {
        0 => idleNode ??= IdleBehavior(() =>
        {
            // Transition conditions - but no execution logic here
            OnTick(() =>
            {
                if (ShouldPatrol()) state.Value = 1;
                if (ResourceNearby()) state.Value = 2;
            });
        }),

        1 => patrolNode ??= PatrolBehavior(() =>
        {
            OnExit(() => state.Value = 0);
        }),

        2 => gatherNode ??= GatherBehavior(() =>
        {
            OnExit(() => state.Value = 0);
        }),

        _ => null
    };
});

// Sub-FSM: Combat mode with Engage, Flee states
private Node CombatFSM(Action lifecycle = null) => YieldSimple("Combat FSM", () =>
{
    var state = Variable(() => 0);

    Node engageNode = null;
    Node fleeNode = null;

    lifecycle?.Invoke();

    return () => state.Value switch
    {
        0 => engageNode ??= EngageBehavior(() =>
        {
            OnTick(() =>
            {
                if (Health < 20) state.Value = 1;
            });
        }),

        1 => fleeNode ??= FleeBehavior(() =>
        {
            OnExit(() => state.Value = 0);
        }),

        _ => null
    };
});
```

**Benefits of hierarchical state machines:**
- **Encapsulation** - Each sub-FSM manages its own complexity
- **Reusability** - Sub-FSMs can be extracted into methods and reused
- **Clarity** - High-level modes are separate from low-level state details
- **Scalability** - Add new sub-states without affecting sibling state machines
- **Testability** - Each FSM method can be tested independently
- **Pure state handling** - State machines only select states; all logic lives in BT nodes

### Combining State Machines and Behavior Trees

The recommended architecture uses `YieldSimple` as the top-level state machine, with each state returning a behavior tree node.

**The state machine is a pure state selector:**
- It holds the state variable and cached node references
- The switch expression maps state values to behavior tree nodes
- ALL execution logic (movement, combat, conditions) lives in the BT nodes
- State transitions are defined via lifecycle hooks that execute inside the BT nodes

```csharp
private void Awake() => AI = YieldSimple(() =>
{
    var state = Variable(() => 0);  // State machine holds state

    Node idleNode = null;           // State machine caches nodes
    Node attackNode = null;

    // The switch is pure state selection - no execution logic
    return () => state.Value switch
    {
        // Each case returns a BT node - all execution happens inside that node
        0 => idleNode ??= IdleBehavior(() =>
        {
            // This OnTick executes inside IdleBehavior, not in the state machine
            OnTick(() =>
            {
                if (CanSeeEnemy())
                    state.Value = 1;  // Transition happens, but check runs in BT
            });
        }),

        1 => attackNode ??= AttackBehavior(() =>
        {
            OnExit(() => state.Value = 0);  // Transition on BT completion
        }),

        _ => null
    };
});
```

**YieldSimple also supports lifecycle callbacks in its setup lambda**, which fire when the state machine itself enters/exits. This is useful for initialization and cleanup that applies across all states:

```csharp
private void Awake() => AI = YieldSimple("Enemy AI", () =>
{
    var state = Variable(() => 0);
    var currentTarget = Variable<GameEntity>(() => null);

    Node idleNode = null;
    Node attackNode = null;

    // These fire when the YieldSimple node itself enters/exits
    OnEnter(() =>
    {
        // Initialize shared resources, register event handlers, etc.
    });

    OnExit(() =>
    {
        // Cleanup shared resources, unregister event handlers, etc.
    });

    return () => state.Value switch
    {
        0 => idleNode ??= /* ... */,
        1 => attackNode ??= /* ... */,
        _ => null
    };
});
```

This pattern is especially useful when:
- Multiple states share common initialization (e.g., registering for events)
- Cleanup must happen regardless of which state was active when the AI stops
- You need to track state machine-level variables that persist across state changes

### Lifecycle-Driven State Transitions

**Lifecycle methods are the key to clean state transitions.** Every node can accept a lifecycle lambda, making it easy to attach state transition logic:

```csharp
// The lifecycle parameter pattern
protected virtual Node AttackState(Action lifecycle = null) => Sequence("Attack", () =>
{
    // ... behavior implementation ...
    
    lifecycle?.Invoke();  // Allow caller to attach state transitions
});

// Usage - attach state transition via lifecycle
1 => attackNode ??= AttackState(() =>
{
    OnExit(() => state.Value = 0);  // Transition back to idle when attack ends
}),
```

**Common lifecycle patterns for state transitions:**
- **`OnTick(() => state.Value = X)`** - Transition based on continuous condition checks
- **`OnExit(() => state.Value = X)`** - Transition when the behavior completes (success or failure)
- **`OnSuccess(() => state.Value = X)`** - Transition only on successful completion
- **`OnFailure(() => state.Value = X)`** - Transition only on failure

### Global vs Local State Transitions

State transitions can be defined at two levels: **globally** (in the YieldSimple setup) or **locally** (within individual state nodes). Each approach has trade-offs:

**Local transitions** (inside each state node):
```csharp
2 => attackNode ??= Reactive * Sequence("Attack", () =>
{
    Condition("Target Alive", () => currentTarget.Value && currentTarget.Value.IsAlive);
    Attack(() => currentTarget.Value);

    // Transition logic is localized to this state
    OnExit(() =>
    {
        currentTarget.Value = null;
        state.Value = 0;
    });
}),
```

**Global transitions** (in YieldSimple's setup, outside the switch):
```csharp
private void Awake() => AI = YieldSimple("Enemy AI", () =>
{
    var state = Variable(() => 0);
    var currentTarget = Variable<GameEntity>(() => null);

    Node idleNode = null;
    Node attackNode = null;

    // Global OnTick - runs every tick regardless of current state
    OnTick(() =>
    {
        // Interrupt ANY state if we see a high-priority target
        if (Pawn.Sight.VisibleEntities.Any(e => e.Priority == Priority.High))
        {
            currentTarget.Value = Pawn.Sight.VisibleEntities.First(e => e.Priority == Priority.High);
            state.Value = 2;  // Force attack state
        }
    });

    return () => state.Value switch
    {
        0 => idleNode ??= /* ... */,
        1 => moveToNexusNode ??= /* ... */,
        2 => attackNode ??= /* ... */,
        _ => null
    };
});
```

**When to use local transitions:**
- Transition logic is specific to one state (e.g., "when attack finishes, go idle")
- State has clear completion conditions defined by its behavior tree
- You want transitions co-located with the behavior they affect

**When to use global transitions:**
- Transition can occur from multiple/any state (e.g., "flee if health low")
- You need to check conditions that span across states
- Priority-based interrupts that override normal state flow
- Centralized control over state machine behavior is preferred

**You can combine both approaches** for maximum flexibility:
```csharp
OnTick(() =>
{
    // Global: high-priority interrupts
    if (health < 20) state.Value = 3;  // Flee from any state
});

return () => state.Value switch
{
    0 => idleNode ??= JustRunning(() =>
    {
        OnTick(() =>
        {
            // Local: state-specific transitions
            if (Pawn.Sight.VisibleEntities.Count > 0)
                state.Value = 2;
        });
    }),
    // ...
};
```

### Modular Node Design

**Split behaviors into reusable, parameterized nodes.** Each node should do one thing well and accept `Func<T>` parameters for dynamic data:

```csharp
// BAD: Monolithic, hard to reuse
1 => attackNode ??= Sequence(() =>
{
    var target = Pawn.Sight.VisibleEntities[0];  // Hardcoded target source
    Pawn.MoveTo(() => target.transform.position);
    // ... 50 lines of attack logic ...
});

// GOOD: Modular, reusable components
1 => attackNode ??= AttackState(() =>
{
    OnExit(() => state.Value = 0);
}),

protected virtual Node AttackState(Action lifecycle = null) => Reactive * Sequence("Attack State", () =>
{
    var target = Variable<GameEntity>(() => null);

    OnTick(() =>
    {
        if (Pawn.Sight.VisibleEntities.Count > 0)
            target.Value = Pawn.Sight.VisibleEntities[0];
    });

    Condition("Has Targets", () => Pawn.Sight.VisibleEntities.Count > 0);

    Sequence(() =>
    {
        WaitUntil("Target Not Null", () => target.Value);
        Attack(() => target.Value);  // Delegate to specific attack behavior
    });

    lifecycle?.Invoke();
});

protected virtual Node Attack(Func<GameEntity> getTarget, Action lifecycle = null) => Reactive * Sequence("Attack", () =>
{
    var target = UseEveryTick(getTarget);  // Fresh target every tick
    Condition(() => target.Value);

    Pawn.MoveTo(() => (target.Value.transform.position, 2f));

    D.While(() => target.Value);
    Sequence(() =>
    {
        D.Until(Status.Success);
        Cooldown(1f);

        Do(() =>
        {
            target.Value.Health -= 5;
        });
    });
    
    lifecycle?.Invoke();
});
```

**Benefits of modular design:**
- **Reusability** - `Attack()` can be called from any state with any target source
- **Testability** - Each node can be tested in isolation
- **Overridability** - Use `protected virtual` for inheritance-based customization
- **Composability** - Build complex behaviors from simple building blocks
- **Clarity** - Each method has a single responsibility

### Anti-Patterns to Avoid

These mistakes, identified through Remedy's production experience, lead to unmaintainable AI:

#### ❌ Building FSMs Inside Pure BTs

**Problem**: Using behavior tree structure to enforce state machine logic.

```csharp
// BAD: Trying to enforce Idle → Combat → Search flow with BT structure
AI = Selector(() =>
{
    D.Condition(() => state == "idle" && !enemyVisible);
    IdleBehavior();

    D.Condition(() => state == "combat" && enemyVisible);
    CombatBehavior();

    D.Condition(() => state == "search" && !enemyVisible && wasInCombat);
    SearchBehavior();
});
```

The conditions become complex, the flow is hard to trace, and any state can potentially reach any other state. Use `YieldSimple` instead.

#### ❌ Gigantic Monolithic Trees

**Problem**: All AI logic in one massive tree with no state separation.

```csharp
// BAD: Everything crammed into one tree
AI = Selector("Do Everything", () =>
{
    // 200 lines of patrol logic
    // 300 lines of combat logic  
    // 150 lines of flee logic
    // 100 lines of idle logic
    // ... impossible to debug or modify
});
```

Split into separate state-specific behavior trees, orchestrated by a state machine.

#### ❌ Execution Logic in State Machines

**Problem**: Putting behavioral code in the state selection logic.

```csharp
// BAD: State machine doing execution work
return () => {
    if (enemyVisible)
    {
        transform.LookAt(enemy);     // ❌ Execution logic
        weapon.Aim();                 // ❌ Execution logic
        return 1;                     // state selection
    }
    return 0;
};
```

State machines should ONLY return state values. All execution goes in the behavior tree nodes.

#### ❌ Ignoring Animation/State Coupling

**Problem**: Behavior tree can interrupt at any point, breaking animation sequences.

```csharp
// BAD: Cover behavior without enforced entry/exit
AI = Selector(() =>
{
    D.Condition(() => needsCover);
    Sequence(() =>
    {
        PlayAnimation("EnterCover");
        ShootFromCover();
        // If BT switches here, exit animation never plays!
    });

    D.Condition(() => shouldMove);
    MoveBehavior();  // Can interrupt mid-cover
});
```

Use FSM to enforce Enter → InState → Exit flow for any stateful behavior.

#### ❌ Putting Transition Conditions in Wrong Place

**Problem from Alan Wake 2**: Designers put phase transition conditions inside behavior trees instead of FSM transitions.

```csharp
// BAD: Phase transition buried in BT logic
Phase1Behavior = Sequence(() =>
{
    DoPhase1Stuff();
    Do(() => { if (lightsOff) phase = 2; });  // ❌ Transition hidden in BT
});

// GOOD: Transition visible in FSM
return () => phase switch
{
    1 => phase1Node ??= Phase1Behavior(() => OnTick(() => { if (lightsOff) phase = 2; })),
    // Transition is visible and co-located with state definition
};
```

Keep transitions in lifecycle hooks attached to the FSM, not buried in BT execution.

### Planning a System: Step-by-Step

When asked to create an AI system, follow this process:

1. **Identify the high-level states (WHAT)**
   - What are the mutually exclusive modes? (Idle, Combat, Flee, Patrol)
   - What conditions trigger transitions between states?

2. **Design each state's behavior tree (HOW)**
   - What steps are needed to accomplish this state's goal?
   - What conditions should interrupt or complete the behavior?

3. **Define modular, reusable nodes**
   - What actions are shared across states? (MoveTo, Attack, Wait)
   - What parameters do they need? (target, position, duration)

4. **Wire up state transitions via lifecycle**
   - Use `OnTick` for condition-based transitions
   - Use `OnExit` for completion-based transitions
   - Use `OnSuccess`/`OnFailure` for outcome-specific transitions

### Goal-Oriented Decomposition (Think Right-to-Left)

**Don't think forward (left-to-right), think backward from the goal (right-to-left).** Similar to HTN (Hierarchical Task Network) planners, start with what you want to achieve and decompose it into smaller sub-goals until you reach primitive actions.

```
High-Level Goal → Sub-Goals → ... → Primitive Actions
   (abstract)                           (concrete)
```

**Example: "Acquire Item" decomposition**

Start with the goal: "I need to have this item"

```
AcquireItem(itemID)
├── Already have it? → Success (base case)
├── Exists in world? → CollectItem(itemID)
│                       ├── MoveTo(itemPosition)
│                       └── PickUp(item)
└── Can craft it? → CraftItem(itemID)
                    ├── For each ingredient:
                    │   └── AcquireItem(ingredientID)  ← RECURSION!
                    └── PerformCrafting(recipe)
```

**In code:**

```csharp
// HIGH-LEVEL: What do we want? To have an item.
public Node AcquireItem(Func<string> getItemID) => Selector("Acquire", () =>
{
    var itemID = Variable(getItemID);

    // Base case: already have it
    Condition("Have", () => Inventory.Contains(itemID.Value));

    // Option 1: collect from world
    D.Condition("In World", () => ItemExists(itemID.Value));
    YieldSimpleCached(() => CollectItem(getItemID));

    // Option 2: craft it (which may recursively acquire ingredients)
    D.Condition("Craftable", () => IsCraftable(itemID.Value));
    YieldSimpleCached(() => CraftItem(getItemID));
});

// MID-LEVEL: How do we craft? Get ingredients, then craft.
private Node CraftItem(Func<string> getItemID) => Sequence("Craft", () =>
{
    var recipe = Variable(() => GetRecipe(getItemID()));

    // Recursively acquire each ingredient
    D.ForEach(() => recipe.Value.RequiredItems, out var requiredItemID);
    YieldSimpleCached(() => AcquireItem(requiredItemID));  // ← Recursive!

    // Then perform the crafting
    Wait(() => recipe.Value.CraftTime);
});

// LOW-LEVEL: How do we collect? Move there, pick it up.
private Node CollectItem(Func<string> getItemID) => Sequence("Collect", () =>
{
    var item = Variable(() => FindItemInWorld(getItemID()));
    
    MoveTo(() => item.Value.transform.position);
    PickUp(() => item.Value);
});

// PRIMITIVE: Actual movement
private Node MoveTo(Func<Vector3> getPosition) => Leaf("MoveTo", () =>
{
    OnBaseTick(() =>
    {
        var pos = getPosition();
        agent.SetDestination(pos);
        return agent.remainingDistance < 0.5f ? Status.Success : Status.Running;
    });
});
```

**Benefits of goal-oriented decomposition:**
- **Natural recursion** - Sub-goals can require the same high-level goals (crafting needs acquiring)
- **Builds vocabulary** - Each decomposition level adds reusable "words" to your AI vocabulary
- **Self-documenting** - High-level nodes read like intentions: `AcquireItem`, `AttackEnemy`, `FleeToSafety`
- **Handles complexity** - Complex goals automatically break down into manageable pieces
- **Emergent behavior** - The AI "figures out" how to achieve goals based on available options

**The decomposition hierarchy:**

| Level | Examples | Characteristics |
|-------|----------|-----------------|
| **Goals** | `AcquireItem`, `DefeatEnemy`, `Survive` | Abstract, what we want |
| **Strategies** | `CraftItem`, `MeleeAttack`, `FleeToSafety` | How to achieve goals |
| **Tactics** | `CollectItem`, `ApproachTarget`, `FindCover` | Concrete steps |
| **Primitives** | `MoveTo`, `Wait`, `PlayAnimation` | Atomic actions |

**When designing, ask:**
1. What is the **goal**? (e.g., "Have sword")
2. What **strategies** achieve it? (Buy, Craft, Find, Steal)
3. What **tactics** implement each strategy? (Go to shop, Pay gold, Take item)
4. What **primitives** exist? (MoveTo, Interact, Wait)

### Async vs Node Decomposition (When to Use UniTask)

ClosureBT supports **async/await via UniTask** in all lifecycle callbacks. This creates an important architectural choice: when should you use async code inside a single node versus decomposing into multiple nodes?

**Use async inside a node when:**
- You have a **linear sequence of timed operations** that don't need individual reactive cancellation
- The operations are **tightly coupled** and don't make sense as reusable components
- You need **fine-grained control** over timing with `UniTask.WaitForSeconds`, `UniTask.Delay`, etc.
- You want to use **try-finally for guaranteed cleanup** regardless of cancellation
- The logic is **self-contained** and doesn't benefit from behavior tree visualization

**Use node decomposition when:**
- Each step should be **individually visible** in the node graph debugger
- Steps are **reusable** across different behaviors
- You need **reactive invalidation** at each step (conditions that can abort mid-behavior)
- Different steps might need different **decorators** (timeouts, conditions, loops)
- The behavior tree structure aids **understanding and debugging**

#### Example: Looting with Async

```csharp
// Using async - simpler when operations are tightly coupled and linear
public Node Loot(Func<Inventory> getInventory) => Sequence("Loot", () =>
{
    MoveTo(() => (getInventory().transform.position, 1f));
    
    Leaf("Loot Items", () =>
    {
        OnBaseTick(async (ct, tick) =>
        {
            var other = getInventory();

            foreach (var slot in other.Slots)
            {
                if (slot.Count == 0)
                    continue;

                await UniTask.WaitForSeconds(1f, cancellationToken: ct);

                if (Inventory.Add(slot.ItemID, slot.Count) <= 0)
                    break;  // Inventory full
            }

            return Status.Success;
        });
    });
});

// Over-decomposed alternative - more nodes than necessary
public Node LootOverDecomposed(Func<Inventory> getInventory) => Sequence("Loot", () =>
{
    MoveTo(() => (getInventory().transform.position, 1f));
    
    D.ForEach(() => getInventory().Slots, out var getSlot);
    Sequence("Loot Slot", () =>
    {
        Condition(() => getSlot().Count > 0);
        Wait(1f);
        Do(() => Inventory.Add(getSlot().ItemID, getSlot().Count));
    });
});
```

The async version is preferable here because:
1. The looting loop is self-contained logic
2. We can break early when inventory is full (harder with ForEach)
3. Individual slot transfers don't need reactive cancellation
4. No reuse need for "transfer single slot" as a standalone behavior

#### try-finally for Guaranteed Cleanup / Finalization

**Critical**: When async operations modify state that must be reverted on cancellation, use `try-finally`:
**Critical**: When an operation requires finalization regardless of result, use `try-finally`

```csharp
Leaf("Channel Ability", () =>
{
    OnBaseTick(async (ct, tick) =>
    {
        // Setup
        _isChanneling = true;
        _channelEffect = SpawnEffect();
        
        try
        {
            await UniTask.WaitForSeconds(3f, cancellationToken: ct);
            CastAbility();
            return Status.Success;
        }
        finally
        {
            // ALWAYS runs - even if cancelled!
            _isChanneling = false;
            if (_channelEffect != null)
                Destroy(_channelEffect);
        }
    });
});
```

**Why try-finally matters:**
- When a node is cancelled (via `ResetImmediately()`, reactive invalidation, or parent failure), the `CancellationToken` is triggered
- Without try-finally, cleanup code after `await` would never execute
- The finally block **always executes**, ensuring state is properly cleaned up
- This is especially important for: visual effects, audio, physics state, animation locks, resource handles

#### Async Timing Patterns

```csharp
// Sequential delays
await UniTask.WaitForSeconds(1f, cancellationToken: ct);
DoStep1();
await UniTask.WaitForSeconds(0.5f, cancellationToken: ct);
DoStep2();

// Polling with delay
while (!condition)
{
    ct.ThrowIfCancellationRequested();
    await UniTask.Yield(ct);  // Wait one frame
}

// Parallel async operations
await UniTask.WhenAll(
    FetchDataAsync(ct),
    PlayAnimationAsync(ct)
);

// Timeout wrapper
var completed = await UniTask.WaitForSeconds(5f, cancellationToken: ct)
    .TimeoutWithoutException(TimeSpan.FromSeconds(10));
```

#### Decision Matrix

| Scenario | Prefer Async | Prefer Nodes |
|----------|--------------|--------------|
| 3-step timed sequence | ✅ | |
| Each step needs conditions | | ✅ |
| Tight loop with early exit | ✅ | |
| Steps reused elsewhere | | ✅ |
| Complex cleanup on cancel | ✅ (try-finally) | |
| Need node graph visibility | | ✅ |
| Mixing with reactive conditions | | ✅ |
| Simple fire-and-forget delays | ✅ | |

**General guideline**: If you find yourself creating many small leaf nodes just for timing/sequencing within a single logical action, consider consolidating into one async leaf with try-finally for cleanup.

### Beyond HFSMBTH: Future Directions

The HFSMBTH pattern is powerful, but Remedy and others are exploring further hybridizations:

#### Utility Systems

Behavior trees are great for *executing* behavior, but selecting which behavior to execute often involves weighing multiple factors. **Utility AI** scores each option and picks the highest:

```csharp
// Conceptual: Utility-based state selection
AI = YieldSimple("Utility AI", () =>
{
    var actions = new[]
    {
        (Score: () => 1f - Health / MaxHealth, Node: () => HealBehavior()),      // Low health = high priority
        (Score: () => EnemiesNearby * 0.5f, Node: () => CombatBehavior()),       // More enemies = fight
        (Score: () => ResourcesVisible * 0.3f, Node: () => GatherBehavior()),    // Opportunistic gathering
    };

    return () => actions
        .OrderByDescending(a => a.Score())
        .First()
        .Node();
});
```

This could complement HFSMBTH by making state transitions more nuanced than simple boolean conditions.

#### Goal-Oriented Action Planning (GOAP)

Instead of manually defining transitions, GOAP plans backwards from goals. The AI has:
- **Goals**: Desired world states (e.g., "Enemy is dead")
- **Actions**: Things that change world state (e.g., "Attack reduces enemy health")
- **Planner**: Finds action sequence to reach goal

GOAP excels at emergent behavior but can be harder to predict/debug. It could be integrated as a planning layer above the FSM.

#### Hierarchical Task Networks (HTN)

Similar to goal-oriented decomposition (already used in ClosureBT), HTN formally decomposes abstract tasks into primitive operations. The key difference is HTN planners can *search* for valid decompositions, handling conditional branching automatically.

#### The Hybrid Future

Remedy's conclusion: the HFSMBTH was "worth it" but they're exploring:

> "We'll probably start looking into trying to create a hybrid where we can put in a utility system as a reference inside a finite State machine instead or together with a behavior tree so that our designers have more flexibility and have more options."

The pattern: **layer different decision-making approaches** where each excels:
- **FSM**: Enforces valid state transitions and control flow
- **BT**: Executes complex, hierarchical behavior sequences
- **Utility**: Scores and selects between competing options
- **GOAP/HTN**: Plans multi-step goal achievement

ClosureBT's `YieldSimple` and `YieldDynamic` provide the foundation for experimenting with these hybrids.

## API Quick Reference

One-liner descriptions for all available functions. Use `using static ClosureBT.BT;` to access these.

### Composite Nodes

| Function | Description |
|----------|-------------|
| `Sequence(name, setup)` | Runs children sequentially; fails on first failure, succeeds if all succeed |
| `SequenceAlways(name, setup)` | Runs all children sequentially regardless of failures; succeeds only if all succeed |
| `Selector(name, setup)` | Runs children sequentially; succeeds on first success, fails if all fail |
| `Parallel(name, setup)` | Runs all children simultaneously; succeeds when all complete |
| `Race(name, setup)` | Runs all children simultaneously; succeeds when any child succeeds first |

### Leaf Nodes

| Function | Description |
|----------|-------------|
| `Leaf(name, setup)` | Creates a custom leaf node with `OnBaseTick` returning Status |
| `Condition(name, condition)` | Returns Success if `condition()` is true, Failure if false; invalidates on change |
| `Do(name, action)` | Executes `action()` and immediately returns Success |
| `Wait(name, duration)` | Returns Running for `duration` seconds, then Success |
| `Wait(name, () => duration)` | Dynamic wait duration evaluated each tick |
| `WaitUntil(name, condition)` | Returns Running while `condition()` is false, Success when true |
| `WaitWhile(name, condition)` | Returns Running while `condition()` is true, Success when false |
| `Cooldown(name, duration)` | Returns Failure during cooldown, Success when ready (resets on success) |
| `JustRunning(name, setup)` | Always returns Running; never completes |
| `JustSuccess(name, setup)` | Always returns Success immediately |
| `JustFailure(name, setup)` | Always returns Failure immediately |
| `JustOnTick(name, onTick)` | Executes `onTick()` every tick, always returns Running |

### Yield / Dynamic Nodes

| Function | Description |
|----------|-------------|
| `YieldSimple(name, setup)` | State machine pattern; `setup` returns `Func<Node>` called every tick |
| `YieldSimpleCached(getNode)` | Caches and reuses a single node from `getNode()`; ideal for recursion |
| `YieldDynamic(name, setup)` | Full control yield with `YieldController` for reset policies and switching |

### Decorators (D.*)

Decorators modify the **next node** created. Place `D.*()` before the node it decorates.

| Function | Description |
|----------|-------------|
| `D.Condition(condition)` | Only runs child while `condition()` is true; fails when false |
| `D.ConditionLatch(condition)` | Latches when condition becomes true; child runs to completion even if condition turns false |
| `D.Invert()` | Flips child's Success → Failure and Failure → Success |
| `D.AlwaysSucceed()` | Forces Success regardless of child's actual status |
| `D.AlwaysFail()` | Forces Failure regardless of child's actual status |
| `D.Until(status)` | Repeats child until it returns the specified `status` (Success or Failure) |
| `D.Until(condition)` | Repeats child until `condition()` becomes true |
| `D.While(condition)` | Runs child while `condition()` is true; returns Failure when false |
| `D.Repeat()` | Infinite loop; child resets and reruns each time it completes |
| `D.RepeatCount(amount)` | Repeats child `amount` times, then returns Success |
| `D.ForEach(getList, out getCurrent)` | Iterates collection; runs child for each item; `getCurrent()` returns current |
| `D.Timeout(duration)` | Fails if child doesn't complete within `duration` seconds |
| `D.Cooldown(duration)` | Returns Failure during cooldown; ticks child when cooldown passed |
| `D.Latched()` | Blocks child's invalidation signals from propagating to parent |
| `D.ValueChanged(getValue)` | Invalidates when `getValue()` returns a different value |
| `D.ResetOnEnter()` | Resets child to initial state on both entry and exit |
| `D.MustCompleteFirst()` | Ensures child runs to completion before allowing exit |

### Lifecycle Callbacks

Use inside node setup lambdas to hook into the node lifecycle. All lifecycle methods support three overloads: sync, async, and async with Tick Core.

| Function | Overloads | Description |
|----------|-----------|-------------|
| `OnEnabled(action)` | sync, async, tick-core | Called once when node first activates (before OnEnter) |
| `OnEnter(action)` | sync, async, tick-core | Called each time node enters (including re-entry) |
| `OnPreTick(action)` | sync, tick-core | Called every tick before OnBaseTick |
| `OnBaseTick(func)` | sync, tick-core | Main tick logic; return Status (Running/Success/Failure) |
| `OnTick(action)` | sync, tick-core | Called every tick after OnBaseTick |
| `OnSuccess(action)` | sync, async, tick-core | Called when node completes with Success |
| `OnFailure(action)` | sync, async, tick-core | Called when node completes with Failure |
| `OnExit(action)` | sync, async, tick-core | Called when node exits (success or failure) |
| `OnDisabled(action)` | sync, async, tick-core | Called only during Reset (not normal tick flow) |
| `OnInvalidCheck(func)` | sync | Return true to signal invalidation to reactive parents |

**Overload Signatures:**
- **Sync**: `Action` - simple synchronous callback
- **Async**: `Func<CancellationToken, UniTask>` - awaitable operations without tick-based synchronization
- **Tick Core**: `Func<CancellationToken, Func<UniTask>, UniTask>` - async with `await tick()` for tick-synchronized control

### Variables

| Function | Description |
|----------|-------------|
| `Variable<T>()` | Creates an uninitialized variable |
| `Variable(() => init)` | Creates a variable initialized during Enabling phase |
| `Variable(displayValue, () => init)` | Creates a variable with editor display value |

### Use* Methods (FRP-Style Reactive Helpers)

Use inside node setup lambdas to create reactive data pipelines.

| Function | Description |
|----------|-------------|
| `UseEveryTick(source)` | Returns variable updated from `source` every tick |
| `UseTimeElapsed()` | Returns variable tracking seconds elapsed since node started |
| `UseTicksElapsed()` | Returns variable counting ticks since node started |
| `UseDebounce(delay, source)` | Propagates value only after `source` stops changing for `delay` seconds |
| `UseThrottle(delay, source)` | Propagates value at most once per `delay` seconds |

| `UseDistinctUntilChanged(source)` | Only propagates when value actually changes |
| `UseValueDidChange(source)` | Returns true for one frame when `source` signals a change |
| `UseWhere(source, predicate)` | Filters values; only propagates when `predicate(value)` is true |
| `UseSelect(source, selector)` | Transforms values via `selector(value)` |
| `UseScan(source, seed, accumulator)` | Accumulates values over time like Array.Reduce |
| `UseRollingBuffer(count, waitFill, source)` | Maintains a list of last `count` values |
| `UseWindowTime(duration, source)` | Maintains list of values from last `duration` seconds |
| `UseCountChanged(source)` | Counts how many times `source` has signaled |
| `UseTimePredicateElapsed(predicate)` | Tracks time elapsed since `predicate(elapsed)` became true |
| `UsePipe(var, ...transforms)` | Chains multiple Use* transformations together |

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

### Async Lifecycle Methods: Architectural Guidance

ClosureBT lifecycle methods support three overload patterns, each suited for different use cases:

#### Overload Types

| Pattern | Signature | Purpose | Use Case |
|---------|-----------|---------|----------|
| **Sync** | `Action` | Synchronous execution | Simple state changes, logging, variable updates |
| **Async** | `Func<CancellationToken, UniTask>` | Awaitable operations | Asset loading, I/O, network requests, async setup |
| **Tick Core** | `Func<CancellationToken, Func<UniTask>, UniTask>` | Tick-synchronized async | Multi-frame sequences, frame-based control, animations |

#### When to Use Each Pattern

**Sync (`Action`)** - The default for most cases:
```csharp
OnEnter(() => _startTime = Time.time);
OnTick(() => UpdateSensors());
OnSuccess(() => score += 100);
OnExit(() => weapon.EndAttack());
```

Use when:
- Setting variables or state
- Calling synchronous Unity APIs
- Logging or debug output
- Quick calculations

**Async (`Func<CancellationToken, UniTask>`)** - For awaitable operations:
```csharp
OnEnabled(async ct =>
{
    await LoadResourcesAsync(ct);
    await InitializeSystemAsync(ct);
});

OnExit(async ct => await SaveStateAsync(ct));
```

Use when:
- Loading assets or resources
- Network requests
- File I/O operations
- Any awaitable operation without needing tick-based frame control

**Tick Core (`Func<CancellationToken, Func<UniTask>, UniTask>`)** - For tick-synchronized async operations:
```csharp
OnEnter(async (ct, tick) =>
{
    PlayStartAnimation();
    await tick(); // Wait for next tree tick
    await tick(); // Wait for another tick
    await tick(); // And another
    StartMainLogic();
});

OnBaseTick(async (ct, tick) =>
{
    MoveTowards(target);
    await tick();
    if (ReachedTarget())
        return Status.Success;
    return Status.Running;
});
```

Use when:
- You need to synchronize with the behavior tree tick cycle
- Playing animations that should align with game ticks
- Phased sequences where each phase should span one frame
- Main logic with frame-by-frame control via `await tick()`

#### CancellationToken Usage

The `CancellationToken` is cancelled when:
- The node is reset via `ResetImmediately()` or `ResetGracefully()`
- A parent node forces the child to stop
- The behavior tree is destroyed

Always pass `ct` to async operations to support graceful cancellation:
```csharp
OnEnter(async ct =>
{
    try
    {
        await LongRunningOperation(ct);
    }
    catch (OperationCanceledException)
    {
        // Clean up if cancelled
        CleanupPartialState();
    }
});
```

#### Tick Core Pattern Details

The `tick` function (second parameter) returns a `UniTask` that completes on the next behavior tree tick. This ties your async code to the tree's tick cycle:

```csharp
OnBaseTick(async (ct, tick) =>
{
    // This runs on tick 1
    _progress = 0f;
    
    while (_progress < 1f)
    {
        _progress += Time.deltaTime;
        await tick(); // Suspend until next tick
    }
    
    // This runs after _progress reaches 1.0
    return Status.Success;
});
```

**Key insight**: Each `await tick()` suspends execution until the next `Tick()` call on the tree. The node returns `Status.Running` implicitly while suspended.

#### Lifecycle Method Availability

| Lifecycle | Sync | Async | Tick Core |
|-----------|:----:|:-----:|:---------:|
| `OnEnabled` | ✅ | ✅ | ✅ |
| `OnEnter` | ✅ | ✅ | ✅ |
| `OnPreTick` | ✅ | ❌ | ✅ |
| `OnBaseTick` | ✅ | ❌ | ✅ |
| `OnTick` | ✅ | ❌ | ✅ |
| `OnSuccess` | ✅ | ✅ | ✅ |
| `OnFailure` | ✅ | ✅ | ✅ |
| `OnExit` | ✅ | ✅ | ✅ |
| `OnDisabled` | ✅ | ✅ | ✅ |

**Note**: `OnPreTick`, `OnBaseTick`, and `OnTick` don't have simple async overloads (only sync and Tick Core). Use Tick Core if you need awaitable operations that integrate with tick-based control in these callbacks.

#### Best Practices

1. **Prefer sync for simple operations** - Don't use async just because you can
2. **Use async for true async operations** - Loading, I/O, network calls
3. **Use Tick Core for frame-spanning sequences** - Animations, gradual effects
4. **Always handle cancellation** - The token can be cancelled at any time
5. **Don't block in async callbacks** - Use `await`, not `.Result` or `.Wait()`

## Key Concepts

### Lifecycle Callbacks

Most node creators accept an optional `lifecycle` parameter. All lifecycle methods support multiple overloads for different execution patterns:

**Sync Overload** - Simple synchronous callback:
```csharp
OnEnter(() => Debug.Log("Started"));
OnSuccess(() => CollectItem());
```

**Async Overload** - For awaitable operations:
```csharp
OnEnter(async ct =>
{
    await PlayEnterAnimationAsync(ct);
    await InitializeAsync(ct);
});

OnExit(async ct => await Cleanup(ct));
```

**Tick Core Overload** - For tick-synchronized async operations using `await tick()`:
```csharp
OnEnter(async (ct, tick) =>
{
    StartEnterSequence();
    await tick(); // Wait for next tree tick
    ContinueEnterSequence();
    await tick(); // Wait for another tick
    FinalizeEntry();
});
```

Available callbacks:
- `OnEnabled()` - First activation (once per Active session) - sync, async, tick-core
- `OnEnter()` - Each entry (including re-entry) - sync, async, tick-core
- `OnPreTick()` - Called every tick before OnBaseTick - sync, tick-core
- `OnBaseTick()` - Main logic returning Status - sync, tick-core
- `OnTick()` - Called every tick after OnBaseTick - sync, tick-core
- `OnSuccess()` / `OnFailure()` - On completion - sync, async, tick-core
- `OnExit()` - Cleanup - sync, async, tick-core
- `OnDisabled()` - Only on reset - sync, async, tick-core

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
        OnBaseTick(() => MoveLogic());
        OnSuccess(() => _finalPosition = transform.position);
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

## Use* Hooks (React-Style Reactive State)

The `Use*` methods are inspired by **React Hooks** and provide a declarative way to manage reactive state and derived data within nodes. They enable FRP (Functional Reactive Programming) style data pipelines that automatically update as values change.

### Core Philosophy

Hooks follow the React pattern of **composable, declarative state management**:
- **Encapsulated Logic** - Each hook handles its own state and lifecycle
- **Composable** - Hooks can be chained together via `UsePipe` or nested calls
- **Declarative** - Describe *what* you want, not *how* to compute it imperatively
- **Automatic Cleanup** - Hooks tie into node lifecycle automatically

### When to Use Hooks

| Scenario | Use Hook? | Why |
|----------|-----------|-----|
| Transform/derive data from source | ✅ Yes | Use `UseSelect`, `UseWhere`, `UseScan` |
| Track time or tick counts | ✅ Yes | Use `UseTimeElapsed`, `UseTicksElapsed` |
| Debounce/throttle rapid changes | ✅ Yes | Use `UseDebounce`, `UseThrottle` |
| Detect value changes | ✅ Yes | Use `UseValueDidChange`, `UseDistinctUntilChanged` |
| Buffer recent values | ✅ Yes | Use `UseRollingBuffer`, `UseWindowTime` |
| Simple one-off value storage | ❌ No | Use `Variable` directly |
| Complex stateful logic | ❌ No | Use `OnTick` with manual state |

### Naming Convention

**Critical**: Internal/intermediate variables should use underscore prefix (`_`). Only the returned `VariableType<T>` should be unprefixed:

```csharp
// ✅ CORRECT - returned variable is unprefixed, internal state is prefixed
public VariableType<List<Unit>> UseSelectManyUnitsViaMouseDrag(Func<bool> predicate)
{
    var selectedUnits = Variable(new List<Unit>());  // Returned value - no underscore
    var _lmbDrag = UseMouseDragDifference(predicate);  // Internal - underscore prefix

    OnTick(() =>
    {
        if (predicate())
        {
            selectedUnits.Value.Clear();
            // ... selection logic using _lmbDrag ...
        }
    });

    return selectedUnits;
}

// ❌ WRONG - missing underscore on internal variable
var lmbDrag = UseMouseDragDifference(predicate);  // Should be _lmbDrag
```

### Anatomy of a Hook

Every `Use*` hook follows this structure:

```csharp
public static VariableType<TResult> UseMyHook<TSource, TResult>(/* parameters */)
{
    // 1. Create the returned variable (no underscore)
    var result = Variable<TResult>();

    // 2. Create internal state variables (underscore prefix)
    var _internalState = Variable<SomeType>();

    // 3. Subscribe to source signals if applicable
    source.OnSignal += value =>
    {
        // React to source changes
        result.Value = Transform(value);
    };

    // 4. Hook into lifecycle if needed (OnPreTick, OnEnter, etc.)
    OnPreTick(() =>
    {
        // Per-tick processing
    });

    // 5. Return the result variable
    return result;
}
```

### Built-in Hooks Reference

#### Data Source Hooks

**`UseEveryTick(source)`** - Re-evaluates source every tick

Use when you need a `Func<T>` to be evaluated fresh every tick and accessible as a `VariableType<T>`.

```csharp
// From a Func<T> - evaluates every tick
var target = UseEveryTick(() => Pawn.Sight.VisibleEntities.FirstOrDefault());

// From a VariableType<T> - copies value every tick
var targetCopy = UseEveryTick(someVariable);

// Real example from Enemy.cs
protected virtual Node Attack(Func<GameEntity> getTarget, Action lifecycle = null) => Reactive * Sequence("Attack", () =>
{
    var target = UseEveryTick(getTarget);  // Fresh target reference every tick
    Condition(() => target.Value);

    Pawn.MoveTo(() => (target.Value.transform.position, 2f));
    // ...
});
```

#### Time Tracking Hooks

**`UseTimeElapsed()`** - Tracks seconds since node started

```csharp
var elapsed = UseTimeElapsed();

OnTick(() =>
{
    Debug.Log($"Running for {elapsed.Value} seconds");
    
    if (elapsed.Value > 5f)
        state.Value = 1;  // Transition after 5 seconds
});
```

**`UseTicksElapsed()`** - Counts ticks since node started

```csharp
var ticks = UseTicksElapsed();

OnTick(() =>
{
    if (ticks.Value % 60 == 0)  // Every 60 ticks
        DoPeriodicCheck();
});
```

**`UseTimePredicateElapsed(predicate)`** - Tracks time while predicate is true

```csharp
// Track how long player has been in danger zone
var timeInDanger = UseTimePredicateElapsed(elapsed => playerInDangerZone);

OnTick(() =>
{
    if (timeInDanger.Value > 3f)
        TriggerWarning();
});
```

#### Rate Limiting Hooks

**`UseDebounce(delay, source)`** - Waits for value to settle before propagating

Use when you want to react only after rapid changes stop. The value only updates after `delay` seconds of no changes.

```csharp
var _rawInput = Variable(() => Input.mousePosition);
var debouncedInput = UseDebounce(0.3f, _rawInput);

OnTick(() =>
{
    _rawInput.Value = Input.mousePosition;
    // debouncedInput.Value only updates 0.3s after mouse stops moving
});
```

**`UseThrottle(delay, source)`** - Limits update frequency

Use when you want to sample at most once per `delay` seconds, regardless of how often source changes.

```csharp
var _position = UseEveryTick(() => transform.position);
var throttledPosition = UseThrottle(0.1f, _position);  // Max 10 updates/sec

OnTick(() =>
{
    // throttledPosition.Value updates at most every 0.1 seconds
    SendNetworkUpdate(throttledPosition.Value);
});
```

#### Change Detection Hooks

**`UseValueDidChange(source)`** - Returns true for one frame when source changes

This is one of the most useful hooks for detecting state transitions.

```csharp
// From Unit.cs - detect when destination changes to interrupt attack
var moveToPositionChanged = UseValueDidChange(UseEveryTick(() => Pawn.Destination));

OnTick(() =>
{
    if (moveToPositionChanged.Value)
        state.Value = 0;  // Player gave new command, return to idle
});
```

**`UseDistinctUntilChanged(source)`** - Filters out duplicate consecutive values

```csharp
var _rawState = UseEveryTick(() => CalculateState());
var state = UseDistinctUntilChanged(_rawState);

// state only signals when the value actually differs from previous
```

**`UseCountChanged(source)`** - Counts how many times source has signaled

```csharp
var _damage = Variable<float>();
var hitCount = UseCountChanged(_damage);

OnTick(() =>
{
    if (hitCount.Value >= 3)
        TriggerComboBonus();
});
```

#### Transformation Hooks

**`UseSelect(source, selector)`** - Transforms values (like LINQ Select)

```csharp
var _entity = UseEveryTick(() => Pawn.Sight.VisibleEntities.FirstOrDefault());
var targetPosition = UseSelect(_entity, e => e?.transform.position ?? Vector3.zero);

// targetPosition.Value is always a Vector3, even if entity is null
```

**`UseWhere(source, predicate)`** - Filters values (like LINQ Where)

```csharp
var _health = UseEveryTick(() => entity.Health);
var lowHealthAlerts = UseWhere(_health, h => h < 20f);

// lowHealthAlerts only updates when health drops below 20
```

**`UseScan(source, seed, accumulator)`** - Accumulates values over time (like Reduce)

```csharp
var _damageEvent = Variable<float>();
var totalDamage = UseScan(_damageEvent, 0f, (total, damage) => total + damage);

// Apply damage somewhere
_damageEvent.Value = 10f;
_damageEvent.Value = 5f;
// totalDamage.Value is now 15f
```

#### Buffer Hooks

**`UseRollingBuffer(count, waitToFill, source)`** - Maintains last N values

```csharp
var _position = UseEveryTick(() => transform.position);
var recentPositions = UseRollingBuffer(10, false, _position);

OnTick(() =>
{
    // recentPositions.Value is List<Vector3> of last 10 positions
    var averagePosition = recentPositions.Value.Aggregate(Vector3.zero, (a, b) => a + b) / recentPositions.Value.Count;
});
```

**`UseWindowTime(duration, source)`** - Maintains values from last N seconds

```csharp
var _damage = Variable<float>();
var recentDamage = UseWindowTime(3f, _damage);

OnTick(() =>
{
    // recentDamage.Value contains all damage events from last 3 seconds
    var dps = recentDamage.Value.Sum() / 3f;
});
```

#### Composition Hook

**`UsePipe(source, ...transforms)`** - Chains multiple transformations

```csharp
// Without UsePipe - nested and hard to read
var result = UseDistinctUntilChanged(UseThrottle(0.1f, UseSelect(source, x => x * 2)));

// With UsePipe - linear and readable
var result = UsePipe(
    source,
    s => UseSelect(s, x => x * 2),
    s => UseThrottle(0.1f, s),
    s => UseDistinctUntilChanged(s)
);
```

### Creating Custom Hooks

Custom hooks encapsulate reusable reactive logic. They follow the same pattern as built-in hooks.

#### Simple Example: Mouse Delta

```csharp
private VariableType<Vector3> UseMouseDelta()
{
    var delta = Variable(Vector3.zero);  // Returned - no underscore
    var _last = Variable(Vector3.zero);  // Internal - underscore

    OnPreTick(() =>
    {
        delta.Value = Input.mousePosition - _last.Value;
        _last.Value = Input.mousePosition;
    });

    return delta;
}

// Usage
var mouseDelta = UseMouseDelta();
OnTick(() => Debug.Log($"Mouse moved: {mouseDelta.Value}"));
```

#### Stateful Example: Mouse Drag

```csharp
private VariableType<(Vector3 Start, Vector3 End)> UseMouseDragDifference(Func<bool> predicate)
{
    var _start = Variable(Vector3.zero);
    var _state = Variable(onEnter: () => 0);  // Reset state on re-entry
    var values = Variable<(Vector3 Start, Vector3 End)>(() => (Vector3.zero, Vector3.zero));

    OnPreTick(() =>
    {
        var active = predicate();

        if (_state.Value == 0)
        {
            if (active)
            {
                _start.Value = Utils.GetPlaneCastMousePos();
                _state.Value = 1;
            }
        }

        if (_state.Value == 1)
        {
            if (active)
                values.Value = (_start.Value, Utils.GetPlaneCastMousePos());
            else
                _state.Value = 0;
        }
    });

    return values;
}
```

#### Composing Hooks

Hooks can call other hooks, building complex behavior from simple parts:

```csharp
public VariableType<List<Unit>> UseSelectManyUnitsViaMouseDrag(Func<bool> predicate)
{
    var selectedUnits = Variable(new List<Unit>());
    var _lmbDrag = UseMouseDragDifference(predicate);  // Compose with another hook

    OnTick(() =>
    {
        if (predicate())
        {
            selectedUnits.Value.Clear();
            var center = (_lmbDrag.Value.Start + _lmbDrag.Value.End) / 2f;
            var area = _lmbDrag.Value.End - _lmbDrag.Value.Start;
            
            // Use _lmbDrag to find units in selection box...
            var hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask);
            // ...populate selectedUnits
        }
    });

    return selectedUnits;
}
```

### Hook Best Practices

1. **Use OnPreTick for hooks** - Ensures values are updated before other OnTick callbacks read them
2. **Return only one variable** - A hook should have a single output; use tuples or classes for multiple values
3. **Prefix internal state with underscore** - Makes it clear what's internal vs. exposed
4. **Initialize with sensible defaults** - Use `Variable(() => defaultValue)` to ensure clean state
5. **Consider re-entry** - Use `Variable(onEnter: () => value)` if state should reset when node re-enters
6. **Chain with UsePipe for readability** - Avoid deeply nested hook calls
7. **Document signal behavior** - Note whether your hook signals on every tick or only on changes

### Hook vs Manual Implementation

**Use hooks when:**
- You need the same reactive pattern in multiple places
- The logic is self-contained and reusable
- You want automatic lifecycle integration
- You're building data transformation pipelines

**Use manual OnTick when:**
- Logic is unique to one node
- You need complex conditional logic
- Performance is critical (hooks have slight overhead)
- You need to coordinate multiple values atomically

## Common Patterns

### SequenceAlways vs Sequence

- **Sequence**: Stops on first failure, returns Failure
- **SequenceAlways**: Continues through all children regardless of failures, returns Success only if all succeeded

### Yield - Dynamic Node Insertion

**YieldSimpleCached** - Caches single node, reuse across ticks:

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

**YieldSimple** - Pure state selection with switch expression. The state machine contains NO execution logic - it only selects which BT node to run:

```csharp
YieldSimple("State Machine", () =>
{
    // State machine only holds state and cached nodes
    var state = Variable(() => 0);

    Node idleNode = null;
    Node combatNode = null;
    Node patrolNode = null;

    // Pure state selection - NO execution logic here
    return () => state.Value switch
    {
        // Each case returns a BT node that contains ALL execution logic
        0 => idleNode ??= IdleBehavior(() =>
        {
            // Transition conditions execute inside the BT node
            OnTick(() =>
            {
                if (CanSeeEnemy())
                    state.Value = 1;
            });
        }),

        1 => combatNode ??= CombatBehavior(() =>
        {
            OnExit(() => state.Value = 0);  // Transition on completion
        }),

        2 => patrolNode ??= PatrolBehavior(() =>
        {
            OnExit(() => state.Value = 0);
        }),

        _ => null
    };
});

// Behavior tree nodes contain ALL execution logic
private Node IdleBehavior(Action lifecycle = null) => JustRunning("Idle", () =>
{
    // Execution logic here
    lifecycle?.Invoke();
});

private Node CombatBehavior(Action lifecycle = null) => Sequence("Combat", () =>
{
    AttackTarget();  // Execution logic
    lifecycle?.Invoke();
});

private Node PatrolBehavior(Action lifecycle = null) => Sequence("Patrol", () =>
{
    PatrolArea();  // Execution logic
    lifecycle?.Invoke();
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

**Key points:**
- **State machine is pure selection** - The switch expression ONLY maps state values to nodes
- **All execution in BT nodes** - Movement, combat, calculations happen in behavior tree nodes
- **Transitions via lifecycle** - State changes are triggered from inside BT nodes via `OnTick`, `OnExit`, etc.
- **`Variable(() => state)`** - Use a Variable to track state, initialized to default (e.g., 0)
- **Switch expression** - Clean pattern matching on `state.Value` for state selection
- **Null-coalescing (`??=`)** - Nodes are created once and cached for performance
- **Integer vs Enum states** - Use integers (0, 1, 2...) for simple cases with few states. Use an enum for complex state machines with many states or when self-descriptive state names are important for clarity:

```csharp
enum EnemyState { Idle, Patrol, Chase, Attack, Flee }

private void Awake() => AI = YieldSimple("Enemy AI", () =>
{
    var state = Variable(() => EnemyState.Idle);

    Node idleNode = null;
    Node chaseNode = null;

    return () => state.Value switch
    {
        EnemyState.Idle => idleNode ??= JustRunning(() => { /* ... */ }),
        EnemyState.Chase => chaseNode ??= Sequence(() => { /* ... */ }),
        _ => null
    };
});
```

## Important Notes for LLMs

### Architecture Guidelines

1. **State Machine for selection, Behavior Tree for execution** - State machines (`YieldSimple`) are PURE state selectors with NO execution logic. ALL execution (movement, combat, conditions) happens inside behavior tree nodes. State transitions are defined via lifecycle hooks that execute inside the BT nodes.
2. **Think right-to-left (goal-oriented)** - Start with the goal, decompose into sub-goals, then primitives. Don't build forward from actions.
3. **Build a vocabulary** - Create high-level nodes (`AcquireItem`, `AttackEnemy`) that decompose into reusable mid-level and primitive nodes
4. **Lifecycle methods enable state transitions** - Use `OnTick`, `OnExit`, `OnSuccess`, `OnFailure` to change state values
5. **Modularize nodes** - Split behaviors into reusable methods with `Func<T>` parameters and `Action lifecycle = null`
6. **Use `protected virtual`** - Allow subclasses to override specific behaviors while inheriting the state machine structure
7. **One node, one responsibility** - Each node method should do one thing well
8. **Lifecycle methods at the top** - Place lifecycle callbacks (`OnTick`, `OnEnter`, `OnExit`, etc.) at the top of node setup lambdas, right after variable declarations. Exception: `lifecycle?.Invoke()` goes at the bottom to allow callers to attach their own lifecycle hooks after the node's internal setup
9. **Use hooks for reactive data pipelines** - Prefer `Use*` hooks over manual state management when transforming, filtering, or deriving data. They're composable and self-documenting.
10. **Underscore prefix for internal hook state** - When creating custom hooks, prefix internal variables with `_` (e.g., `_lastValue`, `_state`). Only the returned `VariableType<T>` should be unprefixed.
11. **Async for tightly-coupled sequences** - Use async/await with UniTask inside a single Leaf when you have linear timed operations that don't need individual reactive cancellation. Use try-finally for guaranteed cleanup on cancellation.

### Technical Details

1. **OnDisabled is separate** - Only during resets, not normal flow
2. **Async is pervasive** - Use `async ct =>` in lifecycle callbacks
3. **CancellationTokens matter** - Always use provided `ct` parameter
4. **Re-entry triggers OnEnter** - `Tick(allowReEnter: true)` calls OnEnter, NOT OnEnabled
5. **Decorators use a stack** - `D.*()` pushes decorator, next node pops and wraps
6. **Variables are scoped** - Declare inside node setup lambdas
7. **Variable init timing** - `Variable(() => init)` runs during Enabling, before OnEnabled
8. **Reactive invalidation flow** - Check completed children → Reset forward nodes → Re-enter invalidated node
9. **Yield enables recursion** - `YieldSimpleCached` caches, `YieldSimple` re-evaluates every tick
10. **Parameters as Func** - Always pass `Func<T>` not values to stay current
11. **Return values as Func** - Use `out Func<T>` pattern, can pass directly as parameters
12. **IsReactive flag** - Set on composite nodes, they check it to enable invalidation
13. **Hooks use OnPreTick** - Built-in hooks update values in `OnPreTick` so they're available to `OnTick` callbacks
14. **Hook composition via UsePipe** - Chain multiple transformations for readability: `UsePipe(source, UseSelect, UseThrottle, UseDistinctUntilChanged)`

### When Building a System

1. **Start with the goal** - What does the AI need to achieve? (e.g., "Survive", "Defeat player", "Gather resources")
2. **Define states as pure selection** - State machine only selects which BT node runs; NO execution logic in the switch
3. **Put ALL execution in BT nodes** - Movement, combat, calculations, conditions - everything happens in behavior tree nodes
4. **Decompose each state into sub-goals** - What must happen to complete this state?
5. **Keep decomposing until you reach primitives** - MoveTo, Wait, PlayAnimation
6. **Wire state transitions via lifecycle inside BT nodes** - Use `OnTick`, `OnExit`, `OnSuccess`, `OnFailure` inside nodes
7. **Reuse nodes across states** - Same `MoveTo`, `Attack` nodes work everywhere

### Debugging Checklist

1. Check **SubStatus** to see exact phase
2. Verify **CancellationTokens** are passed correctly
3. Confirm **OnExit** properly cleans up children
4. Check **OnInvalidCheck** for unexpected invalidations
5. Ensure decorators call **child.Tick()** appropriately
6. For reactive trees: **IsReactive** set on composite (not just children)
7. For reactive trees: **OnInvalidCheck** returns false when valid
8. Watch for **OnEnter** called multiple times (expected in reactive trees)

---

This document reflects the ClosureBT implementation. For up-to-date behavior, refer to source code.