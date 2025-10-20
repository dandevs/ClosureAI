using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureAI.Samples
{
    [DefaultExecutionOrder(-1000)] // Ensure these items are added into the list first before other things start
    public class Item : MonoBehaviour
    {
        public static readonly List<Item> Instances = new();
        public string ID;

        private void OnEnable() => Instances.Add(this);
        private void OnDisable() => Instances.Remove(this);

        public static Item FindWithID(string id)
        {
            return Instances.Find(item => item.ID == id);
        }

        public static Item Find(Func<Item, bool> predicate)
        {
            foreach (var instance in Instances)
            {
                if (predicate(instance))
                    return instance;
            }

            return null;
        }

        public static Item FindNearest(Vector3 fromPosition, Func<Item, bool> predicate)
        {
            Item nearest = null;
            var nearestDistanceSqr = float.MaxValue;

            foreach (var instance in Instances)
            {
                if (!predicate(instance))
                    continue;

                var distanceSqr = (instance.transform.position - fromPosition).sqrMagnitude;

                if (distanceSqr < nearestDistanceSqr)
                {
                    nearest = instance;
                    nearestDistanceSqr = distanceSqr;
                }
            }

            return nearest;
        }
    }
}
