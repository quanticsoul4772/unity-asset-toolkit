using System;
using UnityEngine;

namespace EasyPath
{
    /// <summary>
    /// Represents a node in the pathfinding grid.
    /// </summary>
    public class PathNode : IComparable<PathNode>
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Vector2Int Position => new Vector2Int(X, Y);
        
        public bool IsWalkable { get; set; }
        public int MovementPenalty { get; set; }
        
        // A* costs
        public int GCost { get; set; } // Cost from start
        public int HCost { get; set; } // Heuristic cost to end
        public int FCost => GCost + HCost; // Total cost
        
        public PathNode Parent { get; set; }
        
        // World position
        public Vector3 WorldPosition { get; set; }
        
        public PathNode(int x, int y, bool isWalkable, Vector3 worldPosition)
        {
            X = x;
            Y = y;
            IsWalkable = isWalkable;
            WorldPosition = worldPosition;
            MovementPenalty = 0;
        }
        
        public void Reset()
        {
            GCost = int.MaxValue;
            HCost = 0;
            Parent = null;
        }
        
        public int CompareTo(PathNode other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0)
            {
                compare = HCost.CompareTo(other.HCost);
            }
            return compare;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is PathNode other)
            {
                return X == other.X && Y == other.Y;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return X * 10000 + Y;
        }
        
        public override string ToString()
        {
            return $"PathNode({X}, {Y}) W:{IsWalkable} F:{FCost}";
        }
    }
}
