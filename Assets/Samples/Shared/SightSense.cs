using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureAI.Samples
{
    public class SightSense : MonoBehaviour
    {
        private static readonly Collider[] _colliders = new Collider[50];

        // Static comparer for non-alloc sorting by distance
        private static readonly EntityDistanceComparer _distanceComparer = new();

        public List<GameEntity> Entities = new();
        public GameEntityTag TagFilter;
        public float Range = 5f;
        public float MaxConeRange = 45f;
        public Transform ConeOrigin;

        private void Update()
        {
            FindWithComponent(Entities);
            KeepWhere(Entities, IsInsideCone);
            KeepWhere(Entities, HasMatchingTag);

            // Sort entities by distance to this transform (non-alloc)
            _distanceComparer.Origin = transform.position;
            Entities.Sort(_distanceComparer);
        }

        private T GetFirstOfComponentType<T>() where T : Component
        {
            foreach (var entity in Entities)
            {
                if (entity.TryGetComponent<T>(out var result))
                    return result;
            }

            return null;
        }

        private void FindWithComponent<T>(List<T> result) where T : Component
        {
            result.Clear();

            // Use the non-alloc version to avoid generating garbage
            var found = Physics.OverlapSphereNonAlloc(transform.position, Range, _colliders);

            if (found == _colliders.Length)
                Debug.LogWarning($"Sight buffer of size {_colliders.Length} filled. Some results may be missing. Consider increasing the buffer size.", this);

            for (var i = 0; i < found; i++)
            {
                var col = _colliders[i];

                if (col.gameObject == gameObject)
                    continue;

                if (col && col.TryGetComponent<T>(out var comp))
                {
                    if (!result.Contains(comp))
                        result.Add(comp);
                }
            }
        }


        private static void KeepWhere<T>(List<T> list, Func<T, bool> predicate)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (!predicate(list[i]))
                    list.RemoveAt(i);
            }
        }

        private bool IsInsideCone(GameEntity entity)
        {
            var coneOrigin = ConeOrigin ? ConeOrigin : transform;
            var conePosition = new Vector3(coneOrigin.position.x, 0, coneOrigin.position.z);
            var entityPosition = new Vector3(entity.transform.position.x, 0, entity.transform.position.z);

            // Calculate direction from cone origin to entity in XZ plane
            var directionToEntity = (entityPosition - conePosition).normalized;
            var coneForward = new Vector3(coneOrigin.forward.x, 0, coneOrigin.forward.z).normalized;

            // Calculate angle between cone forward and direction to entity
            var angle = Vector3.Angle(coneForward, directionToEntity);

            // Check if angle is within cone half-angle
            return angle <= MaxConeRange * 0.5f;
        }

        private bool HasMatchingTag(GameEntity entity)
        {
            return (entity.Tag & TagFilter) != 0;
        }

        // Only draw gizmos when the object is selected
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Range);
            DrawConeGizmo();
        }

        private void DrawConeGizmo()
        {
            var coneOrigin = ConeOrigin ? ConeOrigin : transform;
            var conePosition = coneOrigin.position;
            var coneForward = coneOrigin.forward;

            Gizmos.color = Color.red;
            var halfAngle = MaxConeRange * 0.5f;
            var segments = 10;

            var prevEndPoint = Vector3.zero;

            // Draw arc for cone visualization
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                var direction = rotation * new Vector3(coneForward.x, 0, coneForward.z).normalized;
                var endPoint = conePosition + direction * Range;

                // Draw arc segments (outer edge only)
                if (i > 0)
                {
                    Gizmos.DrawLine(prevEndPoint, endPoint);
                }

                prevEndPoint = endPoint;
            }

            // Draw the two outer edge lines from cone origin to arc endpoints
            var leftAngle = Mathf.Lerp(-halfAngle, halfAngle, 0f);
            var rightAngle = Mathf.Lerp(-halfAngle, halfAngle, 1f);
            var leftRotation = Quaternion.AngleAxis(leftAngle, Vector3.up);
            var rightRotation = Quaternion.AngleAxis(rightAngle, Vector3.up);
            var leftDirection = leftRotation * new Vector3(coneForward.x, 0, coneForward.z).normalized;
            var rightDirection = rightRotation * new Vector3(coneForward.x, 0, coneForward.z).normalized;
            var leftEndPoint = conePosition + leftDirection * Range;
            var rightEndPoint = conePosition + rightDirection * Range;

            Gizmos.DrawLine(conePosition, leftEndPoint);
            Gizmos.DrawLine(conePosition, rightEndPoint);
        }

        // Non-alloc comparer for sorting entities by distance
        private class EntityDistanceComparer : IComparer<GameEntity>
        {
            public Vector3 Origin;

            public int Compare(GameEntity a, GameEntity b)
            {
                var da = (a.transform.position - Origin).sqrMagnitude;
                var db = (b.transform.position - Origin).sqrMagnitude;
                return da.CompareTo(db);
            }
        }
    }
}
