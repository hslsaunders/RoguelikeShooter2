#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using _Project.CodeBase.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.CodeBase.Navmesh
{
    public class NavmeshManager : GameService<NavmeshManager>
    {
        [field: SerializeField] public Tilemap Tilemap { get; private set; }
        [SerializeField] private int numChunksInRegionDim;
        [field: SerializeField] public int NumTilesInChunkDim { get; private set; }
        [SerializeField] private int neighborLoadRange;
        [SerializeField] private bool _debugChunks;
        [SerializeField] private bool _debugRegions;
        [SerializeField] private bool _debugNodes;
        [Range(0, 5)] [SerializeField] private int _debugNodeNeighborCount;
        [SerializeField] private bool _showOnlyWalkable;
        //[SerializeField] private Vector2Int _pathfindTestStart;
        //[SerializeField] private Vector2Int _pathfindTestEnd;

        public bool testPathfinder;
        public Vector2Int testPathFindStart;
        public Vector2Int testPathFindEnd;
        public bool isGroundUnit;
        //public float heuristicMultipler = 1f;

        private Dictionary<Vector2Int, NavmeshRegion> _regions = new Dictionary<Vector2Int, NavmeshRegion>();
        private Dictionary<Vector2Int, NavmeshChunk> _chunks = new Dictionary<Vector2Int, NavmeshChunk>();

        public float RegionSize { get; private set; }
        public float ChunkSize { get; private set; }
        public float NodeSize { get; private set; }
        private PlayerManager _player;
        public Vector2 NodeDimensions { get; private set; }
        public Vector2 NodeExtents { get; private set; }
        private PathFinder _testPathFinder;

        private void Start()
        {
            _player = PlayerManager.Singleton;
            GenerateRegions();
        }

        private void Update()
        {
            if (testPathfinder && (_testPathFinder == null || _testPathFinder.Start != testPathFindStart ||
                _testPathFinder.End != testPathFindEnd))// || Math.Abs(_testPathFinder.heuristicMultipler - heuristicMultipler) > .001f))
            {
                _testPathFinder = new PathFinder(testPathFindStart, testPathFindEnd, isGroundUnit, null);
                //_testPathFinder.heuristicMultipler = heuristicMultipler;
            }

            foreach (NavmeshChunk chunk in _chunks.Values)
            {
                chunk.Tick();
            }

            //Vector2Int gridPos = GetNodeGridPos(Utils.WorldMousePos);
            //Debug.Log(gridPos + " " + GetChunkGridPosFromNodeGridPos(gridPos));
        }

        public void GenerateRegions()
        {
            NodeSize = Tilemap.cellSize.x;
            ChunkSize = NumTilesInChunkDim * NodeSize;
            RegionSize = numChunksInRegionDim * ChunkSize;
            NodeDimensions = new Vector2(NodeSize, NodeSize);
            NodeExtents = new Vector2(NodeSize / 2f, NodeSize / 2f);

            NavmeshRegion startingRegion = GenRegion(_player.entity.transform.position);

            _regions[startingRegion.gridPos] = startingRegion;

            for (int x = -neighborLoadRange; x <= neighborLoadRange; x++)
            for (int y = -neighborLoadRange; y <= neighborLoadRange; y++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                if (offset == Vector2Int.zero) continue;

                Vector2Int gridPos = startingRegion.gridPos + offset;
                _regions[gridPos] = GenRegion(gridPos);
            }
        }

        private NavmeshRegion GenRegion(Vector2 pos)
        {
            Vector2Int gridPos = GetRegionGridPos(pos);
            return GenRegion(gridPos);
        }

        private NavmeshRegion GenRegion(Vector2Int gridPos)
        {
            Vector2 worldPos = GetRegionCenter(gridPos);

            NavmeshRegion newRegion = new NavmeshRegion
            {
                gridPos = gridPos,
                worldPos = worldPos,
                numChunksInDim = numChunksInRegionDim
            };

            Vector2 chunkExtents = new Vector2(ChunkSize / 2f, ChunkSize / 2f);
            Vector2 regionTopLeft = new Vector2(-RegionSize / 2f, RegionSize / 2f) + worldPos;
            for (int x = 0; x < numChunksInRegionDim; x++)
            for (int y = 0; y < numChunksInRegionDim; y++)
            {
                Vector2 globalChunkPos = regionTopLeft + chunkExtents.SetY(-chunkExtents.y)
                                                       + new Vector2(x * ChunkSize, -y * ChunkSize);
                Vector2Int chunkGridPos = GetChunkGridPos(globalChunkPos);

                NavmeshChunk newChunk = new NavmeshChunk(chunkGridPos, globalChunkPos, NumTilesInChunkDim,
                    this);
                _chunks.Add(chunkGridPos, newChunk);
            }

            return newRegion;
        }

        public Vector2Int GetRegionGridPos(Vector2 pos) => (pos / RegionSize).FloorVector();
        public Vector2Int GetChunkGridPos(Vector2 pos) => (pos / ChunkSize).FloorVector();
        public NavmeshRegion GetRegion(Vector2 pos) => _regions[GetRegionGridPos(pos)];
        public Vector2Int GetNodeGridPos(Vector2 pos) => (pos / NodeSize).FloorVector();
        public bool TryGetNodeAtWorldPos(Vector2 pos, out NavmeshNode node)
        {
            node = null;
            if (TryGetChunkFromWorldPos(pos, out NavmeshChunk chunk))
            {
                node = chunk.GetNode(pos);
                return true;
            }

            return false;
        }

        public bool TryGetNodeAtGridPos(Vector2Int pos, out NavmeshNode node)
        {
            node = null;
            if (TryGetChunkFromNodePos(pos, out NavmeshChunk chunk))
            {
                //Debug.Log($"GridPos{pos}, chunk: {chunk.GridPos}");
                node = chunk.GetNode(pos);
                return true;
            }

            return false;
        }

        
        public NavmeshNode GetNodeAtGridPos(Vector2Int pos)
        {
            NavmeshNode node = null;
            if (TryGetChunkFromNodePos(pos, out NavmeshChunk chunk))
            {
                //Debug.Log($"GridPos{pos}, chunk: {chunk.GridPos}");
                node = chunk.GetNode(pos);
            }

            return node;
        }

        public bool TryGetChunkFromWorldPos(Vector2 pos, out NavmeshChunk chunk)
        {
            return TryGetChunkFromChunkPos(GetChunkGridPos(pos), out chunk);
        }

        public bool TryGetChunkFromNodePos(Vector2Int pos, out NavmeshChunk chunk)
        {
            // Debug.Log(pos + " " + GetChunkGridPosFromNodeGridPos(pos));
            return TryGetChunkFromChunkPos(GetChunkGridPosFromNodeGridPos(pos), out chunk);
        }

        public bool TryGetChunkFromChunkPos(Vector2Int pos, out NavmeshChunk chunk) => 
            _chunks.TryGetValue(pos, out chunk);

        private Vector2 GetRegionCenter(Vector2Int pos) => (Vector2)pos * RegionSize + 
                                                           new Vector2(RegionSize / 2f, RegionSize / 2f);

        public bool IsValidNavmeshPos(Vector2Int pos) => TryGetChunkFromNodePos(pos, out NavmeshChunk chunk);

        public bool IsWalkableAtPos(Vector2Int pos)
        {
            NavmeshNode node = GetNodeAtGridPos(pos);
            //Debug.Log($"pos: {pos}, groundWalkable: {node.groundWalkable}");
            return node != null && node.groundWalkable;
        }

        public bool HasTileAtPos(Vector2Int pos)
        {
            NavmeshNode node = GetNodeAtGridPos(pos);
            //Debug.Log($"pos: {pos}, groundWalkable: {node.groundWalkable}");
            return node != null && node.hasTile;
        }

        public Vector2Int GetChunkGridPosFromNodeGridPos(Vector2Int pos) => 
            ((Vector2)pos / NumTilesInChunkDim).FloorVector();
        public Vector2 NodePosToWorldPos(Vector2Int pos) => (Vector2)pos * NodeSize + NodeExtents;

        public void StepPathFinder()
        {
            _testPathFinder.stepFind = true;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            GUIStyle style = new GUIStyle {alignment = TextAnchor.MiddleCenter};
            if (_debugRegions)
            {
                DebugRegions(style);
            }

            if (_debugChunks)
            {
                DebugChunks(style);
            }

            if (_debugNodes)
            {
                DebugNodes(style);
            }

            if (!testPathfinder) return;
            
            Handles.color = Color.magenta;
            Handles.DrawWireCube(NodePosToWorldPos(testPathFindStart), NodeDimensions);
            Handles.DrawWireCube(NodePosToWorldPos(testPathFindEnd), NodeDimensions);

            if (_testPathFinder == null)
            {
                Debug.Log("Path Finder is null");
                return;
            }
            if (_testPathFinder.Path == null)
            {
                Debug.Log("Path is null");
                return;
            }
            
            for (int i = 1; i < _testPathFinder.Path.Count; i++)
            {
                Handles.DrawLine(NodePosToWorldPos(_testPathFinder.Path[i - 1]), 
                    NodePosToWorldPos(_testPathFinder.Path[i]));
            }
        }

        private void DebugRegions(GUIStyle style)
        {
            Handles.color = Color.red;
            foreach ((Vector2Int pos, NavmeshRegion region) in _regions)
            {
                Handles.DrawWireCube(region.worldPos, new Vector3(RegionSize, RegionSize));

                Handles.Label(region.worldPos, $"{pos}", style);
            }
        }

        private void DebugChunks(GUIStyle style)
        {
            Handles.color = Color.green;

            foreach ((Vector2Int pos, NavmeshChunk chunk) in _chunks)
            {
                Handles.DrawWireCube(chunk.CenterPos, new Vector3(ChunkSize, ChunkSize));
                Handles.Label(chunk.CenterPos, $"{pos}", style);
            }
        }

        private void DebugNodes(GUIStyle style)
        {
            for (int x = -_debugNodeNeighborCount; x <= _debugNodeNeighborCount; x++)
            for (int y = -_debugNodeNeighborCount; y <= _debugNodeNeighborCount; y++)
            {
                if (TryGetChunkFromChunkPos(GetChunkGridPos(Utils.WorldMousePos) + new Vector2Int(x, y), out NavmeshChunk chunk))
                {
                    int index = 0;
                    foreach (NavmeshNode node in chunk.nodes)
                    {
                        index++;
                        if (!node.groundWalkable && _showOnlyWalkable) continue;
                        Vector2 worldPos = NodePosToWorldPos(node.gridPos);
                        if (index == 0 || index == chunk.nodes.Length * chunk.nodes.Length - 1)
                            Handles.Label(worldPos, $"{node.gridPos}", style);

                        Handles.color = node.groundWalkable ? Color.yellow : Color.red;

                        //if (node.hasTile)
                        //    Handles.DrawWireDisc(worldPos, Vector3.back, NodeSize / 2f);
                        
                        Handles.DrawWireCube(worldPos, new Vector3(NodeSize, NodeSize));
                    }
                }
            }
        }
#endif
    }
}