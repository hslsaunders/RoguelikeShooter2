using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _Project.CodeBase.Navmesh
{
    public class PathFinder
    {
        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }
        public bool isGroundUnit;
        public List<Vector2Int> Path { get; private set; }
        public bool stepFind;
        public float heuristicMultipler = 1f;
        public Action OnFinishPath;

        private Dictionary<string, PathNode> _openList = new Dictionary<string, PathNode>(); 
        private Dictionary<string, PathNode> _closedList = new Dictionary<string, PathNode>();
        private int[] _walkableValues;
        private bool _returnWorldSpaceValues;
        private List<Vector2Int> _pathPointsToFill;
        private List<Vector2> _pathPointsToFillWorldSpace;
        //private float _startToEndDist;
        
        private int _numSteps = 0;
        
        private NavmeshManager _manager;
       
        private Stopwatch _stopwatch;

        public const float D = 1;
        public const float D2 = 1.5f;
        public const int NUM_STEPS_TO_FAIL = 2250;
        public const int NUM_STEPS_TO_AWAIT = 750;

        public PathFinder(Vector2Int gridStart, Vector2 worldEnd, bool isGroundUnit,
            List<Vector2> pathPointsToFillWorldSpace = null)
        {
            InitializePathfinder(gridStart, worldEnd, isGroundUnit, pathPointsToFillWorldSpace);
        }
        public PathFinder(Vector2 start, Vector2 worldEnd, bool isGroundUnit, List<Vector2> pathPointsToFillWorldSpace = null)
        {
            InitializePathfinder(start, worldEnd, isGroundUnit, pathPointsToFillWorldSpace);
        }

        public PathFinder(Vector2Int gridStart, Vector2Int gridEnd, bool isGroundUnit, List<Vector2> pathPointsToFillWorldSpace = null, bool isWorldSpace = true)
        {
            PreInitializeForWorldSpace(pathPointsToFillWorldSpace);
            InitializePathfinder(gridStart, gridEnd, isGroundUnit);
        }
        
        public PathFinder(Vector2Int gridStart, Vector2Int gridEnd, bool isGroundUnit, List<Vector2Int> pathPointsToFill = null)
        {
            _manager = NavmeshManager.Get();
            InitializePathfinder(gridStart, gridEnd, isGroundUnit, pathPointsToFill);
        }

        private void PreInitializeForWorldSpace(List<Vector2> pathPointsToFillWorldSpace = null)
        {
            _manager = NavmeshManager.Get();
            _returnWorldSpaceValues = true;
            _pathPointsToFillWorldSpace = pathPointsToFillWorldSpace;
        }
        
        private void InitializePathfinder(Vector2 worldStart, Vector2Int gridEnd, bool isGroundUnit,
            List<Vector2> pathPointsToFillWorldSpace = null)
        {
            PreInitializeForWorldSpace(pathPointsToFillWorldSpace);
            InitializePathfinder(_manager.GetNodeGridPos(gridEnd), gridEnd, isGroundUnit);
        }
        private void InitializePathfinder(Vector2Int gridStart, Vector2 worldEnd, bool isGroundUnit,
            List<Vector2> pathPointsToFillWorldSpace = null)
        {
            PreInitializeForWorldSpace(pathPointsToFillWorldSpace);
            InitializePathfinder(gridStart, _manager.GetNodeGridPos(worldEnd), isGroundUnit);
        }
        
        private void InitializePathfinder(Vector2 worldStart, Vector2 worldEnd, bool isGroundUnit, 
            List<Vector2> pathPointsToFillWorldSpace = null)
        {
            PreInitializeForWorldSpace(pathPointsToFillWorldSpace);
            InitializePathfinder(_manager.GetNodeGridPos(worldStart), _manager.GetNodeGridPos(worldEnd), isGroundUnit);
        }
        
        private void InitializePathfinder(Vector2Int gridStart, Vector2Int gridEnd, bool isGroundUnit, List<Vector2Int> pathPointsToFill = null)
        {
            Start = gridStart;
            End = gridEnd;
            this.isGroundUnit = isGroundUnit;
            _pathPointsToFill = pathPointsToFill;

            _openList = new Dictionary<string, PathNode>
            {
                {$"{Start.x} {Start.y}", new PathNode(Start, 0f, 0f, null)}
            };
            
            _closedList = new Dictionary<string, PathNode>();

            Path = new List<Vector2Int>();

            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            
            
            if (!IsValidPathNode(gridStart)) return;
            if (!IsValidPathNode(gridEnd)) return;
            //if (!Manager.IsWalkableAtPos(End)) return;

           // _startToEndDist = Vector2Int.Distance(gridStart, worldEnd);
            
            if (Start != gridEnd)
                FindPath();
        }

        private bool IsValidPathNode(Vector2Int pos) => isGroundUnit ? _manager.IsWalkableAtPos(pos)
            : !_manager.HasTileAtPos(pos);

        private async void FindPath()
        {
            await Task.Run(() =>
            {
                stepFind = false;
                PathNode curNode = null;
                PathNode endNode = null;
                float lowF = Mathf.Infinity;

                /*
                string prePathDebug = "Open List Values: ";
                foreach (var openListValue in _openList.Values)
                    prePathDebug += $" {openListValue.gridPos}";
                prePathDebug += "Closed List Values: ";
                foreach (var closeListValue in _closedList.Values)
                    prePathDebug += $" {closeListValue.gridPos}";
                Debug.Log(prePathDebug);
                */

                // first determine node with lowest F
                foreach (var node in _openList.Values)
                {
                    var curF = node.movementCost + node.heuristicCost;

                    // currently this is just a brute force loop through every item in the list
                    // could be sped up using a sorted list or binary heap
                    if (lowF > curF)
                    {
                        lowF = curF;
                        curNode = node;
                    }
                }

                // no path exists!
                if (curNode == null)
                {
                    return;
                }


                // move selected node from open to closed list
                var label = $"{curNode.gridPos.x} {curNode.gridPos.y}";
                _openList.Remove(label);
                _closedList[label] = curNode;

                // check target
                if (curNode.gridPos == End)
                    endNode = curNode;

                // check each of the adjacent squares
                for (var x = -1; x < 2; x++)
                {
                    for (var y = -1; y < 2; y++)
                    {
                        var col = curNode.gridPos.x + x;
                        var row = curNode.gridPos.y + y;

                        Vector2Int neighborPos = new Vector2Int(col, row);

                        // make sure is a neighboring (not current) node and is on the grid
                        // https://stackoverflow.com/questions/9404683/how-to-get-the-length-of-row-column-of-multidimensional-array-in-c
                        bool
                            isNeighbor =
                                true; //only4way ? (i == 0 && j != 0) || (i != 0 && j == 0) : (i != 0 || j != 0); // 4 or 8 way movement?
                        if (isNeighbor && _manager.IsValidNavmeshPos(neighborPos))
                        {
                            var key = $"{col} {row}";
                            //Debug.Log($"Current Node: {curNode.gridPos}, Neighbor: {neighborPos}, " +
                            //          $"Is Walkable At neighbor: {isWalkableAtNeighbor}");

                            // if groundWalkable, not on closed list, and not already on open list - add to open list
                            // https://www.geeksforgeeks.org/c-sharp-check-if-an-array-contain-the-elements-that-match-the-specified-conditions/
                            //if (Array.Exists(_walkableValues, e => e == levelData[col, row]) && !_closedList.ContainsKey(key) && !_openList.ContainsKey(key))
                            if (IsValidPathNode(neighborPos) && !_closedList.ContainsKey(key) &&
                                !_openList.ContainsKey(key))
                            {

                                // diagonals have greater movement cost

                                float moveCost = D;
                                if (x != 0 && y != 0)
                                {
                                    moveCost = D2;
                                }

                                // calculate heuristic value
                                float heuristicValue = DiagonalDistance(neighborPos, End) * .05f;//heuristicMultipler;
                                //heuristicValue *= 1f + D / 100f;

                                /*
                                if (curNode.parent != null)
                                {
                                    Vector2 parentToNeighborDir = neighborPos - curNode.gridPos;
                                    Vector2 parentParentToParentDir =  curNode.gridPos - curNode.parent.gridPos;
                                    heuristicValue *= 1f + (parentToNeighborDir - parentParentToParentDir).magnitude / 30f;
                                }
                                */



                                //ApplyStraightLineHeuristicInfluence(ref heuristicValue, neighborPos);

                                //Mathf.Abs(col - End.x) + Mathf.Abs(row - End.y);
                                ////((col - End.x) * (col - End.x) + (row - End.y) * (row - End.y)) * 25f;

                                //Debug.Log($"move cost: {moveCost}, dist cost: {distCost}");
                                // slightly different path results from:
                                //(Mathf.Abs(col - End.x) + Mathf.Abs(row - End.y)) * 10;
                                //Debug.DrawLine(_manager.NodePosToWorldPos(curNode.gridPos), _manager.NodePosToWorldPos(neighborPos), Color.cyan, 5f);
                                var found = new PathNode(neighborPos, moveCost, heuristicValue, curNode);
                                _openList.Add(key, found);
                            }
                        }
                    }
                }


                _numSteps++;

                // recurse if target not reached
                if (endNode == null)
                {
                    FindPath();
                }
                else
                {
                    CreatePath(endNode);
                    //Debug.Log($"Success, number of operations: {_numSteps}, path length: {Path.Count}");
                    if (_returnWorldSpaceValues && _pathPointsToFillWorldSpace != null)
                    {
                        _pathPointsToFillWorldSpace.Clear();
                        foreach (Vector2Int gridPos in Path)
                        {
                            _pathPointsToFillWorldSpace.Add(_manager.NodePosToWorldPos(gridPos));
                        }
                    }
                    else if (_pathPointsToFill != null)
                        _pathPointsToFill = Path;
                    
                    OnFinishPath?.Invoke();
                    
                    _stopwatch.Stop();
                   // Debug.Log($"{_stopwatch.Elapsed.TotalMilliseconds}");
                }
            });
        }

        private void CreatePath(PathNode node)
        {
            var step = new Vector2Int(node.gridPos.x, node.gridPos.y);
            Path.Add(step);

            if (node.movementCost > 0)
            {
                CreatePath(node.parent);
            }
        }

        private void ApplyStraightLineHeuristicInfluence(ref float heuristic, Vector2 current)
        {
            float dx1 = current.x - End.x;
            float dy1 = current.y - End.y;
            float dx2 = Start.x - End.x;
            float dy2 = Start.y - End.y;
            float cross = Mathf.Abs(dx1 * dy2 - dx2 * dy1);
            heuristic += cross * 0.001f;
        }
        private float DiagonalDistance(Vector2Int start, Vector2Int end)
        {
            float dx = Mathf.Abs(start.x - end.x);
            float dy = Mathf.Abs(start.y - end.y);

            return D * (dx + dy) + (D2 - 2 * D) * Mathf.Min(dx, dy);
        }
    }
}