#if UNITASK_INSTALLED
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ClosureBT.Samples.Shared
{
    public class Placement : MonoBehaviour
    {
        public static readonly List<Placement> Instances = new();

        [SerializeField] private UnityEvent OnPlacement;
        [field: SerializeField] public bool Done { get; private set; }

        public string ID;

        private void OnEnable() => Instances.Add(this);
        private void OnDisable() => Instances.Remove(this);

        public void NotifyPlacementSuccess()
        {
            if (!Done)
            {
                Done = true;
                OnPlacement.Invoke();
                enabled = false;
            }
            else
                Debug.LogWarning($"Placement {ID} has already been completed.");
        }

        public static Placement FindByID(string id)
        {
            return Instances.Find(p => p.ID == id);
        }
    }
}
#endif
