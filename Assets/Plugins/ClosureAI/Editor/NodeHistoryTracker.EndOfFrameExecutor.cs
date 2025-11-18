#if UNITASK_INSTALLED
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT.Editor
{
    public static partial class NodeHistoryTracker
    {
        [DefaultExecutionOrder(int.MinValue)]
        public class EndOfFrameExecutor : MonoBehaviour
        {
            private readonly List<Action> Actions = new();

            private void Awake()
            {
                StartCoroutine(EOFRoutine());
            }

            private void OnEnable()
            {
                Snapshots.Clear();
                _currentGlobalSnapshotIndex = 0;
                // _delayCallScheduled = false;
                _originalData.Clear();
                _isShowingHistory = false;
            }

            private void OnDisable()
            {
                // _delayCallScheduled = false;
                Resume(); // Ensure we resume if component is disabled
            }

            public void Schedule(Action action)
            {
                Actions.Add(action);
            }

            private IEnumerator EOFRoutine()
            {
                var eof = new WaitForEndOfFrame();

                while (true)
                {
                    yield return eof;

                    foreach (var action in Actions)
                        action();

                    Actions.Clear();
                }
            }
        }
    }
}

#endif
