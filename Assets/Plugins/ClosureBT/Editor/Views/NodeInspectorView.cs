#if UNITASK_INSTALLED
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static ClosureBT.UI.VisualElementBuilderHelper;
using static ClosureBT.BT;
using System.Collections.Generic;

namespace ClosureBT.Editor.UI
{
    public class NodeInspectorView : VisualElement
    {
        public NodeInspectorView(Func<Node> getNode)
        {
            // Root styling - flex column to stack scrollview and footer
            E(this, _ =>
            {
                Style(new()
                {
                    backgroundColor = ColorPalette.InspectorBackground,
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column,
                });

                // ScrollView for main content
                E<ScrollView>(scrollView =>
                {
                    Style(new()
                    {
                        paddingLeft = 0,
                        paddingRight = 0,
                        paddingTop = 0,
                        paddingBottom = 0,
                        flexGrow = 1,
                    });

                    // Container with border and rounded corners
                    E<VisualElement>(container =>
                    {
                        Style(new()
                        {
                            marginLeft = 8,
                            marginRight = 8,
                            marginTop = 8,
                            marginBottom = 8,
                            borderRadius = 4,
                            borderTopWidth = 1,
                            borderBottomWidth = 1,
                            borderLeftWidth = 1,
                            borderRightWidth = 1,
                            borderTopColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                            borderBottomColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                            borderLeftColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                            borderRightColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                            overflow = Overflow.Hidden,
                        });

                        E(HeaderSection(getNode));
                        E(PropertiesSection(getNode));
                    });
                });
            });
        }

        // Section builders
        private VisualElement HeaderSection(Func<Node> getNode) => E<VisualElement>(header =>
        {
            Style(new()
            {
                backgroundColor = ColorPalette.HeaderBackground,
                borderTopLeftRadius = 4,
                borderTopRightRadius = 4,
                paddingLeft = 12,
                paddingRight = 12,
                paddingTop = 10,
                paddingBottom = 10,
                marginBottom = 0,
                borderBottomWidth = 1,
                borderBottomColor = ColorPalette.MediumBorder,
            });

            // Now consistently using the pattern
            E(HeaderLabel());
            E(HeaderTitle(getNode));
            E(StatusBadge(getNode));
        });

        private Label HeaderLabel() => E(new Label("Inspector"), label =>
        {
            Style(new()
            {
                fontSize = 10,
                color = ColorPalette.DimGrayText,
                unityFontStyleAndWeight = FontStyle.Normal,
                marginBottom = 4,
                letterSpacing = 0.5f,
            });
        });

        private Label HeaderTitle(Func<Node> getNode) => E<Label>(titleLabel =>
        {
            Style(new()
            {
                fontSize = 15,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = ColorPalette.VeryLightGrayText,
                marginBottom = 2,
            });

            Scheduler.Execute(() =>
            {
                var node = getNode();
                titleLabel.text = node != null ? node.Name : "No Selection";
            })
            .Every(0);
        });

        private StatusBadge StatusBadge(Func<Node> getNode) => E<StatusBadge>(statusBadge =>
        {
            Style(new()
            {
                marginTop = 6,
            });

            var hash = int.MinValue;

            Scheduler.Execute(() =>
            {
                var node = getNode();

                if (!Node.IsInvalid(node))
                {
                    var newHash = HashCode.Combine(node.Status, node.SubStatus);

                    if (hash == newHash)
                        return;

                    hash = newHash;
                    statusBadge.Text = $"{node.Status.ToString().ToUpper()} / {node.SubStatus.ToString().ToUpper()}";

                    // Dynamic status colors using ColorPalette
                    var badgeColor = node.Status switch
                    {
                        Status.Success => ColorPalette.StatusSuccessColor,
                        Status.Running => ColorPalette.StatusRunningColor,
                        Status.Failure => ColorPalette.StatusFailureColor,
                        _ => ColorPalette.StatusDefaultColor
                    };

                    badgeColor = node.SubStatus == SubStatus.Done && node.IsInvalid()
                        ? ColorPalette.StatusFailureColor
                        : badgeColor;

                    statusBadge.BackgroundColor = badgeColor;
                    statusBadge.TextColor = Color.white;
                }
                else
                {
                    statusBadge.Text = "NONE";
                    statusBadge.BackgroundColor = ColorPalette.StatusNoneColor;
                    statusBadge.TextColor = ColorPalette.MediumGrayText;
                }
            })
            .Every(0);
        });

        private VisualElement PropertiesSection(Func<Node> getNode) => E<VisualElement>(propertiesSection =>
        {
            Style(new()
            {
                backgroundColor = ColorPalette.WindowBackground,
                borderBottomLeftRadius = 4,
                borderBottomRightRadius = 4,
                padding = 0,
            });

            E(PropertiesSectionTitle());
            E(PropertiesContainer(getNode));
        });

        private Label PropertiesSectionTitle() => E(new Label("Variables"), sectionTitle =>
        {
            Style(new()
            {
                fontSize = 11,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = ColorPalette.LightGrayText,
                marginBottom = 0,
                marginTop = 0,
                marginLeft = 12,
                marginRight = 12,
                paddingTop = 12,
                paddingBottom = 8,
                // borderBottomWidth = 1,
                borderBottomColor = ColorPalette.MediumBorder,
            });
        });

        private VisualElement PropertiesContainer(Func<Node> getNode) => E<VisualElement>(propertiesContainer =>
        {
            Style(new()
            {
                paddingLeft = 12,
                paddingRight = 12,
                paddingTop = 8,
                paddingBottom = 8,
                borderColor = ColorPalette.MediumBorder,
                borderWidth = 1f,
                borderRadius = 4f,
                flexGrow = 1,
            });

            Node previousNode = null;

            Scheduler.Execute(() =>
            {
                var node = getNode();

                if (previousNode == node)
                    return;

                previousNode = node;
                propertiesContainer.Clear();
                DeduplicateVariables(node);

                var hasDisplay = false;
                var hiddenVariables = new List<VariableType>();

                // Variables exist
                if (node?.Variables != null)
                {
                    // First, call .Name on all variables to populate InsideUsePipe and other metadata
                    // Display public variables
                    foreach (var variable in node.Variables)
                    {
                        if (!variable.IsPublicVariable || variable.Name.StartsWith("_"))
                        {
                            hiddenVariables.Add(variable);
                            continue;
                        }

                        hasDisplay = true;
                        var propertyField = new InspectorPropertyField(variable);

                        E(propertyField, field =>
                        {
                            Style(new()
                            {
                                backgroundColor = ColorPalette.PropertyFieldBackground,
                                borderRadius = 3,
                                paddingLeft = 10,
                                paddingRight = 10,
                                marginBottom = 2,
                                marginTop = 2,
                            });
                        });

                        propertiesContainer.Add(propertyField);
                    }

                    // Add hidden variables section if there are any
                    if (hiddenVariables.Count > 0)
                    {
                        propertiesContainer.Add(HiddenVariablesSection(hiddenVariables));
                    }

                    // If no public variables were displayed, show empty state
                    if (!hasDisplay && hiddenVariables.Count == 0)
                    {
                        propertiesContainer.Add(EmptyState("No variables available"));
                    }

                    else if (node.Variables.Count == 0 && hiddenVariables.Count == 0)
                        propertiesContainer.Add(EmptyState("No variables available"));

                    return;
                }

                // Node exists but no variables
                if (node != null && !hasDisplay)
                {
                    propertiesContainer.Add(EmptyState("No variables available"));
                    return;
                }

                // No node selected
                propertiesContainer.Add(EmptyState("Select a node to view properties"));
            })
            .Every(0);
        });

        private static void DeduplicateVariables(Node node)
        {
            if (Node.IsInvalid(node) || node.Variables == null)
                return;

            foreach (var variable in node.Variables)
                _ = variable.Name; // Trigger name resolution

            // Group variables by name, keeping only the last one for each name
            var variableGroups = new Dictionary<string, List<VariableType>>();

            foreach (var variable in node.Variables)
            {
                var name = variable.Name;
                if (!variableGroups.ContainsKey(name))
                    variableGroups[name] = new System.Collections.Generic.List<VariableType>();

                variableGroups[name].Add(variable);
            }

            // Mark all but the last variable in each group as non-public
            foreach (var group in variableGroups.Values)
            {
                if (group.Count > 1)
                {
                    for (int i = 0; i < group.Count - 1; i++)
                    {
                        group[i].MarkAsNonPublic();
                    }
                }
            }
        }

        private static VisualElement EmptyState(string message) => E<VisualElement>(emptyState =>
        {
            Style(new()
            {
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                paddingTop = 24,
                paddingBottom = 24,
            });

            E(new Label(message), label =>
            {
                Style(new()
                {
                    fontSize = 11,
                    color = ColorPalette.DimGrayText,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    unityTextAlign = TextAnchor.MiddleCenter,
                });
            });
        });

        private static VisualElement HiddenVariablesSection(List<VariableType> hiddenVariables) => E<VisualElement>(section =>
        {
            // Style(new()
            // {
            //     borderTopWidth = 1,
            //     borderTopColor = ColorPalette.SubtleBorder,
            // });

            var isExpanded = false;
            VisualElement contentContainer = null;
            Label chevronLabel = null;

            // Header with chevron and title
            var header = E<VisualElement>(h =>
            {
                Style(new()
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 6,
                    paddingRight = 6,
                    borderRadius = 3,
                    marginBottom = 2,
                    marginTop = 2,
                });

                h.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    h.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
                });

                h.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    h.style.backgroundColor = Color.clear;
                });

                // Chevron
                chevronLabel = new Label("▶");
                E(chevronLabel, chevron =>
                {
                    Style(new()
                    {
                        fontSize = 9,
                        color = ColorPalette.MediumGrayText,
                        marginRight = 8,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        minWidth = 10,
                    });
                });
                h.Add(chevronLabel);

                // Title
                E(new Label($"Hidden Variables ({hiddenVariables.Count})"), title =>
                {
                    Style(new()
                    {
                        fontSize = 10,
                        padding = 8,
                        color = ColorPalette.MediumGrayText,
                        unityFontStyleAndWeight = FontStyle.Normal,
                    });
                });
            });

            // Content container (initially hidden)
            contentContainer = E<VisualElement>(content =>
            {
                Style(new()
                {
                    display = DisplayStyle.None,
                    marginTop = 8,
                });

                foreach (var variable in hiddenVariables)
                {
                    var propertyField = new InspectorPropertyField(variable);

                    E(propertyField, field =>
                    {
                        Style(new()
                        {
                            backgroundColor = ColorPalette.PropertyFieldBackground,
                            borderRadius = 3,
                            paddingLeft = 10,
                            paddingRight = 10,
                            marginBottom = 2,
                            marginTop = 2,
                        });
                    });

                    content.Add(propertyField);
                }
            });

            // Toggle functionality
            header.RegisterCallback<ClickEvent>(evt =>
            {
                isExpanded = !isExpanded;
                contentContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                chevronLabel.text = isExpanded ? "▼" : "▶";
            });

            section.Add(header);
            section.Add(contentContainer);
        });
    }
}

#endif
