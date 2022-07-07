using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _Project.CodeBase.Navmesh
{
    public class PathFinder
    {
        public readonly Vector2Int start;
        public readonly Vector2Int end;
        private NavmeshManager _manager;
        public List<Vector2Int> Path { get; private set; }
        public bool stepFind;

        private Dictionary<string, PathNode> _openList = new Dictionary<string, PathNode>(); 
        private Dictionary<string, PathNode> _closedList = new Dictionary<string, PathNode>();
        private int[] _walkableValues;

        private Stopwatch _stopwatch;
        
        public PathFinder(Vector2Int start, Vector2Int end, NavmeshManager manager)
        {
            this.start = start;
            this.end = end;
            _manager = manager;
            
            _openList = new Dictionary<string, PathNode>
            {
                {$"{this.start.x} {this.start.y}", new PathNode(this.start, 0f, 0f, null)}
            };

            _closedList = new Dictionary<string, PathNode>();

            Path = new List<Vector2Int>();

            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            
            if (!_manager.IsWalkableAtPos(start)) return;
            //if (!_manager.IsWalkableAtPos(end)) return;
            
            if (this.start != end)
                FindPath();
        }

        private void FindPath()
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
                var curF = node.movementCost + node.distanceCost;

                // currently this is just a brute force loop through every item in the list
                // could be sped up using a sorted list or binary heap
                if (lowF > curF)
                {
                    lowF = curF;
                    curNode = node;
                }
            }

            // no path exists!
            if (curNode == null) { return; }

            // move selected node from open to closed list
            var label = $"{curNode.gridPos.x} {curNode.gridPos.y}";
            _openList.Remove(label);
            _closedList[label] = curNode;

            // check target
            if (curNode.gridPos == end)
                endNode = curNode;
           // else
            //{

                // check each of the adjacent squares
                for (var i = -1; i < 2; i++)
                {
                    for (var j = -1; j < 2; j++)
                    {
                        var col = curNode.gridPos.x + i;
                        var row = curNode.gridPos.y + j;

                        Vector2Int neighborPos = new Vector2Int(col, row);

                        // make sure is a neighboring (not current) node and is on the grid
                        // https://stackoverflow.com/questions/9404683/how-to-get-the-length-of-row-column-of-multidimensional-array-in-c
                        bool
                            isNeighbor =
                                true; //only4way ? (i == 0 && j != 0) || (i != 0 && j == 0) : (i != 0 || j != 0); // 4 or 8 way movement?
                        if (isNeighbor && _manager.IsValidNavmeshPos(neighborPos))
                        {
                            var key = $"{col} {row}";

                            bool isWalkableAtNeighbor = _manager.IsWalkableAtPos(neighborPos);
                            //Debug.Log($"Current Node: {curNode.gridPos}, Neighbor: {neighborPos}, " +
                            //          $"Is Walkable At neighbor: {isWalkableAtNeighbor}");

                            // if walkable, not on closed list, and not already on open list - add to open list
                            // https://www.geeksforgeeks.org/c-sharp-check-if-an-array-contain-the-elements-that-match-the-specified-conditions/
                            //if (Array.Exists(_walkableValues, e => e == levelData[col, row]) && !_closedList.ContainsKey(key) && !_openList.ContainsKey(key))
                            if (isWalkableAtNeighbor && !_closedList.ContainsKey(key) && !_openList.ContainsKey(key))
                            {

                                // diagonals have greater movement cost
                                var moveCost = 10;
                                if (i != 0 && j != 0)
                                {
                                    moveCost = 14;
                                }

                                // calculate heuristic value
                                var distCost = (col - end.x) * (col - end.x) + (row - end.y) * (row - end.y);
                                // TODO slightly different path results from:
                                //var h = (Mathf.Abs(col - finX)) + (Mathf.Abs(row - finY)) * 10;

                                var found = new PathNode(neighborPos, moveCost, distCost, curNode);
                                _openList.Add(key, found);
                            }
                        }
                    }
                }
            //}
            //Debug.Log(curNode.gridPos);
            //while (!stepFind)
            //{
                //await Task.Yield();
           // }

            // recurse if target not reached
            if (endNode == null)
            {
                FindPath();
            }
            else
            {
                CreatePath(endNode);
                _stopwatch.Stop();
                Debug.Log($"{_stopwatch.Elapsed.TotalMilliseconds}");
            }
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
    }
}