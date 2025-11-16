#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureAI.Samples
{
    [DefaultExecutionOrder(-1000)]
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }
        public List<ItemDatabaseEntry> Entries;

        private void Awake()
        {
            Instance = this;
        }

        public static ItemDatabaseEntry GetFromID(string id)
        {
            ItemDatabaseEntry entry = null;

            foreach (var e in Instance.Entries)
            {
                if (e.ID == id)
                    entry = e;
            }

            if (entry == null)
                Debug.LogWarning($"Recipe with ID '{id}' not found.");

            return entry;
        }
    }

    //***********************************************************************************

    [Serializable]
    public class ItemDatabaseEntry
    {
        public string ID;
        public int Value = 1;
        public float TimeToCraft = 2f;
        public bool Craftable;
        public string RequiredBuilding;
        public GameObject DisplayModel;
        public bool IsCookingIngredient;

        public List<string> RequiredItems;
    }
}
#endif
