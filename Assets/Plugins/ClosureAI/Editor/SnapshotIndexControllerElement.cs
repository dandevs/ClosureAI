#if UNITASK_INSTALLED
using System;
using UnityEngine.UIElements;
using ClosureAI.Editor.UI;
using ClosureAI.UI;
using UnityEngine;
using static ClosureAI.AI;
using static ClosureAI.UI.VisualElementBuilderHelper;

namespace ClosureAI.Editor
{
    public class SnapshotIndexControllerElement : VisualElement
    {
        private readonly Func<Node> _getNode;
        private Node Node => _getNode();

        public SnapshotIndexControllerElement(Func<Node> getNode)
        {
            _getNode = getNode;
            CreateViewElement();
        }

        public void CreateViewElement() => E(this, _ =>
        {
            Style(new()
            {
                paddingTop = 0,
                paddingBottom = 0,
                paddingLeft = 0,
                paddingRight = 0,
                display = DisplayStyle.Flex,
                flexShrink = 0,
            });

            // Container with border - flush to edges for integrated toolbar feel
            E<VisualElement>(container =>
            {
                Style(new()
                {
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    marginBottom = 0,
                    borderRadius = 0,
                    borderTopWidth = 1,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    overflow = Overflow.Hidden,
                });

                // Update border color - always use neutral border
                Scheduler.Execute(() =>
                {
                    var borderColor = new Color(0.15f, 0.15f, 0.15f, 1f);

                    container.style.borderTopColor = borderColor;
                    container.style.borderBottomColor = borderColor;
                    container.style.borderLeftColor = borderColor;
                    container.style.borderRightColor = borderColor;
                })
                .Every(0);

                // Header row with status badge and info
                //     E<FlexRow>(headerRow =>
                //     {
                //         FlexGap(20);

                //         Style(new()
                //         {
                //             backgroundColor = ColorPalette.HeaderBackground,
                //             minHeight = 32,
                //             alignItems = Align.Center,
                //             paddingLeft = 12,
                //             paddingRight = 12,
                //             paddingTop = 8,
                //             paddingBottom = 8,
                //             marginBottom = 0,
                //             borderBottomWidth = 1,
                //             borderBottomColor = ColorPalette.MediumBorder,
                //         });

                //         // Status badge
                //         E<StatusBadge>(statusBadge =>
                //         {
                //             Style(new()
                //             {
                //                 marginRight = 12,
                //                 marginTop = 0, // Override the default margin from StatusBadge
                //             });

                //         Scheduler.Execute(() =>
                //         {
                //             if (NodeHistoryTracker.IsShowingHistory)
                //             {
                //                 statusBadge.Text = "PAUSED";
                //                 statusBadge.BackgroundColor = ColorPalette.SnapshotHistoryIndicator;
                //                 statusBadge.TextColor = Color.white;
                //             }
                //             else
                //             {
                //                 statusBadge.Text = "LIVE";
                //                 statusBadge.BackgroundColor = ColorPalette.SnapshotLiveIndicator;
                //                 statusBadge.TextColor = Color.white;
                //             }
                //         })
                //         .Every(0);
                //     });

                //     E(SpacerElement());

                //         // Total snapshots info
                //         E<Label>(label =>
                //         {
                //             var prevHash = 0;

                //             Style(new()
                //             {
                //                 fontSize = 10,
                //                 color = ColorPalette.DimGrayText,
                //                 unityTextAlign = TextAnchor.MiddleRight,
                //                 unityFontStyleAndWeight = FontStyle.Normal,
                //             });

                //         Scheduler.Execute(() =>
                //         {
                //             var maxSnapshots = Preferences.MaxRecordedSnapshots;
                //             var snapshotCount = NodeSnapshotHolder.SnapshotGlobalIndex;
                //             var hash = HashCode.Combine(maxSnapshots, snapshotCount);

                //             if (hash != prevHash)
                //             {
                //                 prevHash = hash;
                //                 label.text = $"Snapshots: {snapshotCount}/{maxSnapshots}";
                //             }
                //         })
                //         .Every(0);
                //     });
                // });

                // Main control row
                E<FlexRow>(controlRow =>
                {
                    Style(new()
                    {
                        backgroundColor = ColorPalette.WindowBackground,
                        minHeight = 48,
                        alignItems = Align.Center,
                        paddingLeft = 12,
                        paddingRight = 12,
                        paddingTop = 10,
                        paddingBottom = 10,

                        borderTopColor = ColorPalette.MediumBorder,
                        borderTopWidth = 1,
                    });

                // E(SnapshotButton("◀ Prev", NodeHistoryTracker.PreviousSnapshot, () =>
                // {
                //     var snapshotCount = NodeSnapshotHolder.GlobalEntries.Count;
                //     var currentIndex = NodeHistoryTracker.CurrentGlobalSnapshotIndex;
                //     return snapshotCount > 0 && currentIndex > 0;
                // }));
                //
                // E(SnapshotButton("Next ▶", NodeHistoryTracker.NextSnapshot, () =>
                // {
                //     var snapshotCount = NodeSnapshotHolder.GlobalEntries.Count;
                //     var currentIndex = NodeHistoryTracker.CurrentGlobalSnapshotIndex;
                //     var maxGlobalIndex = NodeSnapshotHolder.SnapshotGlobalIndex - 1;
                //     return snapshotCount > 0 && currentIndex < maxGlobalIndex;
                // }));

                E<FlexColumn>(() =>
                {
                    Style(new()
                    {
                        flexGrow = 1,
                        paddingLeft = 30,
                        paddingRight = 30,
                    });

                    // Local slider with label
                    E<FlexRow>(() =>
                    {
                        Style(new()
                        {
                            alignItems = Align.Center,
                            marginBottom = 0,
                        });

                        SnapshotSliderLocal(() => Node);

                        // Previous button
                        E(LocalSnapshotButton("◀", () =>
                        {
                            if (Node.IsInvalid(Node))
                                return;

                            var snapshotHolder = NodeSnapshotHolder.Get(Node);
                            if (snapshotHolder.Entries.Count == 0)
                                return;

                            var currentLocalIndex = GetCurrentLocalIndex(Node, snapshotHolder);
                            if (currentLocalIndex > 0)
                            {
                                var globalIndex = snapshotHolder.Entries[currentLocalIndex - 1].GlobalIndex;
                                NodeHistoryTracker.ChangeGlobalSnapshotIndex(globalIndex);
                            }
                        }, () =>
                        {
                            if (Node.IsInvalid(Node))
                                return false;

                            var snapshotHolder = NodeSnapshotHolder.Get(Node);
                            if (snapshotHolder.Entries.Count == 0)
                                return false;

                            var currentLocalIndex = GetCurrentLocalIndex(Node, snapshotHolder);
                            return currentLocalIndex > 0;
                        }));

                        // Next button
                        E(LocalSnapshotButton("▶", () =>
                        {
                            if (Node.IsInvalid(Node))
                                return;

                            var snapshotHolder = NodeSnapshotHolder.Get(Node);
                            if (snapshotHolder.Entries.Count == 0)
                                return;

                            var currentLocalIndex = GetCurrentLocalIndex(Node, snapshotHolder);
                            var maxLocalIndex = snapshotHolder.Entries.Count - 1;

                            if (currentLocalIndex < maxLocalIndex)
                            {
                                var globalIndex = snapshotHolder.Entries[currentLocalIndex + 1].GlobalIndex;
                                NodeHistoryTracker.ChangeGlobalSnapshotIndex(globalIndex);
                            }
                        }, () =>
                        {
                            if (Node.IsInvalid(Node))
                                return false;

                            var snapshotHolder = NodeSnapshotHolder.Get(Node);
                            if (snapshotHolder.Entries.Count == 0)
                                return false;

                            var currentLocalIndex = GetCurrentLocalIndex(Node, snapshotHolder);
                            var maxLocalIndex = snapshotHolder.Entries.Count - 1;
                            return currentLocalIndex < maxLocalIndex;
                        }));

                        SnapshotCounterLabel(() =>
                        {
                            if (Node.IsInvalid(Node))
                                return (0, 0);

                            var snapshotHolder = NodeSnapshotHolder.Get(Node);
                            if (snapshotHolder.Entries.Count == 0)
                                return (0, 0);

                            var currentIndex = GetCurrentLocalIndex(Node, snapshotHolder);
                            return (currentIndex, snapshotHolder.Entries.Count - 1);
                        });
                    });

                    // Global slider with label
                    // E<FlexRow>(() =>
                    // {
                    //     Style(new()
                    //     {
                    //         alignItems = Align.Center,
                    //     });
                    //
                    //     E<Label>(label =>
                    //     {
                    //         label.text = "Global:";
                    //         Style(new()
                    //         {
                    //             fontSize = 9,
                    //             color = ColorPalette.SnapshotInfoText,
                    //             width = 38,
                    //             unityTextAlign = TextAnchor.MiddleLeft,
                    //         });
                    //     });
                    //
                    //     SnapshotSliderGlobal();
                    //
                    //     SnapshotCounterLabel(() =>
                    //     {
                    //         var maxGlobalIndex = NodeSnapshotHolder.SnapshotGlobalIndex - 1;
                    //         if (maxGlobalIndex < 0)
                    //             return (0, 0);
                    //
                    //         var currentIndex = NodeHistoryTracker.IsShowingHistory
                    //             ? NodeHistoryTracker.CurrentGlobalSnapshotIndex
                    //             : maxGlobalIndex;
                    //
                    //         return (currentIndex, maxGlobalIndex);
                    //     });
                    // });
                });

                    // Current index label - DISABLED
                    // E<Label>(label =>
                    // {
                    //     Style(new()
                    //     {
                    //         minWidth = 85,
                    //         fontSize = 11,
                    //         color = ColorPalette.VeryLightGrayText,
                    //         unityTextAlign = TextAnchor.MiddleCenter,
                    //         backgroundColor = ColorPalette.SnapshotIndexBackground,
                    //         borderRadius = 3,
                    //         paddingTop = 4,
                    //         paddingBottom = 4,
                    //         paddingLeft = 8,
                    //         paddingRight = 8,
                    //         marginRight = 12,
                    //         unityFontStyleAndWeight = FontStyle.Normal,
                    //     });
                    //
                    // Scheduler.Execute(() =>
                    // {
                    //     var snapshotCount = NodeSnapshotHolder.SnapshotGlobalIndex;
                    //     var currentIndex = NodeHistoryTracker.CurrentGlobalSnapshotIndex;
                    //     var isShowingHistory = NodeHistoryTracker.IsShowingHistory;
                    //     var maxGlobalIndex = NodeSnapshotHolder.SnapshotGlobalIndex - 1;
                    //
                    //     if (snapshotCount == 0)
                    //         label.text = "No Data";
                    //     else if (isShowingHistory)
                    //         label.text = $"{currentIndex} / {maxGlobalIndex}";
                    //     else
                    //         label.text = "Live";
                    // })
                    // .Every(0);
                    // });

                    // Resume button
                    E<Button>(btn =>
                    {
                        btn.text = "▶ Resume";
                        btn.clicked += NodeHistoryTracker.Resume;

                        Style(new()
                        {
                            minWidth = 80,
                            height = 28,
                            paddingLeft = 12,
                            paddingRight = 12,
                            paddingTop = 4,
                            paddingBottom = 4,
                            borderRadius = 3,
                        });

                        Scheduler.Execute(() =>
                        {
                            var isShowingHistory = NodeHistoryTracker.IsShowingHistory;
                            btn.SetEnabled(isShowingHistory);
                        })
                        .Every(0);
                    });
                });
            });
        });

        private static void SnapshotSliderLocal(Func<Node> getNode) => E<SliderInt>(slider =>
        {
            Style(new()
            {
                flexGrow = 1,
                height = 20,
            });

            var isUpdatingFromCode = false;

            slider.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingFromCode)
                    return;

                var node = getNode();

                if (Node.IsInvalid(node))
                    return;

                var snapshotHolder = NodeSnapshotHolder.Get(node);

                // Map local index to global index
                if (evt.newValue >= 0 && evt.newValue < snapshotHolder.Entries.Count)
                {
                    var globalIndex = snapshotHolder.Entries[evt.newValue].GlobalIndex;
                    NodeHistoryTracker.ChangeGlobalSnapshotIndex(globalIndex);
                }
            });

             Scheduler.Execute(() =>
             {
                 var node = getNode();

                 if (Node.IsInvalid(node))
                 {
                     slider.SetEnabled(false);
                     slider.lowValue = 0;
                     slider.highValue = 0;
                     return;
                 }

                 var snapshotHolder = NodeSnapshotHolder.Get(node);
                 var snapshotCount = snapshotHolder.Entries.Count;

                 slider.lowValue = 0;
                 slider.highValue = snapshotCount > 0 ? snapshotCount - 1 : 0;
                 slider.SetEnabled(true);

                 // Map current global index to local index
                 var localIndex = snapshotCount > 0 ? snapshotCount - 1 : 0; // Default to latest or 0
                 var isShowingHistory = NodeHistoryTracker.IsShowingHistory;

                 if (isShowingHistory && snapshotCount > 0)
                 {
                     var currentGlobalIndex = NodeHistoryTracker.CurrentGlobalSnapshotIndex;

                     // Use the nearest snapshot logic - rounds up by default, down if beyond last entry
                     if (snapshotHolder.TryGetNearestSnapshot(currentGlobalIndex, out _, out localIndex))
                     {
                         // Successfully found nearest snapshot
                     }
                     else
                     {
                         // No entries at all, default to 0
                         localIndex = 0;
                     }
                 }

                 isUpdatingFromCode = true;
                 slider.SetValueWithoutNotify(localIndex);
                 isUpdatingFromCode = false;
            })
            .Every(0);
        });

        private static void SnapshotSliderGlobal() => E<SliderInt>(slider =>
        {
            Style(new()
            {
                flexGrow = 1,
                height = 20,
            });

            var isUpdatingFromCode = false;

            slider.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingFromCode)
                    return;

                NodeHistoryTracker.ChangeGlobalSnapshotIndex(evt.newValue);
            });

            Scheduler.Execute(() =>
            {
                var maxGlobalIndex = NodeSnapshotHolder.SnapshotGlobalIndex - 1;

                slider.lowValue = 0;
                slider.highValue = Mathf.Max(0, maxGlobalIndex);
                slider.SetEnabled(maxGlobalIndex >= 0);

                isUpdatingFromCode = true;
                var currentValue = NodeHistoryTracker.IsShowingHistory ? NodeHistoryTracker.CurrentGlobalSnapshotIndex : maxGlobalIndex;
                slider.SetValueWithoutNotify(currentValue);
                isUpdatingFromCode = false;
            })
            .Every(0);
        });

        private static void SnapshotCounterLabel(Func<(int current, int total)> getCounterValues) => E<Label>(label =>
        {
            Style(new()
            {
                fontSize = 9,
                color = ColorPalette.DimGrayText,
                minWidth = 40,
                unityTextAlign = TextAnchor.MiddleRight,
                marginLeft = 8,
                unityFontStyleAndWeight = FontStyle.Normal,
            });

            var prevHash = 0;

            Scheduler.Execute(() =>
            {
                var (current, total) = getCounterValues();
                var hash = HashCode.Combine(current, total);

                if (hash != prevHash)
                {
                    prevHash = hash;
                    label.text = (total == 0 && current == 0) ? "0/0" : $"{current}/{total}";
                }
            })
            .Every(0);
        });

        private static int GetCurrentLocalIndex(Node node, NodeSnapshotHolder snapshotHolder)
        {
            if (NodeHistoryTracker.IsShowingHistory)
            {
                var currentGlobalIndex = NodeHistoryTracker.CurrentGlobalSnapshotIndex;
                if (snapshotHolder.TryGetNearestSnapshot(currentGlobalIndex, out var entry, out var localIndex))
                {
                    return localIndex;
                }
                return 0;
            }
            else
            {
                return snapshotHolder.Entries.Count - 1;
            }
        }

        private static Button LocalSnapshotButton(string text, Action onClick, Func<bool> isEnabled) => E(new Button(onClick), btn =>
        {
            btn.text = text;

            Style(new()
            {
                fontSize = 11,
                minWidth = 26,
                height = 24,
                marginLeft = 6,
                marginRight = 0,
                paddingLeft = 6,
                paddingRight = 6,
                paddingTop = 3,
                paddingBottom = 3,
                borderRadius = 2,
            });

            Scheduler.Execute(() =>
            {
                btn.SetEnabled(isEnabled());
            })
            .Every(0);
        });



        private VisualElement SpacerElement() => E<VisualElement>(() =>
        {
            Style(new() { flexGrow = 1, });
        });

        private Button SnapshotButton(string text, Action onClicked, Func<bool> enabledCondition) => E<Button>(btn =>
        {
            Style(new()
            {
                height = 26,
                width = 70,
                fontSize = 10,
                borderColor = ColorPalette.SnapshotButtonBorder,
                borderRadius = 3,
            });

            btn.text = text;
            btn.clicked += onClicked;

            Scheduler.Execute(() =>
            {
                btn.SetEnabled(enabledCondition());
            })
            .Every(0);
        });
    }
}

#endif