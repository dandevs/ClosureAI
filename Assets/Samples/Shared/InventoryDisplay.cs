using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ClosureAI.Samples
{
    public class InventoryDisplay : MonoBehaviour
    {
        public Vector3 Axis = Vector3.up;
        public Vector3 Offset = Vector3.zero;
        public float Spacing = 0.5f;
        public Vector3 BoundingBoxSize = Vector3.one;
        public float UniformScale = 0.5f;
        public Inventory Inventory;

        private readonly List<GameObject> _displayObjects = new();
        private readonly ConditionalWeakTable<GameObject, string> _objectToID = new();

        private void OnEnable() => Inventory.OnChange += OnInventoryChange;
        private void OnDisable() => Inventory.OnChange -= OnInventoryChange;

        public void OnInventoryChange()
        {
            for (var i = 0; i < _displayObjects.Count; i++)
                Destroy(_displayObjects[i]);

            _displayObjects.Clear();

            for (var i = 0; i < Inventory.Items.Count; i++)
            {
                var itemID = Inventory.Items[i];
                var entry = ItemDatabase.GetFromID(itemID);

                if (entry == null)
                {
                    Debug.LogWarning($"No recipe found for item ID '{itemID}'.");
                    continue;
                }

                var displayObject = Instantiate(entry.DisplayModel, transform);

                // Normalize the object size based on its bounding box
                NormalizeObjectSize(displayObject);

                displayObject.transform.localPosition = Offset + Axis * (i * Spacing);
                displayObject.transform.localRotation = Quaternion.identity;

                _displayObjects.Add(displayObject);
            }
        }

        private void NormalizeObjectSize(GameObject obj)
        {
            var renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            var bounds = renderer.bounds;
            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            if (maxDimension > 0)
            {
                var targetMaxDimension = Mathf.Min(BoundingBoxSize.x, BoundingBoxSize.y, BoundingBoxSize.z);
                var scale = (targetMaxDimension / maxDimension) * UniformScale;
                obj.transform.localScale = Vector3.one * scale;
            }
        }
    }
}
