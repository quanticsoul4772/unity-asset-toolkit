using System.Collections.Generic;
using UnityEngine;

namespace EasyPath
{
    /// <summary>
    /// Grid component for A* pathfinding.
    /// Attach to a GameObject to create a pathfinding grid.
    /// </summary>
    public class EasyPathGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int _width = 20;
        [SerializeField] private int _height = 20;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _obstacleCheckRadius = 0.4f;
        [SerializeField] private float _obstacleCheckHeight = 0.5f; // Check above ground level
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        [SerializeField] private Color _walkableColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color _blockedColor = new Color(1f, 0f, 0f, 0.3f);
        
        private PathNode[,] _nodes;
        private AStarPathfinder _pathfinder;
        
        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public int WalkableCount { get; private set; }
        
        private void Awake()
        {
            BuildGrid();
        }
        
        /// <summary>
        /// Build or rebuild the pathfinding grid.
        /// </summary>
        public void BuildGrid()
        {
            _nodes = new PathNode[_width, _height];
            WalkableCount = 0;
            
            Vector3 origin = transform.position;
            
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 worldPos = GridToWorld(x, y);
                    // Check for obstacles at elevated height to avoid detecting ground plane
                    Vector3 checkPos = worldPos + Vector3.up * _obstacleCheckHeight;
                    bool walkable = !Physics.CheckSphere(checkPos, _obstacleCheckRadius, _obstacleLayer);
                    
                    _nodes[x, y] = new PathNode(x, y, walkable, worldPos);
                    
                    if (walkable)
                    {
                        WalkableCount++;
                    }
                }
            }
            
            _pathfinder = new AStarPathfinder(this);
        }
        
        /// <summary>
        /// Reset all nodes for a new pathfinding query.
        /// </summary>
        public void ResetNodes()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _nodes[x, y].Reset();
                }
            }
        }
        
        /// <summary>
        /// Find a path from start to end world position.
        /// </summary>
        public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
        {
            if (_pathfinder == null)
            {
                BuildGrid();
            }
            return _pathfinder.FindPath(startWorld, endWorld);
        }
        
        /// <summary>
        /// Get a node at grid coordinates.
        /// </summary>
        public PathNode GetNode(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return null;
            }
            return _nodes[x, y];
        }
        
        /// <summary>
        /// Get the node at a world position.
        /// </summary>
        public PathNode GetNodeFromWorldPosition(Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            return GetNode(gridPos.x, gridPos.y);
        }
        
        /// <summary>
        /// Get all valid neighbors of a node.
        /// </summary>
        public IEnumerable<PathNode> GetNeighbors(PathNode node)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }
                    
                    int checkX = node.X + x;
                    int checkY = node.Y + y;
                    
                    PathNode neighbor = GetNode(checkX, checkY);
                    if (neighbor != null)
                    {
                        // Check for diagonal corner cutting
                        if (x != 0 && y != 0)
                        {
                            PathNode adjX = GetNode(node.X + x, node.Y);
                            PathNode adjY = GetNode(node.X, node.Y + y);
                            
                            if (adjX != null && !adjX.IsWalkable && adjY != null && !adjY.IsWalkable)
                            {
                                continue; // Can't cut corners
                            }
                        }
                        
                        yield return neighbor;
                    }
                }
            }
        }
        
        /// <summary>
        /// Convert grid coordinates to world position.
        /// </summary>
        public Vector3 GridToWorld(int x, int y)
        {
            Vector3 origin = transform.position;
            return new Vector3(
                origin.x + x * _cellSize + _cellSize * 0.5f,
                origin.y,
                origin.z + y * _cellSize + _cellSize * 0.5f
            );
        }
        
        /// <summary>
        /// Convert world position to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 origin = transform.position;
            int x = Mathf.FloorToInt((worldPos.x - origin.x) / _cellSize);
            int y = Mathf.FloorToInt((worldPos.z - origin.z) / _cellSize);
            return new Vector2Int(
                Mathf.Clamp(x, 0, _width - 1),
                Mathf.Clamp(y, 0, _height - 1)
            );
        }
        
        /// <summary>
        /// Check if a position is walkable.
        /// </summary>
        public bool IsWalkable(Vector3 worldPos)
        {
            PathNode node = GetNodeFromWorldPosition(worldPos);
            return node != null && node.IsWalkable;
        }
        
        /// <summary>
        /// Check if grid coordinates are walkable.
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            PathNode node = GetNode(x, y);
            return node != null && node.IsWalkable;
        }
        
        /// <summary>
        /// Set the walkability of a node at grid coordinates.
        /// </summary>
        public void SetWalkable(int x, int y, bool walkable)
        {
            PathNode node = GetNode(x, y);
            if (node != null)
            {
                if (node.IsWalkable && !walkable)
                {
                    WalkableCount--;
                }
                else if (!node.IsWalkable && walkable)
                {
                    WalkableCount++;
                }
                node.IsWalkable = walkable;
            }
        }
        
        /// <summary>
        /// Toggle the walkability of a node.
        /// </summary>
        public void ToggleWalkable(int x, int y)
        {
            PathNode node = GetNode(x, y);
            if (node != null)
            {
                SetWalkable(x, y, !node.IsWalkable);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos)
            {
                return;
            }
            
            // Draw grid boundary
            Gizmos.color = Color.white;
            Vector3 origin = transform.position;
            Vector3 size = new Vector3(_width * _cellSize, 0.1f, _height * _cellSize);
            Gizmos.DrawWireCube(origin + size * 0.5f, size);
            
            // Draw cells if grid is built
            if (_nodes == null)
            {
                return;
            }
            
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    PathNode node = _nodes[x, y];
                    Gizmos.color = node.IsWalkable ? _walkableColor : _blockedColor;
                    
                    Vector3 cellCenter = GridToWorld(x, y);
                    Vector3 cellSize = Vector3.one * _cellSize * 0.9f;
                    cellSize.y = 0.1f;
                    
                    Gizmos.DrawCube(cellCenter, cellSize);
                }
            }
        }
    }
}
