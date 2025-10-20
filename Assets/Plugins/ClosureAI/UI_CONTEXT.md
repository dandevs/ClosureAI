# ClosureAI Design Philosophy & Style Guide

This document outlines the design patterns, UI conventions, and coding standards for the ClosureAI behavior tree system.

## Table of Contents
- [UI Architecture](#ui-architecture)
- [Visual Element Builder Pattern](#visual-element-builder-pattern)
- [Styling System](#styling-system)
- [Color Palette](#color-palette)
- [Layout Components](#layout-components)
- [Common UI Patterns](#common-ui-patterns)
- [Design Principles](#design-principles)
- [Best Practices](#best-practices)

---

## UI Architecture

ClosureAI uses Unity's UI Toolkit (UIElements) for all editor windows and inspectors. The custom UI library provides a declarative, hierarchical approach to building interfaces.

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
using static ClosureAI.UI.VisualElementBuilderHelper;

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

## Color Palette

**IMPORTANT:** Always use `ColorPalette` for colors. Never hardcode color values.

### Background Colors

```csharp
ColorPalette.DarkBackground          // (0.12, 0.12, 0.12) - Main dark bg
ColorPalette.MediumDarkBackground    // (0.15, 0.15, 0.15)
ColorPalette.DarkerBackground        // (0.10, 0.10, 0.10)
ColorPalette.MediumBackground        // (0.18, 0.18, 0.18)
ColorPalette.WindowBackground        // Unity's window background
ColorPalette.HeaderBackground        // (0.18, 0.18, 0.18) dark mode
ColorPalette.AlternateBackground     // (0.25, 0.25, 0.25) dark mode
ColorPalette.InspectorBackground     // ~(0.22, 0.22, 0.22)
```

### Text Colors

```csharp
ColorPalette.VeryLightGrayText       // (0.9, 0.9, 0.9) - Highest contrast
ColorPalette.LightGrayText           // (0.8, 0.8, 0.8) - Body text
ColorPalette.MediumGrayText          // (0.7, 0.7, 0.7) - Secondary text
ColorPalette.DimGrayText             // (0.6, 0.6, 0.6) - Tertiary/hints
```

### Accent Colors

```csharp
ColorPalette.BlueAccent              // (0.3, 0.6, 1.0) - Primary actions
ColorPalette.BlueAccentTransparent   // (0.3, 0.6, 1.0, 0.6)
ColorPalette.OrangeAccent            // (1.0, 0.6, 0.2) - Warnings
ColorPalette.GreenAccent             // (0.2, 0.8, 0.2) - Success
```

### Status Colors

```csharp
ColorPalette.StatusSuccessColor      // (0.3, 0.9, 0.3) - Bright green
ColorPalette.StatusFailureColor      // (0.95, 0.3, 0.3) - Bright red
ColorPalette.StatusRunningColor      // (1.0, 0.8, 0.2) - Yellow
```

### Border Colors

```csharp
ColorPalette.SubtleBorder            // (0.2, 0.2, 0.2) - Subtle dividers
ColorPalette.MediumBorder            // (0.3, 0.3, 0.3) - Standard borders
```

### Typography Scale

- **Titles:** fontSize 14-18, Bold, VeryLightGrayText
- **Body:** fontSize 12, Normal, LightGrayText
- **Secondary:** fontSize 11, Normal, MediumGrayText
- **Small/Hints:** fontSize 10, Normal, DimGrayText

---

## Layout Components

### FlexRow & FlexColumn

Use these for layouts with consistent gaps between children:

```csharp
// Row with 20px gap between children
E<FlexRow>(() =>
{
    FlexGap(20);
    E<Label>(() => { /* ... */ });
    E<Button>(() => { /* ... */ });
});

// Column with 10px gap
E<FlexColumn>(() =>
{
    FlexGap(10);
    E<Label>(() => { /* ... */ });
    E<Label>(() => { /* ... */ });
});

// Or create with gap in constructor
var row = new FlexRow(15);
E(row, () => {
    // children...
});
```

### FlexGapView

The base class for FlexRow and FlexColumn. Automatically inserts spacers between children.

---

## Common UI Patterns

### Standard Window Layout

```csharp
public class MyWindow : EditorWindow
{
    [MenuItem("Tools/My Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<MyWindow>();
        window.titleContent = new GUIContent("My Window");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void CreateGUI()
    {
        rootVisualElement.Clear();

        E(rootVisualElement, _ =>
        {
            Style(new()
            {
                backgroundColor = ColorPalette.WindowBackground,
            });

            // Header
            E<VisualElement>(header =>
            {
                Style(new()
                {
                    backgroundColor = ColorPalette.HeaderBackground,
                    paddingLeft = 16,
                    paddingRight = 16,
                    paddingTop = 12,
                    paddingBottom = 12,
                    borderBottomWidth = 1,
                    borderBottomColor = ColorPalette.MediumBorder,
                });

                E<Label>(title => {
                    title.text = "Window Title";
                    Style(new() {
                        fontSize = 14,
                        color = ColorPalette.VeryLightGrayText,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    });
                });
            });

            // Content
            E<VisualElement>(content =>
            {
                Style(new()
                {
                    paddingLeft = 16,
                    paddingRight = 16,
                    paddingTop = 16,
                    paddingBottom = 16,
                    flexGrow = 1,
                });

                // Add content here
            });

            // Footer (optional)
            E<VisualElement>(footer =>
            {
                Style(new()
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    paddingLeft = 16,
                    paddingRight = 16,
                    paddingTop = 12,
                    paddingBottom = 12,
                    borderTopWidth = 1,
                    borderTopColor = ColorPalette.MediumBorder,
                });

                // Add buttons here
            });
        });
    }
}
```

### Button Styles

```csharp
// Standard button
E<Button>(btn =>
{
    btn.text = "Action";
    btn.clicked += OnAction;

    Style(new()
    {
        minWidth = 80,
        height = 28,
        paddingLeft = 12,
        paddingRight = 12,
        borderRadius = 3,
    });
});

// Primary action button
E<Button>(btn =>
{
    btn.text = "Confirm";
    btn.clicked += OnConfirm;

    Style(new()
    {
        minWidth = 80,
        height = 28,
        backgroundColor = ColorPalette.BlueAccent,
        borderWidth = 0,
        borderRadius = 3,
        color = Color.white,
    });
});

// Compact navigation button
E<Button>(btn =>
{
    btn.text = "◀";
    btn.clicked += OnPrevious;

    Style(new()
    {
        fontSize = 11,
        minWidth = 26,
        height = 24,
        paddingLeft = 6,
        paddingRight = 6,
        borderRadius = 2,
    });
});
```

### Info Panel / Section

```csharp
E<VisualElement>(infoPanel =>
{
    Style(new()
    {
        backgroundColor = ColorPalette.MediumDarkBackground,
        borderRadius = 4,
        borderWidth = 1,
        borderColor = ColorPalette.SubtleBorder,
        paddingLeft = 12,
        paddingRight = 12,
        paddingTop = 12,
        paddingBottom = 12,
        marginBottom = 16,
    });

    E<Label>(title => {
        title.text = "Section Title";
        Style(new() {
            fontSize = 12,
            color = ColorPalette.VeryLightGrayText,
            unityFontStyleAndWeight = FontStyle.Bold,
            marginBottom = 8,
        });
    });

    E<Label>(body => {
        body.text = "Section content...";
        Style(new() {
            fontSize = 11,
            color = ColorPalette.LightGrayText,
            whiteSpace = WhiteSpace.Normal,
        });
    });
});
```

### Collapsible Header

```csharp
E<VisualElement>(header =>
{
    Style(new()
    {
        flexDirection = FlexDirection.Row,
        alignItems = Align.Center,
        paddingLeft = 6,
        paddingRight = 6,
        paddingTop = 4,
        paddingBottom = 4,
        borderRadius = 3,
    });

    // Hover effect
    header.RegisterCallback<MouseEnterEvent>(evt =>
    {
        header.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
    });

    header.RegisterCallback<MouseLeaveEvent>(evt =>
    {
        header.style.backgroundColor = Color.clear;
    });

    // Click handler
    header.RegisterCallback<ClickEvent>(evt =>
    {
        isExpanded = !isExpanded;
        // Update UI
    });

    // Arrow icon
    E<Label>(arrow => {
        arrow.text = isExpanded ? "▼" : "▶";
        Style(new() {
            fontSize = 10,
            color = ColorPalette.MediumGrayText,
            marginRight = 6,
        });
    });

    // Title
    E<Label>(title => {
        title.text = "Collapsible Section";
        Style(new() {
            fontSize = 12,
            color = ColorPalette.VeryLightGrayText,
        });
    });
});
```

### Status Badge

```csharp
E<StatusBadge>(badge =>
{
    badge.Text = "RUNNING";
    badge.BackgroundColor = ColorPalette.StatusRunningColor;
    badge.TextColor = Color.white;

    Style(new()
    {
        marginLeft = 8,
    });
});
```

---

## Design Principles

### 1. Consistency
- Always use ColorPalette for colors
- Follow the established typography scale
- Use consistent spacing (multiples of 4: 4, 8, 12, 16, 20, 24)
- Border radius: 3-4px for small elements, 6-8px for large panels

### 2. Hierarchy
- Use header/content/footer structure for windows
- Clear visual separation with borders and background colors
- Typography size indicates importance

### 3. Dark Theme First
- All designs assume dark mode
- High contrast text on dark backgrounds
- Subtle borders and dividers
- Accent colors for actions and status

### 4. Minimal & Professional
- No emojis unless explicitly required
- Clean, rectangular layouts
- Subtle animations/hover effects only
- Focus on functionality over decoration

### 5. Responsive Feedback
- Hover effects on interactive elements
- Clear button states
- Loading/progress indicators where appropriate
- Confirmation dialogs for destructive actions

---

## Best Practices

### Always Use Static Import

```csharp
using static ClosureAI.UI.VisualElementBuilderHelper;
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

### Use FlexRow/FlexColumn for Consistent Gaps

```csharp
// Good
E<FlexRow>(() =>
{
    FlexGap(12);
    E<Button>(() => { /* ... */ });
    E<Button>(() => { /* ... */ });
});

// Avoid
E<VisualElement>(() =>
{
    E<Button>(() => { Style(new() { marginRight = 12 }); });
    E<Button>(() => { /* ... */ });
});
```

### Reactive Updates with Scheduler

```csharp
Scheduler.Execute(() =>
{
    button.SetEnabled(someCondition);
    label.text = currentValue.ToString();
}).Every(0); // Every frame
```

### Clean Up Resources

```csharp
element.RegisterCallback<DetachFromPanelEvent>(evt =>
{
    // Clean up schedulers, event handlers, etc.
});
```

### Common Text Alignment

```csharp
// Left-aligned body text
Style(new()
{
    unityTextAlign = TextAnchor.UpperLeft,
    whiteSpace = WhiteSpace.Normal, // Enable word wrap
});

// Centered titles
Style(new()
{
    unityTextAlign = TextAnchor.MiddleCenter,
});
```

### Standard Window Sizes

- Small dialogs: 400x300 - 450x350
- Medium windows: 600x400 - 800x600
- Large windows: 1000x700+

Set both minSize and maxSize for fixed-size dialogs.

### Interactive Elements

```csharp
// Clickable label (link style)
E<Label>(link =>
{
    link.text = "Click Here";
    Style(new()
    {
        color = ColorPalette.BlueAccent,
        unityFontStyleAndWeight = FontStyle.Bold,
    });

    link.RegisterCallback<MouseEnterEvent>(evt =>
    {
        link.style.color = new Color(0.5f, 0.8f, 1f);
    });

    link.RegisterCallback<MouseLeaveEvent>(evt =>
    {
        link.style.color = ColorPalette.BlueAccent;
    });

    link.RegisterCallback<ClickEvent>(evt =>
    {
        // Handle click
    });
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

## Common Patterns Reference

### Full Window Template

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ClosureAI.UI;
using static ClosureAI.UI.VisualElementBuilderHelper;

namespace ClosureAI.Editor
{
    public class TemplateWindow : EditorWindow
    {
        [MenuItem("Tools/ClosureAI/Template")]
        public static void ShowWindow()
        {
            var window = GetWindow<TemplateWindow>();
            window.titleContent = new GUIContent("Template");
            window.minSize = new Vector2(450, 320);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            E(rootVisualElement, _ =>
            {
                Style(new() { backgroundColor = ColorPalette.WindowBackground });

                // Header
                E<VisualElement>(header =>
                {
                    Style(new()
                    {
                        backgroundColor = ColorPalette.HeaderBackground,
                        paddingLeft = 16,
                        paddingRight = 16,
                        paddingTop = 12,
                        paddingBottom = 12,
                        borderBottomWidth = 1,
                        borderBottomColor = ColorPalette.MediumBorder,
                    });

                    E<Label>(title =>
                    {
                        title.text = "Window Title";
                        Style(new()
                        {
                            fontSize = 14,
                            color = ColorPalette.VeryLightGrayText,
                            unityFontStyleAndWeight = FontStyle.Bold,
                        });
                    });
                });

                // Content
                E<VisualElement>(content =>
                {
                    Style(new()
                    {
                        paddingLeft = 16,
                        paddingRight = 16,
                        paddingTop = 16,
                        paddingBottom = 16,
                        flexGrow = 1,
                    });

                    // Add your content here
                });

                // Footer
                E<VisualElement>(footer =>
                {
                    Style(new()
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.FlexEnd,
                        paddingLeft = 16,
                        paddingRight = 16,
                        paddingTop = 12,
                        paddingBottom = 12,
                        borderTopWidth = 1,
                        borderTopColor = ColorPalette.MediumBorder,
                    });

                    E<Button>(btn =>
                    {
                        btn.text = "Action";
                        btn.clicked += () => { /* ... */ };
                        Style(new()
                        {
                            minWidth = 80,
                            height = 28,
                            borderRadius = 3,
                        });
                    });
                });
            });
        }
    }
}
```

---

## Additional Notes

### EditorPrefs Naming Convention

Use a consistent prefix for all EditorPrefs:

```csharp
private const string PREF_KEY = "ClosureAI";
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
