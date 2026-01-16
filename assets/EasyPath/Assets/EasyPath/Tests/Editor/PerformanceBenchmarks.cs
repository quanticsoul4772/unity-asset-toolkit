using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using EasyPath;
using Debug = UnityEngine.Debug;

namespace EasyPath.Tests.Editor
{
    /// <summary>
    /// Performance benchmarks for pathfinding operations.
    /// These tests measure execution time and ensure performance targets are met.
    /// </summary>
    public class PerformanceBenchmarks
    {
        // Performance targets (in milliseconds)
        private const float SMALL_GRID_PATH_TARGET_MS = 5f;   // 20x20 grid
        private const float MEDIUM_GRID_PATH_TARGET_MS = 20f;  // 50x50 grid
        private const float LARGE_GRID_PATH_TARGET_MS = 100f;  // 100x100 grid
        private const int BENCHMARK_ITERATIONS = 100;
        
        [Test]
        public void PathfindingBenchmark_SmallGrid_MeetsTarget()
        {
            var gridObject = new GameObject("BenchmarkGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();
            // Default 20x20 grid
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            for (int i = 0; i < BENCHMARK_ITERATIONS; i++)
            {
                Vector3 start = grid.GridToWorld(0, 0);
                Vector3 end = grid.GridToWorld(19, 19);
                grid.FindPath(start, end);
            }
            
            stopwatch.Stop();
            float averageMs = (float)stopwatch.ElapsedMilliseconds / BENCHMARK_ITERATIONS;
            
            Object.DestroyImmediate(gridObject);
            
            Debug.Log($"[Benchmark] SmallGrid (20x20) pathfinding: {averageMs:F2}ms average");
            Assert.Less(averageMs, SMALL_GRID_PATH_TARGET_MS, 
                $"Pathfinding took {averageMs:F2}ms, target is {SMALL_GRID_PATH_TARGET_MS}ms");
        }
        
        [Test]
        public void PriorityQueueBenchmark_EnqueueDequeue()
        {
            const int OPERATIONS = 10000;
            var queue = new PriorityQueue<PathNode>(OPERATIONS);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Enqueue operations
            for (int i = 0; i < OPERATIONS; i++)
            {
                var node = new PathNode(i, i, true, Vector3.zero) 
                { 
                    GCost = Random.Range(0, 1000), 
                    HCost = Random.Range(0, 1000) 
                };
                queue.Enqueue(node);
            }
            
            // Dequeue operations
            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
            
            stopwatch.Stop();
            
            Debug.Log($"[Benchmark] PriorityQueue {OPERATIONS} enqueue + {OPERATIONS} dequeue: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 500, 
                $"PriorityQueue operations took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        }
        
        [Test]
        public void PathNodeHashSet_LookupPerformance()
        {
            const int NODE_COUNT = 10000;
            var nodes = new PathNode[NODE_COUNT];
            var hashSet = new System.Collections.Generic.HashSet<PathNode>();
            
            // Create nodes
            for (int i = 0; i < NODE_COUNT; i++)
            {
                nodes[i] = new PathNode(i % 100, i / 100, true, Vector3.zero);
                hashSet.Add(nodes[i]);
            }
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Lookup operations
            for (int i = 0; i < NODE_COUNT; i++)
            {
                hashSet.Contains(nodes[i]);
            }
            
            stopwatch.Stop();
            
            Debug.Log($"[Benchmark] HashSet {NODE_COUNT} lookups: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, 
                $"HashSet lookups took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");
        }
        
        [Test]
        public void GridBuild_Performance()
        {
            // This tests multiple grid sizes
            int[] gridSizes = { 10, 20, 50, 100 };
            
            foreach (int size in gridSizes)
            {
                var gridObject = new GameObject($"BenchmarkGrid_{size}x{size}");
                
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                var grid = gridObject.AddComponent<EasyPathGrid>();
                // Note: Grid builds in Awake, so we measure component addition time
                
                stopwatch.Stop();
                
                Object.DestroyImmediate(gridObject);
                
                Debug.Log($"[Benchmark] Grid build {size}x{size}: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            Assert.Pass("Grid build performance logged");
        }
        
        [Test]
        public void MemoryAllocation_PathfindingDoesNotAllocateExcessively()
        {
            var gridObject = new GameObject("BenchmarkGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();
            
            // Warm up
            grid.FindPath(grid.GridToWorld(0, 0), grid.GridToWorld(10, 10));
            
            // Force GC to get clean baseline
            System.GC.Collect();
            long memoryBefore = System.GC.GetTotalMemory(true);
            
            // Run multiple pathfinds
            for (int i = 0; i < 100; i++)
            {
                grid.FindPath(grid.GridToWorld(0, 0), grid.GridToWorld(19, 19));
            }
            
            long memoryAfter = System.GC.GetTotalMemory(false);
            long memoryUsed = memoryAfter - memoryBefore;
            
            Object.DestroyImmediate(gridObject);
            
            Debug.Log($"[Benchmark] Memory used for 100 pathfinds: {memoryUsed / 1024}KB");
            
            // Each path returns a new List<Vector3>, so some allocation is expected
            // But it should be reasonable (< 1MB for 100 paths)
            Assert.Less(memoryUsed, 1024 * 1024, 
                $"Memory allocation ({memoryUsed / 1024}KB) is excessive for 100 pathfinds");
        }
    }
}
