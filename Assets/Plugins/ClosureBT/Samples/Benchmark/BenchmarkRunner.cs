using UnityEngine;
using UnityEngine.UIElements;

namespace ClosureBT.Samples.Benchmark
{
    /// <summary>
    /// MonoBehaviour coordinator for the benchmark system
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BenchmarkRunner : MonoBehaviour
    {
        private BenchmarkController _controller;
        private BenchmarkUIView _uiView;

        private void Awake()
        {
            // Create controller
            _controller = new BenchmarkController();

            // Get UI Document root
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            // Initialize UI view
            _uiView = new BenchmarkUIView(root, _controller);
        }

        private void Update()
        {
            // Tick the controller every frame
            _controller?.Tick();
        }

        private void OnDestroy()
        {
            // Cleanup nodes
            _controller?.Cleanup();
        }
    }
}
