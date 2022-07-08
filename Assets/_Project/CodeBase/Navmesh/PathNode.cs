using UnityEngine;

namespace _Project.CodeBase.Navmesh
{
    public class PathNode
    {
        public readonly Vector2Int gridPos;
        public readonly float movementCost;
        public readonly float heuristicCost;
        public readonly PathNode parent;

        public PathNode(Vector2Int gridPos, float movementCost, float heuristicCost, PathNode parent)
        {
            this.gridPos = gridPos;
            this.movementCost = movementCost;
            this.heuristicCost = heuristicCost;
            this.parent = parent;
        }
    }
}