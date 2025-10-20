# ClosureAI

A behavior tree system for Unity with async support and visual editor.

## Quick Start

```csharp
var tree = new Node()
    .Do(async token => Debug.Log("Hello!"))
    .Wait(1f)
    .Do(async token => Debug.Log("World!"));

void Update() => tree.Tick(Time.deltaTime);
```

See `CLOSUREAI_CONTEXT.md` for full documentation.
