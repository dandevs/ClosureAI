#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ClosureBT.Editor
{
    public interface IHistoryTracked
    {
        public int CurrentHistoryFrame { get; }
        public bool IsShowingHistory { get; }
        public int MaxHistoryLength { get; set; }

        public void ShowHistory(int frame);
        public void StopShowingHistory();
        public void SaveHistory();
        public void ClearHistory();
    }

    public abstract class HistoryTrackerBase<TOwner, TSerializedHistory> : IHistoryTracked
    {
        protected TOwner Owner { get; set; }
        public int CurrentHistoryFrame { get; private set; } = -1;
        public bool IsShowingHistory { get; private set; }
        protected List<TSerializedHistory> ValueHistory { get; } = new();
        public bool AutoStopHistoryOnEditorUnpause;

        private int _maxHistoryLength = 150;
        public int MaxHistoryLength
        {
            get => _maxHistoryLength;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Max length must be greater than or equal to 0.");

                if (_maxHistoryLength == value)
                    return;

                var previousLength = _maxHistoryLength;
                _maxHistoryLength = value;
                OnMaxHistoryLengthChanged(previousLength);
            }
        }

        protected HistoryTrackerBase(TOwner owner)
        {
            Owner = owner;
        }

        protected virtual void OnShowHistory(int frame) {}
        protected virtual void OnMaxHistoryLengthChanged(int previousLength)  {}
        protected virtual void OnStopShowingHistory() {}
        protected virtual void OnClearHistory() {}
        protected virtual void OnBeginShowHistory(int frame) {}
        protected abstract TSerializedHistory OnSaveHistory();

        public void ShowHistory(int frame)
        {
            if (frame >= 0)
            {
                if (!IsShowingHistory)
                {
                    IsShowingHistory = true;
                    OnBeginShowHistory(frame);

                    if (AutoStopHistoryOnEditorUnpause)
                    {
                        void Resume(PauseState state)
                        {
                            EditorApplication.pauseStateChanged -= Resume;
                            StopShowingHistory();
                        }

                        EditorApplication.pauseStateChanged += Resume;
                    }
                }

                CurrentHistoryFrame = frame;
                OnShowHistory(frame);
            }
        }

        public void ShowIncrementHistory(int amount)
        {
            ShowHistory(CurrentHistoryFrame + amount);
        }

        public void StopShowingHistory()
        {
            if (IsShowingHistory)
            {
                IsShowingHistory = false;
                OnStopShowingHistory();
                CurrentHistoryFrame = -1;
            }
        }

        public void SaveHistory()
        {
            if (MaxHistoryLength == 0)
                return;

            if (IsShowingHistory)
                Debug.LogWarning("Saving history while showing history, may cause issues.");

            ValueHistory.Insert(0, OnSaveHistory());

            if (ValueHistory.Count > MaxHistoryLength)
                ValueHistory.RemoveAt(ValueHistory.Count - 1);
        }

        public void ClearHistory()
        {
            ValueHistory.Clear();
            OnClearHistory();
        }
    }
}

#endif
