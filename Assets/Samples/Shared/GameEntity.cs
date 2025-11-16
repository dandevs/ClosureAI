#if UNITASK_INSTALLED
using System;
using NUnit.Framework.Constraints;
using UnityEngine;

namespace ClosureAI.Samples
{
    [SelectionBase]
    public class GameEntity : MonoBehaviour
    {
        public GameEntityTag Tag;
    }

    [Flags]
    public enum GameEntityTag
    {
        Player = 1 << 0,
        Seeker = 1 << 1,
        Entity = 1 << 2,
    }
}
#endif
