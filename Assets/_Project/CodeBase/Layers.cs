using UnityEngine;

namespace _Project.CodeBase
{
    public static class Layers
    {
        public static LayerMask WorldMask = LayerMask.GetMask("World");
        public static LayerMask ProjectileMask = LayerMask.GetMask("World", "Entity");
    }
}