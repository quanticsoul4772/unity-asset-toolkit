using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EasyPath;

namespace EasyPath.Tests.Runtime
{
    /// <summary>
    /// Integration tests for the complete pathfinding system.
    /// These tests run in PlayMode and test actual gameplay scenarios.
    /// </summary>
    public class PathfindingIntegrationTests
    {
        private GameObject _gridObject;
        private EasyPathGrid _grid;
        
        [SetUp]
        public void SetUp()
        {
            _gridObject = new GameObject("TestGrid");
            _grid = _gridObject.AddComponent<EasyPathGrid>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_gridObject != null)
            {
                Object.DestroyImmediate(_gridObject);
            }
        }
        
        [UnityTest]
        public IEnumerator FindPath_OnEmptyGrid_ReturnsValidPath()
        {
            // Wait a frame for grid initialization
            yield return null;
            
            Vector3 start = _grid.GridToWorld(0, 0);
            Vector3 end = _grid.GridToWorld(5, 5);
            
            List<Vector3> path = _grid.FindPath(start, end);
            
            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0);
        }
        
        [UnityTest]
        public IEnumerator FindPath_ToSamePosition_ReturnsPath()
        {
            yield return null;
            
            Vector3 position = _grid.GridToWorld(5, 5);
            
            List<Vector3> path = _grid.FindPath(position, position);
            
            Assert.IsNotNull(path);
        }
        
        [UnityTest]
        public IEnumerator GridToWorld_AndBackToGrid_IsConsistent()
        {
            yield return null;
            
            int testX = 7;
            int testY = 12;
            
            Vector3 worldPos = _grid.GridToWorld(testX, testY);
            Vector2Int gridPos = _grid.WorldToGrid(worldPos);
            
            Assert.AreEqual(testX, gridPos.x);
            Assert.AreEqual(testY, gridPos.y);
        }
        
        [UnityTest]
        public IEnumerator IsWalkable_DefaultsToTrue()
        {
            yield return null;
            
            // Without obstacles, all cells should be walkable
            bool isWalkable = _grid.IsWalkable(5, 5);
            
            Assert.IsTrue(isWalkable);
        }
        
        [UnityTest]
        public IEnumerator SetWalkable_UpdatesNode()
        {
            yield return null;
            
            _grid.SetWalkable(5, 5, false);
            
            Assert.IsFalse(_grid.IsWalkable(5, 5));
        }
        
        [UnityTest]
        public IEnumerator ToggleWalkable_InvertsState()
        {
            yield return null;
            
            bool initialState = _grid.IsWalkable(5, 5);
            _grid.ToggleWalkable(5, 5);
            
            Assert.AreNotEqual(initialState, _grid.IsWalkable(5, 5));
        }
        
        [UnityTest]
        public IEnumerator GetNode_OutOfBounds_ReturnsNull()
        {
            yield return null;
            
            PathNode node = _grid.GetNode(-1, -1);
            
            Assert.IsNull(node);
        }
        
        [UnityTest]
        public IEnumerator GetNode_WithinBounds_ReturnsNode()
        {
            yield return null;
            
            PathNode node = _grid.GetNode(5, 5);
            
            Assert.IsNotNull(node);
            Assert.AreEqual(5, node.X);
            Assert.AreEqual(5, node.Y);
        }
        
        [UnityTest]
        public IEnumerator BuildGrid_SetsWalkableCount()
        {
            yield return null;
            
            // Grid is 20x20 by default, all walkable without obstacles
            Assert.Greater(_grid.WalkableCount, 0);
        }
    }
}
