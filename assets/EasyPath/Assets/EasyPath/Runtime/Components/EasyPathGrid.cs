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

        // Performance: Version-based reset eliminates O(width*height) reset per pathfind
        private int _currentPathVersion;

        // Performance: Pre-allocated buffer for GetNeighbors to avoid yield iterator allocation
        private readonly List<PathNode> _neighborBuffer = new List<PathNode>(8);

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public int WalkableCount { get; private set; }

        /// <summary>
        /// Current pathfinding version. Incremented each pathfind query to invalidate node state.
        /// </summary>
        public int CurrentPathVersion => _currentPathVersion;
        
        /// <summary>
        /// Unity Awake lifecycle hook that initializes the pathfinding grid and related runtime state when the component is loaded.
        /// </summary>
        private void Awake()
        {
            BuildGrid();
        }
        
        /// <summary>
        /// Initializes the internal grid data for the current component configuration and prepares the pathfinder.
        /// </summary>
        /// <remarks>
        /// Allocates and populates the internal node array, sets WalkableCount to the number of walkable cells, resets the path version to zero, and constructs the A* pathfinder instance. Each node's walkability is determined by sampling the scene using the configured obstacle settings. Calls ValidateGridConfiguration() after building the grid.
        /// </remarks>
        public void BuildGrid()
        {
            _nodes = new PathNode[_width, _height];
            WalkableCount = 0;
            _currentPathVersion = 0;

            // Performance: Cache origin to avoid transform.position access per cell
            Vector3 origin = transform.position;
            float halfCell = _cellSize * 0.5f;
            Vector3 upOffset = Vector3.up * _obstacleCheckHeight;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    // Performance: Inline GridToWorld calculation to avoid method call + transform access
                    Vector3 worldPos = new Vector3(
                        origin.x + x * _cellSize + halfCell,
                        origin.y,
                        origin.z + y * _cellSize + halfCell
                    );

                    // Check for obstacles at elevated height to avoid detecting ground plane
                    Vector3 checkPos = worldPos + upOffset;
                    bool walkable = !Physics.CheckSphere(checkPos, _obstacleCheckRadius, _obstacleLayer);

                    _nodes[x, y] = new PathNode(x, y, walkable, worldPos);

                    if (walkable)
                    {
                        WalkableCount++;
                    }
                }
            }

            _pathfinder = new AStarPathfinder(this);

            // Runtime diagnostics - warn about potential misconfigurations
            ValidateGridConfiguration();
        }
        
        /// <summary>
        /// Validates the grid configuration and logs warnings for common issues.
        /// <summary>
        /// Validates the grid configuration and emits warnings for common misconfigurations or suspicious values.
        /// </summary>
        /// <remarks>
        /// Logs warnings when the walkable cell percentage is unusually low, when the obstacle layer is unset or configured to \"Everything\", or when the cell size is unusually small or large. Logs a summary message when the grid configuration appears reasonable.
        /// </remarks>
        private void ValidateGridConfiguration()
        {
            int totalCells = _width * _height;
            float walkablePercent = (float)WalkableCount / totalCells * 100f;
            
            // Check for suspiciously low walkable percentage
            if (walkablePercent < 10f)
            {
                Debug.LogWarning($"[EasyPathGrid] WARNING: Only {walkablePercent:F1}% of cells are walkable ({WalkableCount}/{totalCells}).\n" +
                    "This usually indicates a configuration issue:\n" +
                    "1. Obstacle Layer may be detecting the ground plane - set to a specific layer (e.g., 'Obstacles')\n" +
                    "2. Obstacle Check Height may be too low - try increasing it above ground level (default 0.5)\n" +
                    "3. Obstacle Check Radius may be too large for your cell size");
            }
            else if (walkablePercent < 30f)
            {
                Debug.LogWarning($"[EasyPathGrid] Low walkable cell count: {walkablePercent:F1}% ({WalkableCount}/{totalCells}). " +
                    "Pathfinding may be limited. Consider checking obstacle layer settings.");
            }
            
            // Check if no obstacle layer is set (will detect everything)
            if (_obstacleLayer.value == 0)
            {
                Debug.LogWarning("[EasyPathGrid] No Obstacle Layer set. Grid will treat all cells as walkable.\n" +
                    "Set the Obstacle Layer to detect obstacles.");
            }
            else if (_obstacleLayer.value == -1 || _obstacleLayer.value == ~0)
            {
                Debug.LogWarning("[EasyPathGrid] Obstacle Layer is set to 'Everything'. This will detect ALL colliders including the ground!\n" +
                    "Create a dedicated 'Obstacles' layer and assign obstacles to it.");
            }
            
            // Check for very small or very large cell sizes
            if (_cellSize < 0.1f)
            {
                Debug.LogWarning($"[EasyPathGrid] Cell size ({_cellSize}) is very small. This may cause performance issues.");
            }
            else if (_cellSize > 10f)
            {
                Debug.LogWarning($"[EasyPathGrid] Cell size ({_cellSize}) is very large. Pathfinding may be imprecise.");
            }
            
            // Log successful configuration
            if (walkablePercent >= 30f)
            {
                Debug.Log($"[EasyPathGrid] Grid built: {_width}x{_height}, {WalkableCount} walkable cells ({walkablePercent:F1}%)");
            }
        }
        
        /// <summary>
        /// Advance the grid's pathfinding version to invalidate any previously cached per-node path state.
        /// This is O(1) compared to the O(width*height) full reset.
        /// </summary>
        /// <remarks>
        /// Incrementing the version allows nodes to be lazily reset on next access without iterating the entire grid.
        /// </remarks>
        public void IncrementPathVersion()
        {
            _currentPathVersion++;
        }

        /// <summary>
        /// Resets a node if it was used in a previous pathfinding query.
        /// Uses version comparison for O(1) lazy reset.
        /// </summary>
        /// <summary>
        /// Resets the node when its LastUsedVersion differs from the grid's current path version.
        /// When reset, the node's LastUsedVersion is updated to the current path version.
        /// </summary>
        /// <param name="node">The node to inspect and reset if stale; null is accepted.</param>
        public void ResetNodeIfNeeded(PathNode node)
        {
            if (node != null && node.LastUsedVersion != _currentPathVersion)
            {
                node.Reset();
                node.LastUsedVersion = _currentPathVersion;
            }
        }

        /// <summary>
        /// Reset all nodes for a new pathfinding query.
        /// Note: Prefer using IncrementPathVersion() + ResetNodeIfNeeded() for better performance.
        /// <summary>
        /// Reset every node in the grid to its default state.
        /// </summary>
        /// <remarks>
        /// Performs a full traversal of the grid (O(width * height)). This method is deprecated; use <see cref="IncrementPathVersion"/> for O(1) invalidation and lazy per-node resets.
        /// </remarks>
        [System.Obsolete("Use IncrementPathVersion() for O(1) reset instead of O(width*height).")]
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
        /// Gets the grid node that contains the specified world-space position.
        /// </summary>
        /// <param name="worldPos">A world-space position to map into the grid.</param>
        /// <returns>The PathNode for the grid cell containing <paramref name="worldPos"/>.</returns>
        public PathNode GetNodeFromWorldPosition(Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            return GetNode(gridPos.x, gridPos.y);
        }
        
        /// <summary>
        /// Get all valid neighbors of a node using a pre-allocated buffer (allocation-free).
        /// </summary>
        /// <param name="node">The node to get neighbors for.</param>
        /// <summary>
        /// Populates the provided list with the valid neighboring nodes of the specified node using an 8-way neighborhood while preventing diagonal corner-cutting.
        /// </summary>
        /// <param name="node">The source grid node whose neighbors will be collected.</param>
        /// <param name="results">The list to fill with neighbor nodes; this list is cleared before use to enable allocation-free reuse. Diagonal neighbors are omitted when both adjacent axis-aligned neighbors are not walkable.</param>
        public void GetNeighbors(PathNode node, List<PathNode> results)
        {
            results.Clear();

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

                        results.Add(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Get all valid neighbors of a node.
        /// Note: This allocates an iterator. Prefer GetNeighbors(node, results) for hot paths.
        /// <summary>
        /// Enumerates the neighbor nodes of the given grid node using an internal reusable buffer.
        /// </summary>
        /// <returns>An IEnumerable&lt;PathNode&gt; containing the valid neighboring nodes. The sequence is backed by an internal buffer and may be mutated or reused by subsequent calls, so callers should not cache the returned collection or rely on its contents persisting.</returns>
        public IEnumerable<PathNode> GetNeighbors(PathNode node)
        {
            GetNeighbors(node, _neighborBuffer);
            return _neighborBuffer;
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