using UnityEngine;

namespace _Project.CodeBase.Navmesh
{
    public class NavmeshNode
    {
        public Vector2Int gridPos;
        public bool walkable;

        public Vector2Int Down => gridPos + new Vector2Int(0, -1);
        public Vector2Int Up => gridPos + new Vector2Int(0, 1);
        public Vector2Int Left => gridPos + new Vector2Int(-1, 0);
        public Vector2Int Right => gridPos + new Vector2Int(1, 0);
    }
}