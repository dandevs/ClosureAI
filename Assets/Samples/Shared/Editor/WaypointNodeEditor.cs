using UnityEngine;
using UnityEditor;

namespace ClosureAI.Samples.Editor
{
    [CustomEditor(typeof(WaypointNode))]
    public class WaypointNodeEditor : UnityEditor.Editor
    {
        private Vector3 lastPosition;
        private WaypointNode waypointNode;

        private void OnEnable()
        {
            waypointNode = (WaypointNode)target;
            lastPosition = waypointNode.transform.position;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Check if position has changed
            if (waypointNode.transform.position != lastPosition)
            {
                lastPosition = waypointNode.transform.position;
                WaypointNode.UpdateAll();
            }
        }

        private void OnSceneGUI()
        {
            if (waypointNode.transform.position != lastPosition)
            {
                Undo.RecordObject(waypointNode.transform, "Move Waypoint (external)");
                lastPosition = waypointNode.transform.position;
                WaypointNode.UpdateAll();
                EditorUtility.SetDirty(waypointNode);
            }
        }
    }
}
