#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT.Samples
{
    [DefaultExecutionOrder(-10)]
    public class WaypointNode : MonoBehaviour
    {
        public static readonly List<WaypointNode> Instances = new();
        public List<WaypointNode> Neighbors = new();

        private static bool LineSegmentsIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float tolerance = 0.5f)
        {
            // Check if line segments (p1-p2) and (p3-p4) intersect in 3D space
            // We'll project to 2D by ignoring the Y component for ground-level waypoints
            var a1 = new Vector2(p1.x, p1.z);
            var a2 = new Vector2(p2.x, p2.z);
            var b1 = new Vector2(p3.x, p3.z);
            var b2 = new Vector2(p4.x, p4.z);

            var dir1 = a2 - a1;
            var dir2 = b2 - b1;

            var denominator = dir1.x * dir2.y - dir1.y * dir2.x;

            // Lines are parallel
            if (Mathf.Abs(denominator) < 0.0001f)
                return false;

            var diff = b1 - a1;
            var t = (diff.x * dir2.y - diff.y * dir2.x) / denominator;
            var u = (diff.x * dir1.y - diff.y * dir1.x) / denominator;

            // Check if intersection point is within both line segments
            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                // Calculate intersection point and check distance tolerance
                var intersection = a1 + t * dir1;
                var distToA1 = Vector2.Distance(intersection, a1);
                var distToA2 = Vector2.Distance(intersection, a2);
                var distToB1 = Vector2.Distance(intersection, b1);
                var distToB2 = Vector2.Distance(intersection, b2);

                // Don't consider it an intersection if it's too close to any endpoint
                return distToA1 > tolerance && distToA2 > tolerance &&
                       distToB1 > tolerance && distToB2 > tolerance;
            }

            return false;
        }

        private static bool IsVisible(Vector3 from, Vector3 to, Transform targetTransform = null)
        {
            // Returns true if there is an unobstructed line of sight from 'from' to 'to'
            var dir = to - from;
            var dist = dir.magnitude;
            if (dist <= Mathf.Epsilon)
                return true;

            var dirNorm = dir / dist;
            var hits = Physics.RaycastAll(from, dirNorm, dist, LayerMask.GetMask("Default"));

            foreach (var hit in hits)
            {
                // Ignore hits on GameEntity components
                if (hit.transform.TryGetComponent<GameEntity>(out _))
                    continue;

                // Ignore hits that belong to the target node (or its children)
                if (targetTransform != null && (hit.transform == targetTransform || hit.transform.IsChildOf(targetTransform)))
                    continue;

                // Anything else blocks visibility
                return false;
            }

            return true;
        }

        public void UpdateNeighbors()
        {
            Neighbors.Clear();
            var others = FindObjectsByType<WaypointNode>(FindObjectsSortMode.None);

            foreach (var other in others)
            {
                if (other == this)
                    continue;

                var direction = (other.transform.position - transform.position).normalized;
                var distance = Vector3.Distance(transform.position, other.transform.position);

                // Use helper for visibility check
                var isBlocked = !IsVisible(transform.position, other.transform.position, other.transform);

                // If not blocked by obstacles, check for intermediate waypoints
                if (!isBlocked)
                {
                    // Check if there's a closer waypoint node that should be connected instead
                    var hasIntermediateWaypoint = false;

                    foreach (var intermediate in others)
                    {
                        if (intermediate == this || intermediate == other)
                            continue;

                        // Check if the intermediate node is roughly on the line between this and other
                        var toIntermediate = intermediate.transform.position - transform.position;
                        var toOther = other.transform.position - transform.position;

                        // Project intermediate onto the line to other
                        var projectionLength = Vector3.Dot(toIntermediate, toOther.normalized);

                        // Only consider if intermediate is between this and other
                        if (projectionLength > 0 && projectionLength < toOther.magnitude)
                        {
                            var projectionPoint = transform.position + toOther.normalized * projectionLength;
                            var distanceToLine = Vector3.Distance(intermediate.transform.position, projectionPoint);

                            // If intermediate is close enough to the line (within a threshold)
                            // and is closer than the target, don't connect directly
                            if (distanceToLine < 1.5f && toIntermediate.magnitude < toOther.magnitude)
                            {
                                hasIntermediateWaypoint = true;
                                break;
                            }
                        }
                    }

                    // Check for line intersections with existing connections
                    if (!hasIntermediateWaypoint)
                    {
                        var hasIntersection = false;
                        foreach (var nodeA in others)
                        {
                            if (nodeA == this || nodeA == other)
                                continue;

                            foreach (var nodeB in nodeA.Neighbors)
                            {
                                if (!nodeB || nodeB == this || nodeB == other || nodeB == nodeA)
                                    continue;

                                // Check if our potential connection (this -> other) intersects with (nodeA -> nodeB)
                                if (LineSegmentsIntersect(transform.position, other.transform.position,
                                                        nodeA.transform.position, nodeB.transform.position))
                                {
                                    hasIntersection = true;
                                    break;
                                }
                            }
                            if (hasIntersection)
                                break;
                        }

                        if (!hasIntersection)
                            Neighbors.Add(other);
                    }
                }
            }
        }

        public static void UpdateAll()
        {
            var allNodes = FindObjectsByType<WaypointNode>(FindObjectsSortMode.None);

            foreach (var node in allNodes)
            {
                if (node)
                    node.UpdateNeighbors();
            }
        }

        private void OnEnable() => Instances.Add(this);
        private void OnDisable() => Instances.Remove(this);

        public WaypointNode GetNeighborInDirection(Vector3 direction)
        {
            if (Neighbors == null || Neighbors.Count == 0)
                return null;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return null;

            var dirNorm = direction.normalized;
            var bestDot = float.NegativeInfinity;
            WaypointNode best = null;

            foreach (var neighbor in Neighbors)
            {
                if (neighbor == null)
                    continue;

                var toNeighbor = neighbor.transform.position - transform.position;
                if (toNeighbor.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                var toNeighborNorm = toNeighbor.normalized;
                var dot = Vector3.Dot(dirNorm, toNeighborNorm);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = neighbor;
                }
            }

            return best;
        }

        public static WaypointNode GetNearest(Vector3 position)
        {
            WaypointNode best = null;
            var bestDistSqr = float.PositiveInfinity;
            var allNodes = FindObjectsByType<WaypointNode>(FindObjectsSortMode.None);

            foreach (var node in allNodes)
            {
                if (node == null)
                    continue;

                var distSqr = (node.transform.position - position).sqrMagnitude;

                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = node;
                }
            }

            return best;
        }

        public static WaypointNode GetNearestInDirection(Vector3 position, Vector3 forward)
        {
            // If forward is invalid, fall back to plain nearest
            if (forward.sqrMagnitude <= Mathf.Epsilon)
                return GetNearest(position);

            var forwardNorm = forward.normalized;
            WaypointNode best = null;
            var bestDot = float.NegativeInfinity;
            var bestDistSqr = float.PositiveInfinity;

            foreach (var node in Instances)
            {
                if (node == null)
                    continue;

                var toNode = node.transform.position - position;
                var dist = toNode.magnitude;
                if (dist <= Mathf.Epsilon)
                    continue;

                var dirToNode = toNode / dist;
                var dot = Vector3.Dot(forwardNorm, dirToNode);

                // Visibility check using helper
                if (!IsVisible(position, node.transform.position, node.transform))
                    continue;

                // Choose by highest dot; if dots are effectively equal, prefer closer node
                if (dot > bestDot || (Mathf.Approximately(dot, bestDot) && dist * dist < bestDistSqr))
                {
                    bestDot = dot;
                    best = node;
                    bestDistSqr = dist * dist;
                }
            }

            return best;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.2f);

            // Draw lines to other nodes
            foreach (var neighbor in Neighbors)
            {
                if (neighbor)
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}
#endif
