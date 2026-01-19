using System.Collections.Generic;
using UnityEngine;

namespace EasyPath
{
    /// <summary>
    /// A* pathfinding algorithm implementation.
    /// </summary>
    public class AStarPathfinder
    {
        private const int STRAIGHT_COST = 10;
        private const int DIAGONAL_COST = 14;
        
        private EasyPathGrid _grid;
        private PriorityQueue<PathNode> _openSet;
        private HashSet<PathNode> _closedSet;

        // Performance: Pre-allocated buffers to avoid per-pathfind allocations
        private readonly List<PathNode> _neighborBuffer = new List<PathNode>(8);
        private readonly Stack<Vector3> _pathStack = new Stack<Vector3>(64);

        /// <summary>
        /// Creates a new AStarPathfinder configured to operate on the provided grid.
        /// </summary>
        /// <param name="grid">The grid instance used for pathfinding operations.</param>
        public AStarPathfinder(EasyPathGrid grid)
        {
            _grid = grid;
            _openSet = new PriorityQueue<PathNode>(256);
            _closedSet = new HashSet<PathNode>();
        }
        
        /// <summary>
        /// Find a path from start to end position.
        /// </summary>
        public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
        {
            PathNode startNode = _grid.GetNodeFromWorldPosition(startWorld);
            PathNode endNode = _grid.GetNodeFromWorldPosition(endWorld);
            
            if (startNode == null || endNode == null)
            {
                return null;
            }
            
            if (!endNode.IsWalkable)
            {
                endNode = FindNearestWalkable(endNode);
                if (endNode == null)
                {
                    return null;
                }
            }
            
            return FindPath(startNode, endNode);
        }
        
        /// <summary>
        /// Find a path between two nodes.
        /// <summary>
        /// Computes a path of world positions from a start node to an end node using the A* algorithm.
        /// </summary>
        /// <param name="startNode">The starting grid node for the path search.</param>
        /// <param name="endNode">The target grid node for the path search.</param>
        /// <returns>
        /// A list of world-space positions representing the path from start to end, or <c>null</c> if no path is available or if either input node is <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method mutates pathfinding state: it clears the internal open and closed sets, increments the grid's path version (enabling lazy node resets), and updates node fields such as <c>Parent</c>, <c>GCost</c>, and <c>HCost</c>. It returns the reconstructed path when the target is reached; otherwise it returns <c>null</c>.
        /// </remarks>
        public List<Vector3> FindPath(PathNode startNode, PathNode endNode)
        {
            if (startNode == null || endNode == null)
            {
                return null;
            }

            // Reset data structures
            _openSet.Clear();
            _closedSet.Clear();

            // Performance: Use version-based reset instead of O(width*height) full reset
            _grid.IncrementPathVersion();

            // Initialize start node (lazy reset via version check)
            _grid.ResetNodeIfNeeded(startNode);
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(startNode, endNode);
            _openSet.Enqueue(startNode);

            while (_openSet.Count > 0)
            {
                PathNode currentNode = _openSet.Dequeue();

                // Found the goal
                if (currentNode.Equals(endNode))
                {
                    return ReconstructPath(currentNode);
                }

                _closedSet.Add(currentNode);

                // Performance: Use pre-allocated buffer for neighbors
                _grid.GetNeighbors(currentNode, _neighborBuffer);

                for (int i = 0; i < _neighborBuffer.Count; i++)
                {
                    PathNode neighbor = _neighborBuffer[i];

                    if (_closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    if (!neighbor.IsWalkable)
                    {
                        continue;
                    }

                    // Performance: Lazy reset via version check
                    _grid.ResetNodeIfNeeded(neighbor);

                    int movementCost = GetMovementCost(currentNode, neighbor);
                    int tentativeGCost = currentNode.GCost + movementCost + neighbor.MovementPenalty;

                    if (tentativeGCost < neighbor.GCost)
                    {
                        neighbor.Parent = currentNode;
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = CalculateHeuristic(neighbor, endNode);

                        if (!_openSet.Contains(neighbor))
                        {
                            _openSet.Enqueue(neighbor);
                        }
                        else
                        {
                            _openSet.UpdatePriority(neighbor);
                        }
                    }
                }
            }

            // No path found
            return null;
        }
        
        private int CalculateHeuristic(PathNode a, PathNode b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            
            // Diagonal distance heuristic
            int straight = Mathf.Abs(dx - dy);
            int diagonal = Mathf.Min(dx, dy);
            
            return STRAIGHT_COST * straight + DIAGONAL_COST * diagonal;
        }
        
        private int GetMovementCost(PathNode from, PathNode to)
        {
            bool isDiagonal = from.X != to.X && from.Y != to.Y;
            return isDiagonal ? DIAGONAL_COST : STRAIGHT_COST;
        }
        
        /// <summary>
        /// Reconstructs the path by following parent links from the specified end node back to the start and returns the path as world positions in start-to-end order.
        /// </summary>
        /// <param name="endNode">The terminal node of the path from which to begin reconstruction; may be null.</param>
        /// <returns>A list of world-space positions representing the path from start to end. Returns an empty list if <paramref name="endNode"/> is null.</returns>
        private List<Vector3> ReconstructPath(PathNode endNode)
        {
            // Performance: Use cached stack to avoid Reverse() operation
            _pathStack.Clear();
            PathNode current = endNode;

            while (current != null)
            {
                _pathStack.Push(current.WorldPosition);
                current = current.Parent;
            }

            // Build path in correct order from stack
            var path = new List<Vector3>(_pathStack.Count);
            while (_pathStack.Count > 0)
            {
                path.Add(_pathStack.Pop());
            }

            return path;
        }
        
        private PathNode FindNearestWalkable(PathNode node)
        {
            int searchRadius = 1;
            int maxRadius = Mathf.Max(_grid.Width, _grid.Height);
            
            while (searchRadius < maxRadius)
            {
                for (int x = -searchRadius; x <= searchRadius; x++)
                {
                    for (int y = -searchRadius; y <= searchRadius; y++)
                    {
                        PathNode candidate = _grid.GetNode(node.X + x, node.Y + y);
                        if (candidate != null && candidate.IsWalkable)
                        {
                            return candidate;
                        }
                    }
                }
                searchRadius++;
            }
            
            return null;
        }
    }
}