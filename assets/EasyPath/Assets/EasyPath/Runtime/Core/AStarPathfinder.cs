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
        /// </summary>
        public List<Vector3> FindPath(PathNode startNode, PathNode endNode)
        {
            if (startNode == null || endNode == null)
            {
                return null;
            }
            
            // Reset data structures
            _openSet.Clear();
            _closedSet.Clear();
            _grid.ResetNodes();
            
            // Initialize start node
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
                
                // Check all neighbors
                foreach (PathNode neighbor in _grid.GetNeighbors(currentNode))
                {
                    if (_closedSet.Contains(neighbor))
                    {
                        continue;
                    }
                    
                    if (!neighbor.IsWalkable)
                    {
                        continue;
                    }
                    
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
        
        private List<Vector3> ReconstructPath(PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode current = endNode;
            
            while (current != null)
            {
                path.Add(current.WorldPosition);
                current = current.Parent;
            }
            
            path.Reverse();
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
