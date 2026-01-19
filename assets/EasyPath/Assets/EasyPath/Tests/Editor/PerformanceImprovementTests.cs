using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using EasyPath;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace EasyPath.Tests.Editor
{
    /// <summary>
    /// Tests to validate the performance improvements implemented.
    /// Each test verifies a specific optimization is working correctly.
    /// </summary>
    public class PerformanceImprovementTests
    {
        private const int ITERATIONS = 1000;

        #region Version-Based Reset Tests

        [Test]
        [Category("Performance")]
        public void VersionBasedReset_IsOOneComplexity()
        {
            // Verify that IncrementPathVersion() is O(1) regardless of grid size
            var gridObject = new GameObject("TestGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();

            var stopwatch = new Stopwatch();

            // Measure time for 10,000 version increments
            stopwatch.Start();
            for (int i = 0; i < 10000; i++)
            {
                grid.IncrementPathVersion();
            }
            stopwatch.Stop();

            Object.DestroyImmediate(gridObject);

            // Log timing for local inspection
            Debug.Log($"[VersionReset] 10,000 IncrementPathVersion calls: {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds}ms)");

            // Skip hard timing assertion in CI environments (batch mode or CI env var)
            bool isCI = Application.isBatchMode || System.Environment.GetEnvironmentVariable("CI") != null;
            if (isCI)
            {
                Assert.Ignore("Performance timing test skipped in CI environment (run locally for timing validation)");
            }

            // Local-only assertion: should complete in < 5ms (it's just incrementing an int)
            Assert.Less(stopwatch.ElapsedMilliseconds, 5, "Version increment should be nearly instant");
        }

        [Test]
        public void LazyNodeReset_OnlyResetsAccessedNodes()
        {
            var gridObject = new GameObject("TestGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();

            // Get a node and mark it
            var node = grid.GetNode(5, 5);
            node.GCost = 100;
            node.HCost = 50;
            node.LastUsedVersion = grid.CurrentPathVersion;

            // Increment version
            grid.IncrementPathVersion();

            // Node should still have old values until ResetNodeIfNeeded is called
            Assert.AreEqual(100, node.GCost, "GCost should remain until lazy reset");

            // Now reset via lazy check
            grid.ResetNodeIfNeeded(node);

            // After reset, should have default values
            Assert.AreEqual(int.MaxValue, node.GCost, "GCost should be reset after ResetNodeIfNeeded");
            Assert.AreEqual(grid.CurrentPathVersion, node.LastUsedVersion, "Version should be updated");

            Object.DestroyImmediate(gridObject);
        }

        #endregion

        #region GetNeighbors Buffer Tests

        [Test]
        public void GetNeighbors_UsesPreAllocatedBuffer()
        {
            var gridObject = new GameObject("TestGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();

            var node = grid.GetNode(10, 10);
            var buffer = new List<PathNode>(8);

            // Force GC
            System.GC.Collect();
            long memBefore = System.GC.GetTotalMemory(true);

            // Call GetNeighbors many times with pre-allocated buffer
            for (int i = 0; i < ITERATIONS; i++)
            {
                grid.GetNeighbors(node, buffer);
            }

            long memAfter = System.GC.GetTotalMemory(false);
            long allocated = memAfter - memBefore;

            Object.DestroyImmediate(gridObject);

            Debug.Log($"[GetNeighbors] Memory allocated for {ITERATIONS} calls: {allocated} bytes");
            // With buffer reuse, should allocate very little (mostly GC overhead)
            Assert.Less(allocated, 10000, "GetNeighbors with buffer should have minimal allocation");
        }

        [Test]
        public void GetNeighbors_ReturnsCorrectCount()
        {
            var gridObject = new GameObject("TestGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();

            // Center node should have 8 neighbors in a walkable grid
            var centerNode = grid.GetNode(10, 10);
            var buffer = new List<PathNode>(8);
            grid.GetNeighbors(centerNode, buffer);

            // Should have 8 neighbors (all diagonal + cardinal)
            Assert.AreEqual(8, buffer.Count, "Center node should have 8 neighbors");

            // Corner node should have 3 neighbors
            var cornerNode = grid.GetNode(0, 0);
            grid.GetNeighbors(cornerNode, buffer);
            Assert.AreEqual(3, buffer.Count, "Corner node should have 3 neighbors");

            Object.DestroyImmediate(gridObject);
        }

        #endregion

        #region ReconstructPath Stack Tests

        [Test]
        public void PathfindingReturnsCorrectPath()
        {
            var gridObject = new GameObject("TestGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();

            Vector3 start = grid.GridToWorld(0, 0);
            Vector3 end = grid.GridToWorld(5, 5);

            var path = grid.FindPath(start, end);

            Assert.IsNotNull(path, "Path should not be null");
            Assert.Greater(path.Count, 0, "Path should have waypoints");

            // First waypoint should be near start
            float startDist = Vector3.Distance(path[0], start);
            Assert.Less(startDist, 2f, "First waypoint should be near start");

            // Last waypoint should be near end
            float endDist = Vector3.Distance(path[path.Count - 1], end);
            Assert.Less(endDist, 2f, "Last waypoint should be near end");

            Object.DestroyImmediate(gridObject);
        }

        #endregion

        #region Comparative Performance Tests

        [Test]
        public void MultiplePathfinds_NoExcessiveMemoryGrowth()
        {
            var gridObject = new GameObject("TestGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();

            // Warmup
            for (int i = 0; i < 10; i++)
            {
                grid.FindPath(grid.GridToWorld(0, 0), grid.GridToWorld(19, 19));
            }

            System.GC.Collect();
            long memBefore = System.GC.GetTotalMemory(true);

            // Run many pathfinds
            for (int i = 0; i < 500; i++)
            {
                var path = grid.FindPath(
                    grid.GridToWorld(Random.Range(0, 10), Random.Range(0, 10)),
                    grid.GridToWorld(Random.Range(10, 19), Random.Range(10, 19))
                );
            }

            long memAfter = System.GC.GetTotalMemory(false);
            long memUsed = memAfter - memBefore;

            Object.DestroyImmediate(gridObject);

            Debug.Log($"[Pathfinding] Memory for 500 pathfinds: {memUsed / 1024}KB");
            // Each path creates a new List<Vector3>, but internal operations should be allocation-free
            // Expect ~500KB max (each path ~1KB max with ~20 waypoints)
            Assert.Less(memUsed, 1024 * 1024, "Memory growth should be reasonable");
        }

        [Test]
        public void PathfindingPerformance_MeetsTargets()
        {
            var gridObject = new GameObject("TestGrid");
            var grid = gridObject.AddComponent<EasyPathGrid>();

            var stopwatch = new Stopwatch();

            // Warmup
            var warmupPath = grid.FindPath(grid.GridToWorld(0, 0), grid.GridToWorld(19, 19));
            Assert.IsNotNull(warmupPath, "Warmup path should succeed");

            stopwatch.Start();
            for (int i = 0; i < 100; i++)
            {
                var path = grid.FindPath(grid.GridToWorld(0, 0), grid.GridToWorld(19, 19));
                // Verify each pathfind succeeds (functional correctness)
                Assert.IsNotNull(path, $"Pathfind {i} should return a valid path");
                Assert.Greater(path.Count, 0, $"Pathfind {i} should have waypoints");
            }
            stopwatch.Stop();

            float avgMs = (float)stopwatch.ElapsedMilliseconds / 100f;

            Object.DestroyImmediate(gridObject);

            // Log performance metrics for monitoring (no hard threshold to avoid CI flakiness)
            Debug.Log($"[Pathfinding] Average time per pathfind (20x20): {avgMs:F3}ms");
            Debug.Log($"[Pathfinding] Total time for 100 pathfinds: {stopwatch.ElapsedMilliseconds}ms");

            // Only assert that pathfinding completes in a reasonable time (10ms is very generous)
            // This prevents test failures on slower hardware while still catching catastrophic performance regressions
            Assert.Less(avgMs, 10f, "Pathfinding should complete in < 10ms on 20x20 grid (sanity check for catastrophic regressions)");
        }

        #endregion
    }
}
