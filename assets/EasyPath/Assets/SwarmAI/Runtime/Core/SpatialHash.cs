using System.Collections.Generic;
using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Spatial hash grid for efficient neighbor queries.
    /// Provides O(1) average-case lookups for nearby objects.
    /// </summary>
    /// <typeparam name="T">Type of items to store in the spatial hash.</typeparam>
    public class SpatialHash<T> where T : class
    {
        private readonly float _cellSize;
        private readonly Dictionary<Vector2Int, List<T>> _cells;
        private readonly Dictionary<T, Vector2Int> _itemCells;
        
        /// <summary>
        /// Number of items in the spatial hash.
        /// </summary>
        public int Count => _itemCells.Count;
        
        /// <summary>
        /// Cell size used for partitioning.
        /// </summary>
        public float CellSize => _cellSize;
        
        /// <summary>
        /// Create a new spatial hash with the specified cell size.
        /// </summary>
        /// <param name="cellSize">Size of each cell. Recommended: 2x the query radius.</param>
        public SpatialHash(float cellSize = 10f)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
            _cells = new Dictionary<Vector2Int, List<T>>();
            _itemCells = new Dictionary<T, Vector2Int>();
        }
        
        /// <summary>
        /// Insert an item at the specified position.
        /// </summary>
        public void Insert(T item, Vector3 position)
        {
            if (item == null) return;
            
            // Remove from old cell if already inserted
            if (_itemCells.ContainsKey(item))
            {
                Remove(item);
            }
            
            Vector2Int cell = PositionToCell(position);
            
            if (!_cells.TryGetValue(cell, out List<T> cellItems))
            {
                cellItems = new List<T>();
                _cells[cell] = cellItems;
            }
            
            cellItems.Add(item);
            _itemCells[item] = cell;
        }
        
        /// <summary>
        /// Remove an item from the spatial hash.
        /// </summary>
        public void Remove(T item)
        {
            if (item == null) return;
            
            if (_itemCells.TryGetValue(item, out Vector2Int cell))
            {
                if (_cells.TryGetValue(cell, out List<T> cellItems))
                {
                    cellItems.Remove(item);
                    
                    // Clean up empty cells to prevent memory buildup
                    if (cellItems.Count == 0)
                    {
                        _cells.Remove(cell);
                    }
                }
                
                _itemCells.Remove(item);
            }
        }
        
        /// <summary>
        /// Update an item's position in the spatial hash.
        /// </summary>
        public void UpdatePosition(T item, Vector3 newPosition)
        {
            if (item == null) return;
            
            Vector2Int newCell = PositionToCell(newPosition);
            
            // Check if we need to move cells
            if (_itemCells.TryGetValue(item, out Vector2Int oldCell))
            {
                if (oldCell == newCell)
                {
                    return; // Same cell, no update needed
                }
                
                // Remove from old cell
                if (_cells.TryGetValue(oldCell, out List<T> oldCellItems))
                {
                    oldCellItems.Remove(item);
                    if (oldCellItems.Count == 0)
                    {
                        _cells.Remove(oldCell);
                    }
                }
            }
            
            // Add to new cell
            if (!_cells.TryGetValue(newCell, out List<T> newCellItems))
            {
                newCellItems = new List<T>();
                _cells[newCell] = newCellItems;
            }
            
            newCellItems.Add(item);
            _itemCells[item] = newCell;
        }
        
        /// <summary>
        /// Query all items within a radius of the specified position.
        /// </summary>
        /// <param name="center">Center of the query sphere.</param>
        /// <param name="radius">Radius to search within.</param>
        /// <returns>List of items within the radius.</returns>
        public List<T> Query(Vector3 center, float radius)
        {
            List<T> results = new List<T>();
            Query(center, radius, results);
            return results;
        }
        
        /// <summary>
        /// Query all items within a radius, using a pre-allocated list to reduce GC.
        /// <para><b>Note:</b> The results list is cleared at the start of this method.</para>
        /// </summary>
        /// <param name="center">Center of the query sphere.</param>
        /// <param name="radius">Radius to search within.</param>
        /// <param name="results">Pre-allocated list to fill with results. <b>Will be cleared first.</b></param>
        /// <param name="getPosition">Optional function to get position of an item for accurate Euclidean distance filtering. If null, all items in nearby cells are returned (faster but less accurate).</param>
        public void Query(Vector3 center, float radius, List<T> results, System.Func<T, Vector3> getPosition = null)
        {
            results.Clear();
            float radiusSq = radius * radius;
            
            // Calculate cell range to check
            int cellRadius = Mathf.CeilToInt(radius / _cellSize);
            Vector2Int centerCell = PositionToCell(center);
            
            // Check all cells in range
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + x, centerCell.y + y);
                    
                    if (_cells.TryGetValue(cell, out List<T> cellItems))
                    {
                        // Filter by actual distance if position function provided
                        if (getPosition != null)
                        {
                            foreach (var item in cellItems)
                            {
                                Vector3 itemPos = getPosition(item);
                                float distSq = (itemPos - center).sqrMagnitude;
                                if (distSq <= radiusSq)
                                {
                                    results.Add(item);
                                }
                            }
                        }
                        else
                        {
                            results.AddRange(cellItems);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Query all items within a radius, excluding a specific item.
        /// Useful for finding neighbors of an agent.
        /// </summary>
        public List<T> QueryExcluding(Vector3 center, float radius, T exclude)
        {
            List<T> results = new List<T>();
            QueryExcluding(center, radius, exclude, results);
            return results;
        }
        
        /// <summary>
        /// Query all items within a radius, excluding a specific item, using a pre-allocated list.
        /// </summary>
        public void QueryExcluding(Vector3 center, float radius, T exclude, List<T> results, System.Func<T, Vector3> getPosition = null)
        {
            Query(center, radius, results, getPosition);
            results.Remove(exclude);
        }
        
        /// <summary>
        /// Clear all items from the spatial hash.
        /// </summary>
        public void Clear()
        {
            _cells.Clear();
            _itemCells.Clear();
        }
        
        /// <summary>
        /// Check if an item exists in the spatial hash.
        /// </summary>
        public bool Contains(T item)
        {
            return item != null && _itemCells.ContainsKey(item);
        }
        
        /// <summary>
        /// Get the cell an item is currently in.
        /// </summary>
        public bool TryGetCell(T item, out Vector2Int cell)
        {
            if (item != null && _itemCells.TryGetValue(item, out cell))
            {
                return true;
            }
            cell = Vector2Int.zero;
            return false;
        }
        
        private Vector2Int PositionToCell(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _cellSize),
                Mathf.FloorToInt(position.z / _cellSize)
            );
        }
    }
}
