using UnityEngine;

namespace _Project.CodeBase.Navmesh
{
    public struct NavmeshRegion
    {
        public Vector2 worldPos;
        public Vector2Int gridPos;
        public int numChunksInDim;
    }
}