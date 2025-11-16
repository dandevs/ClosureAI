using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using static ClosureAI.AI;

namespace ClosureAI.Samples.MemoryGame
{
    public class AsyncAndInterruptionExample : MonoBehaviour
    {
        public Node AI;
        private string _text = null;

        private void Update() => AI.Tick();
        private void OnDestroy() => AI.ResetImmediately();

        private void Awake() => AI = Reactive * SequenceAlways(() =>
        {
            Leaf("An Example", () =>
            {
                OnEnter(async (ct) =>
                {
                    // You can use async/await inside lifecycle hooks
                    await UniTask.WaitForSeconds(0.25f, cancellationToken: ct);
                });

                OnBaseTick(async (ct, tick) =>
                {
                    for (var i = 0; i < 10; i++)
                        await tick();

                    return Status.Success;
                });

                OnExit(async (ct, tick) =>
                {
                    // You can also await for a tick to occur.
                    for (var i = 0; i < 10; i++)
                    {
                        await tick();
                    }
                });
            });

            //------------------------------------------------------------------

            D.Condition("Holding F", () => Keyboard.current != null && Keyboard.current.fKey.isPressed);
            Sequence(() =>
            {
                Wait(0.25f);
                Wait(0.25f);
                Wait(0.25f);
            });

            // D.Condition("Holding Space", () => Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
            _ = Reactive * Sequence(() =>
            {
                Condition("Holding Space", () => Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
                Wait(0.25f);
                Wait(0.25f);
                Wait(0.25f);
            });

            //------------------------------------------------------------------

            Leaf("Async Cleanup", () =>
            {
                OnTick(async (ct, tick) =>
                {
                    // The try-finally pattern ensures cleanup/finalization code ALWAYS runs,
                    // even when the node is cancelled or interrupted by the behavior tree.
                    //
                    // Why this pattern is important:
                    // 1. When a parent node (like SequenceAlways) is reset or a condition fails,
                    //    child nodes are cancelled via their CancellationToken.
                    // 2. The 'await' will throw OperationCanceledException when ct is cancelled.
                    // 3. The 'finally' block executes regardless of how we exit (normal, exception, or cancellation).
                    // 4. This guarantees cleanup logic runs even if the behavior tree structure changes mid-execution.
                    //
                    // Without try-finally, resources could leak or UI state could be left inconsistent
                    // when the tree is interrupted (e.g., when the Space condition in Sequence fails).
                    try
                    {
                        _text = "Hold Space!";
                        await UniTask.WaitForSeconds(999999f, cancellationToken: ct); // Simulate long-running task
                    }
                    finally
                    {
                        // Cleanup always runs: when task completes, is cancelled, or throws exception
                        _text = null;
                    }
                });

                // This is basically just a JustRunning node
                OnBaseTick(() => Status.Running);
            });

            JustRunning(); // Never end
        });

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(_text))
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 32
                };

                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), _text, style);
            }
        }
    }
}
