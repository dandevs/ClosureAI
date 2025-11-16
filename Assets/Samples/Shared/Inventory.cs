#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClosureAI.Samples
{
    /// <summary>
    /// A very barebones Inventory system
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class Inventory : MonoBehaviour
    {
        public List<string> Items;

        public event Action OnChange = delegate {};

        private void OnEnable() => OnChange += OnChangeListener;
        private void OnDisable() => OnChange -= OnChangeListener;

        private void OnChangeListener()
        {
            if (DestroyOnEmpty && Items.Count == 0)
                Destroy(gameObject);
        }

        public bool RemoveOnTake = true;
        public bool DestroyOnEmpty = false;

        public bool Remove(string itemID)
        {
            if (RemoveOnTake)
            {
                Items.Remove(itemID);
                OnChange();
            }

            return true;
        }

        public bool Add(string itemID, int amount = 1)
        {
            Items.Add(itemID);
            OnChange();
            return true;
        }

        public int Count(string itemID) => Items.Count(item => item == itemID);
        public bool Contains(string itemID) => Items.Contains(itemID);
    }
}
#endif
