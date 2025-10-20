#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static ClosureAI.UI.VisualElementBuilderHelper;

namespace ClosureAI.Editor
{
    public class Preferences : SettingsProvider
    {
        private const string PREF_KEY = "ClosureAI";
        private const int DEFAULT_MAX_RECORDED_SNAPSHOTS = 500;

        public static event Action OnAnyPreferenceChanged = delegate {};

        #region EditorPrefs ---------------------------------------------------------------------------------
        private static int? _maxRecordedSnapshots;
        public static int MaxRecordedSnapshots
        {
            get => _maxRecordedSnapshots ??= EditorPrefs.GetInt(Key(nameof(MaxRecordedSnapshots)), DEFAULT_MAX_RECORDED_SNAPSHOTS);
            set
            {
                _maxRecordedSnapshots = value;
                EditorPrefs.SetInt(Key(nameof(MaxRecordedSnapshots)), value);
                OnAnyPreferenceChanged();
            }
        }

        private static bool? _RecordSnapshots;
        public static bool RecordSnapshots
        {
            get => _RecordSnapshots ??= EditorPrefs.GetBool(Key(nameof(RecordSnapshots)), true);
            set
            {
                _RecordSnapshots = value;
                EditorPrefs.SetBool(Key(nameof(RecordSnapshots)), value);
                OnAnyPreferenceChanged();
            }
        }

        private static bool? _trackAllNodes;
        public static bool TrackAllNodes
        {
            get => _trackAllNodes ??= EditorPrefs.GetBool(Key(nameof(TrackAllNodes)), true);
            set
            {
                _trackAllNodes = value;
                EditorPrefs.SetBool(Key(nameof(TrackAllNodes)), value);
                OnAnyPreferenceChanged();
            }
        }

        private static int? _recordOnStatusChangeFlags;
        public static NodeStatusChangeFlags RecordOnStatusChangeFlags
        {
            get => (NodeStatusChangeFlags)(_recordOnStatusChangeFlags ??= EditorPrefs.GetInt(Key(nameof(RecordOnStatusChangeFlags)), (int)(NodeStatusChangeFlags.Running | NodeStatusChangeFlags.Failed | NodeStatusChangeFlags.Success | NodeStatusChangeFlags.ToNone)));
            set
            {
                _recordOnStatusChangeFlags = (int)value;
                EditorPrefs.SetInt(Key(nameof(RecordOnStatusChangeFlags)), (int)value);
                OnAnyPreferenceChanged();
            }
        }
        #endregion -----------------------------------------------------------------------------------------

        public Preferences(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) {}

        public override void OnActivate(string searchContext, VisualElement rootElement) => E(rootElement, _ =>
        {
            E<Label>(label =>
            {
                label.text = "ClosureAI Settings";
                label.style.fontSize = 18;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginBottom = 10;
            });

            E<IntegerField>(field =>
            {
                field.label = "Max Recorded Snapshots";
                field.value = MaxRecordedSnapshots;
                field.RegisterValueChangedCallback(evt =>
                {
                    MaxRecordedSnapshots = Mathf.Max(1, evt.newValue); // Ensure minimum value of 1
                });
                field.style.marginBottom = 5;
            });

            E<Toggle>(toggle =>
            {
                toggle.label = "Auto Record Snapshots";
                toggle.value = RecordSnapshots;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    RecordSnapshots = evt.newValue;
                });
                toggle.style.marginBottom = 5;
            });

            E<EnumFlagsField>(field =>
            {
                field.label = "Record Snapshots On";
                field.Init(RecordOnStatusChangeFlags);
                field.value = RecordOnStatusChangeFlags;
                field.RegisterValueChangedCallback(evt =>
                {
                    RecordOnStatusChangeFlags = (NodeStatusChangeFlags)evt.newValue;
                });
                field.style.marginTop = 10;
                field.style.marginBottom = 5;
            });
        });

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new Preferences("Preferences/ClosureAI", SettingsScope.User);
        }

        public static string Key(string str) => PREF_KEY + "_" + str;
    }

    [Flags]
    public enum NodeStatusChangeFlags
    {
        None = 0,
        Entering = 1 << 0,
        Running = 1 << 1,
        Exiting = 1 << 2,
        Succeeding = 1 << 3,
        Success = 1 << 4,
        Failing = 1 << 5,
        Failed = 1 << 6,
        Done = 1 << 7,
        ToNone = 1 << 8,
        Succeeded = 1 << 9,
    }
}

#endif