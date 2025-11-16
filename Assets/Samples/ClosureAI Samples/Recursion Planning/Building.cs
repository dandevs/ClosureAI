#if UNITASK_INSTALLED
using System.Collections.Generic;
using UnityEngine;

namespace ClosureAI.Samples.Shared
{
    public class Building : MonoBehaviour
    {
        public static readonly List<Building> Instances = new();
        public string ID;

        private void OnEnable() => Instances.Add(this);
        private void OnDisable() => Instances.Remove(this);

        public static Building FindByID(string id)
        {
            return Instances.Find(b => b.ID == id);
        }
    }
}
#endif
