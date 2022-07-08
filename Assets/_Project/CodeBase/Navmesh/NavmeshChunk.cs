using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase.Navmesh
{
    public class NavmeshChunk
    {
        public int TilesInDim { get; private set; }
        public Vector2Int GridPos { get; private set; }
        public Vector2 CenterPos { get; private set; }
        public NavmeshNode[,] nodes;
        private NavmeshManager _manager;
        private Vector2Int _bottomLeftNode;
        
        public NavmeshChunk(Vector2Int gridPos, Vector2 centerPos, int tilesInDim, 
            NavmeshManager manager)
        {
            GridPos = gridPos;
            TilesInDim = tilesInDim;
            CenterPos = centerPos;
            _manager = manager;

            GenerateNodes();
        }

        public NavmeshNode GetNode(Vector2 pos) => GetNode(_manager.GetNodeGridPos(pos));
        
        public NavmeshNode GetNode(Vector2Int pos) => nodes[pos.x - _bottomLeftNode.x, pos.y - _bottomLeftNode.y];

        public void GenerateNodes()
        {
            nodes = new NavmeshNode[_manager.NumTilesInChunkDim, _manager.NumTilesInChunkDim];
            
            Vector2 bottomRightOfChunk = CenterPos - new Vector2(_manager.ChunkSize / 2f, _manager.ChunkSize / 2f);
            _bottomLeftNode = _manager.GetNodeGridPos(bottomRightOfChunk);
            
            //bool[,] positionsWithTile = new bool[_manager.NumTilesInChunkDim,_manager.NumTilesInChunkDim];
            
            for (int x = 0; x < _manager.NumTilesInChunkDim; x++)
            for (int y = 0; y < _manager.NumTilesInChunkDim; y++)
            {
                Vector2Int gridPos = _bottomLeftNode + new Vector2Int(x, y);
                Vector2Int gridPosBelow = gridPos + new Vector2Int(0, -1);
                bool groundIsBelow = _manager.Tilemap.HasTile((Vector3Int)gridPosBelow);
                bool tileAtPos = _manager.Tilemap.HasTile((Vector3Int) gridPos);
                
                bool walkable = groundIsBelow && !tileAtPos;
                NavmeshNode node = new NavmeshNode
                {
                    gridPos = gridPos,
                    groundWalkable = walkable,
                    hasTile = tileAtPos
                };
                nodes[x, y] = node;
                
            }
        }

        public void Tick()
        {
            //Vector2 bottomLeft = _manager.NodePosToWorldPos(_bottomLeftNode);
           // Debug.DrawLine(bottomLeft - _manager.NodeExtents, bottomLeft + _manager.NodeExtents, Color.red);
        }
    }
}