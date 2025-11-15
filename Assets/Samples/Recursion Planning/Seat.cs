using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureAI.Samples.Shared
{
    public class Seat : MonoBehaviour
    {
        public static readonly List<Seat> Instances = new();
        public Pawn Occupant;
        public Pawn ReservedBy;

        private void OnEnable() => Instances.Add(this);
        private void OnDisable() => Instances.Remove(this);

        public static Seat Find(Func<Seat, bool> predicate)
        {
            foreach (var seat in Instances)
            {
                if (predicate(seat))
                    return seat;
            }

            return null;
        }

        public static Seat Find<T>(T binder, Func<T, Seat, bool> predicate)
        {
            foreach (var seat in Instances)
            {
                if (predicate(binder, seat))
                    return seat;
            }

            return null;
        }
    }
}
