using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace SwarmAI.Jobs
{
    /// <summary>
    /// Burst-compatible spatial hash grid for efficient parallel neighbor queries.
    /// Uses NativeMultiHashMap for O(1) cell lookups.
    /// </summary>
    public struct BurstSpatialHash
    {
        /// <summary>Cell size for spatial partitioning.</summary>
        public float CellSize;
        
        /// <summary>Hash map storing agent indices per cell.</summary>
        public NativeMultiHashMap<int, int> CellToAgents;
        
        /// <summary>Whether this spatial hash has been created.</summary>
        public bool IsCreated => CellToAgents.IsCreated;
        
        /// <summary>
        /// Create a new burst spatial hash.
        /// </summary>
        /// <param name="cellSize">Size of each cell.</param>
        /// <param name="capacity">Expected number of agents.</param>
        /// <param name="allocator">Memory allocator to use.</param>
        public BurstSpatialHash(float cellSize, int capacity, Allocator allocator)
        {
            CellSize = math.max(0.1f, cellSize);
            // Each agent might be in multiple cells for border cases, so allocate extra
            CellToAgents = new NativeMultiHashMap<int, int>(capacity * 2, allocator);
        }
        
        /// <summary>
        /// Dispose of native collections.
        /// </summary>
        public void Dispose()
        {
            if (CellToAgents.IsCreated)
            {
                CellToAgents.Dispose();
            }
        }
        
        /// <summary>
        /// Clear all entries from the spatial hash.
        /// </summary>
        public void Clear()
        {
            CellToAgents.Clear();
        }
        
        /// <summary>
        /// Convert a position to a cell hash key.
        /// </summary>
        [BurstCompile]
        public int PositionToHash(float3 position)
        {
            int x = (int)math.floor(position.x / CellSize);
            int z = (int)math.floor(position.z / CellSize);
            // Simple hash combining x and z
            unchecked
            {
                return x * 73856093 ^ z * 19349663;
            }
        }
        
        /// <summary>
        /// Get cell coordinates from position.
        /// </summary>
        [BurstCompile]
        public int2 PositionToCell(float3 position)
        {
            return new int2(
                (int)math.floor(position.x / CellSize),
                (int)math.floor(position.z / CellSize)
            );
        }
        
        /// <summary>
        /// Get hash from cell coordinates.
        /// </summary>
        [BurstCompile]
        public int CellToHash(int2 cell)
        {
            unchecked
            {
                return cell.x * 73856093 ^ cell.y * 19349663;
            }
        }
        
        /// <summary>
        /// Insert an agent at the specified position.
        /// </summary>
        public void Insert(int agentIndex, float3 position)
        {
            int hash = PositionToHash(position);
            CellToAgents.Add(hash, agentIndex);
        }
    }
    
    /// <summary>
    /// Job to build the spatial hash from agent positions.
    /// </summary>
    [BurstCompile]
    public struct BuildSpatialHashJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<AgentData> Agents;
        public NativeMultiHashMap<int, int>.ParallelWriter CellToAgents;
        public float CellSize;
        
        public void Execute(int index)
        {
            AgentData agent = Agents[index];
            if (!agent.IsActive) return;
            
            // Calculate cell hash
            int x = (int)math.floor(agent.Position.x / CellSize);
            int z = (int)math.floor(agent.Position.z / CellSize);
            int hash;
            unchecked
            {
                hash = x * 73856093 ^ z * 19349663;
            }
            
            CellToAgents.Add(hash, index);
        }
    }
}
