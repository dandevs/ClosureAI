#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT.Samples.Shared
{
    public class Harvestable : MonoBehaviour
    {
        public static readonly List<Harvestable> Instances = new();
        public bool DestroyOnEmpty = true;
        public string ItemID;
        public int Amount = 1;
        public string RequiredItem;

        public event Action OnHarvest;

        private void OnEnable() => Instances.Add(this);
        private void OnDisable() => Instances.Remove(this);

        public bool Harvest()
        {
            if (Amount > 0)
            {
                Amount--;
                OnHarvest?.Invoke();
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (Amount <= 0 && DestroyOnEmpty)
                Destroy(gameObject);
        }

        public static Harvestable FindByID(string itemID)
        {
            var result = Instances.Find(h => h.ItemID == itemID);

            if (!result)
                Debug.LogWarning($"Harvestable with ID '{itemID}' not found in the scene.");

            return result;
        }
    }
}
#endif
