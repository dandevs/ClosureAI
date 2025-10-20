using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI.Samples.ContextAI
{
    public class ContextSampleCamera : MonoBehaviour
    {
        public Camera Camera;
        public Transform TargetTransform;
        public Vector3 TargetPosition;
        public float TargetOrthoSize;

        public Vector3 RotationEuler;
        public Vector3 Offset;

        private void Awake()
        {
            TargetOrthoSize = Camera.orthographicSize;
            TargetPosition = transform.position;
        }

        private void Update()
        {
            if (TargetTransform)
                TargetPosition = TargetTransform.position;

            var camTargetPosition = transform.position + Quaternion.Euler(RotationEuler) * Offset;
            Camera.transform.position = camTargetPosition;

            // Lerp to Target Position
            transform.position = Vector3.Lerp(transform.position, TargetPosition, Time.deltaTime * 3f);
            Camera.transform.LookAt(transform.position);

            // Lerp orthoZoom
            Camera.orthographicSize = Mathf.Lerp(Camera.orthographicSize, TargetOrthoSize, Time.deltaTime * 3f);
        }
    }
}
