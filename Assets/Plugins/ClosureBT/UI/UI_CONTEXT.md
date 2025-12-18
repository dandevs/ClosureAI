# ClosureBT UI Library Usage

This document outlines the UI architecture and API usage for the ClosureBT UI library built on Unity's UI Toolkit.

For visual design guidelines, colors, and UI patterns, see [UI_STYLE_GUIDE.md](UI_STYLE_GUIDE.md).

## Table of Contents
- [UI Architecture](#ui-architecture)
- [Visual Element Builder Pattern](#visual-element-builder-pattern)
- [Styling System](#styling-system)
- [File Organization](#file-organization)
- [Additional Notes](#additional-notes)

---

## UI Architecture

ClosureBT uses Unity's UI Toolkit (UIElements) for all editor windows and inspectors. The custom UI library provides a declarative, hierarchical approach to building interfaces.

### Core Files
- **`UI/VisualElementBuilderHelper.cs`** - Core E() helper and layout components
- **`UI/StyleApplyHelper.cs`** - Style() helper for inline styling
- **`Editor/ColorPalette.cs`** - Centralized color scheme
- **`Editor/StatusBadge.cs`** - Reusable badge component

---

## Visual Element Builder Pattern

The `E()` helper method is the foundation of our UI system. It creates elements hierarchically using a stack-based approach.

### Basic Usage

```csharp
using static ClosureBT.UI.VisualElementBuilderHelper;

// Pattern 1: Create with setup action that receives the element
E<VisualElement>(element =>
{
    Style(new() { backgroundColor = Color.red });

    // Nest children
    E<Label>(label => { label.text = "Hello"; });
});

// Pattern 2: Create from existing element
E(new Button("Click Me"), btn =>
{
    btn.clicked += () => Debug.Log("Clicked!");
    Style(new() { height = 30 });
});
```

### How It Works

The E() method:
1. Creates a visual element
2. Pushes it onto an internal stack
3. Executes the setup action (where you add children)
4. Pops the element from the stack
5. Adds it to the current parent

This enables clean, hierarchical UI declaration without manual parent/child management.

---

## Styling System

The `Style()` helper uses `StyleApplyHelper` struct to apply inline styles fluently.

### Style Helper

```csharp
Style(new()
{
    backgroundColor = ColorPalette.DarkBackground,
    borderRadius = 8,
    borderWidth = 2,
    borderColor = ColorPalette.BlueAccent,
    padding = 12,
    margin = 8,
    flexGrow = 1,
    flexDirection = FlexDirection.Row,
    alignItems = Align.Center,
    justifyContent = Justify.Center,
});
```

### Convenience Properties

StyleApplyHelper provides shortcuts for setting multiple values:

- `borderWidth` - Sets all border sides at once
- `borderRadius` - Sets all corners at once
- `borderColor` - Sets all border colors at once
- `padding` - Sets all padding sides at once
- `margin` - Sets all margin sides at once

For individual sides, use specific properties:
```csharp
Style(new()
{
    paddingLeft = 16,
    paddingRight = 16,
    paddingTop = 12,
    paddingBottom = 12,
    borderBottomWidth = 1,
});
```

---

## File Organization

### Editor Windows
Place in `Editor/` folder:
- `TreeEditorWindow.cs` - Main behavior tree editor
- `NodeInspectorView.cs` - Node property inspector
- `UniTaskInstallerWindow.cs` - Setup windows

### UI Components
Place in `UI/` folder:
- `VisualElementBuilderHelper.cs` - Core helpers
- `StyleApplyHelper.cs` - Styling system

### Shared Components
Place in `Editor/` folder:
- `ColorPalette.cs` - Colors
- `StatusBadge.cs` - Reusable components

---

## Additional Notes

### Always Use Static Import

```csharp
using static ClosureBT.UI.VisualElementBuilderHelper;
```

This enables clean E() and Style() calls without prefixes.

### Nest E() Calls for Hierarchy

```csharp
E<VisualElement>(parent =>
{
    Style(new() { padding = 8 });

    E<Label>(child1 => { /* ... */ });
    E<Label>(child2 => { /* ... */ });
});
```

The builder maintains a stack automatically - no manual AddChild() calls needed.

### EditorPrefs Naming Convention

Use a consistent prefix for all EditorPrefs:

```csharp
private const string PREF_KEY = "ClosureBT";
private static string Key(string str) => PREF_KEY + "_" + str;

// Usage
EditorPrefs.GetBool(Key("SomeSetting"), defaultValue);
EditorPrefs.SetBool(Key("SomeSetting"), value);
```

### InitializeOnLoad Pattern

For editor initialization:

```csharp
[InitializeOnLoad]
public class MyInitializer
{
    static MyInitializer()
    {
        // Use delayed call for safety
        EditorApplication.delayCall += Initialize;
    }

    private static void Initialize()
    {
        // Initialization code
    }
}
```

### Scripting Define Symbols

```csharp
// Check for define
private static bool HasDefine(string define)
{
    BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
    NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
    string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
    return defines.Contains(define);
}

// Add define
private static void AddDefine(string define)
{
    BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
    NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
    string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

    if (defines.Contains(define))
        return;

    if (!string.IsNullOrEmpty(defines))
        defines += ";" + define;
    else
        defines = define;

    PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
}
```

---

*This guide is a living document. Update it as patterns evolve and new conventions are established.*
