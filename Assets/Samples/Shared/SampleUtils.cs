using System;
using UnityEngine;

namespace ClosureAI.Samples
{
    public static class SampleUtils
    {
        public static T GetComponentFromScreen<T>(Camera camera, Vector3 pointerPosition)
        {
            var ray = camera.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out var hit))
                return hit.collider.GetComponentInChildren<T>();
            else
                return default;
        }

        public static bool RaycastFromCamera(Camera camera, Vector3 pointerPosition, out Vector3 position)
        {
            var ray = camera.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out var hit))
            {
                position = hit.point;
                return true;
            }
            else
            {
                position = default;
                return false;
            }
        }

        public static Vector3 PlaneCastMouse(Camera camera, Vector3 point)
        {
            var plane = new Plane(Vector3.up, point);
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : default;
        }

        public static Action OnTriggerEnterHook(GameObject go, Action<Collider> onEvent)
        {
            if (!go.TryGetComponent<OnTriggerEnterHookBehaviour>(out var hook))
                hook = go.AddComponent<OnTriggerEnterHookBehaviour>();

            hook.OnEvent += onEvent;
            return () => hook.OnEvent -= onEvent;
        }
    }

    public class OnTriggerEnterHookBehaviour : MonoBehaviour
    {
        public Action<Collider> OnEvent;
        private void OnTriggerEnter(Collider other) => OnEvent?.Invoke(other);
    }
}
