using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for the SpatialHash class.
    /// </summary>
    [TestFixture]
    public class SpatialHashTests
    {
        private class TestItem
        {
            public Vector3 Position { get; set; }
            public string Name { get; set; }
            
            public TestItem(Vector3 pos, string name)
            {
                Position = pos;
                Name = name;
            }
        }
        
        private SpatialHash<TestItem> _spatialHash;
        
        [SetUp]
        public void SetUp()
        {
            _spatialHash = new SpatialHash<TestItem>(10f);
        }
        
        [Test]
        public void Insert_AddsItemToHash()
        {
            var item = new TestItem(Vector3.zero, "test");
            
            _spatialHash.Insert(item, Vector3.zero);
            
            Assert.AreEqual(1, _spatialHash.Count);
            Assert.IsTrue(_spatialHash.Contains(item));
        }
        
        [Test]
        public void Insert_NullItem_DoesNothing()
        {
            _spatialHash.Insert(null, Vector3.zero);
            
            Assert.AreEqual(0, _spatialHash.Count);
        }
        
        [Test]
        public void Insert_SameItemTwice_UpdatesPosition()
        {
            var item = new TestItem(Vector3.zero, "test");
            
            _spatialHash.Insert(item, Vector3.zero);
            _spatialHash.Insert(item, new Vector3(100, 0, 100));
            
            Assert.AreEqual(1, _spatialHash.Count);
        }
        
        [Test]
        public void Remove_RemovesItemFromHash()
        {
            var item = new TestItem(Vector3.zero, "test");
            _spatialHash.Insert(item, Vector3.zero);
            
            _spatialHash.Remove(item);
            
            Assert.AreEqual(0, _spatialHash.Count);
            Assert.IsFalse(_spatialHash.Contains(item));
        }
        
        [Test]
        public void Remove_NullItem_DoesNothing()
        {
            _spatialHash.Remove(null);
            
            Assert.AreEqual(0, _spatialHash.Count);
        }
        
        [Test]
        public void Query_ReturnsItemsInRadius()
        {
            var item1 = new TestItem(Vector3.zero, "near");
            var item2 = new TestItem(new Vector3(5, 0, 0), "also near");
            var item3 = new TestItem(new Vector3(100, 0, 100), "far");
            
            _spatialHash.Insert(item1, item1.Position);
            _spatialHash.Insert(item2, item2.Position);
            _spatialHash.Insert(item3, item3.Position);
            
            var results = _spatialHash.Query(Vector3.zero, 10f);
            
            Assert.IsTrue(results.Contains(item1));
            Assert.IsTrue(results.Contains(item2));
            Assert.IsFalse(results.Contains(item3));
        }
        
        [Test]
        public void Query_WithPreallocatedList_ReducesAllocation()
        {
            var item = new TestItem(Vector3.zero, "test");
            _spatialHash.Insert(item, Vector3.zero);
            
            var results = new List<TestItem>();
            _spatialHash.Query(Vector3.zero, 10f, results);
            
            Assert.AreEqual(1, results.Count);
            Assert.AreSame(item, results[0]);
        }
        
        [Test]
        public void Query_WithDistanceFilter_FiltersAccurately()
        {
            // Item at corner of cell - within cell but outside radius
            var item1 = new TestItem(new Vector3(9, 0, 9), "corner");
            var item2 = new TestItem(Vector3.zero, "center");
            
            _spatialHash.Insert(item1, item1.Position);
            _spatialHash.Insert(item2, item2.Position);
            
            var results = new List<TestItem>();
            _spatialHash.Query(Vector3.zero, 5f, results, i => i.Position);
            
            Assert.AreEqual(1, results.Count);
            Assert.AreSame(item2, results[0]);
        }
        
        [Test]
        public void QueryExcluding_ExcludesSpecifiedItem()
        {
            var item1 = new TestItem(Vector3.zero, "one");
            var item2 = new TestItem(new Vector3(1, 0, 0), "two");
            
            _spatialHash.Insert(item1, item1.Position);
            _spatialHash.Insert(item2, item2.Position);
            
            var results = _spatialHash.QueryExcluding(Vector3.zero, 10f, item1);
            
            Assert.AreEqual(1, results.Count);
            Assert.AreSame(item2, results[0]);
        }
        
        [Test]
        public void UpdatePosition_MovesItemBetweenCells()
        {
            var item = new TestItem(Vector3.zero, "test");
            _spatialHash.Insert(item, Vector3.zero);
            
            // Move to a different cell
            _spatialHash.UpdatePosition(item, new Vector3(50, 0, 50));
            
            Assert.AreEqual(1, _spatialHash.Count);
            
            // Should not be found at origin
            var resultsAtOrigin = _spatialHash.Query(Vector3.zero, 5f);
            Assert.IsFalse(resultsAtOrigin.Contains(item));
            
            // Should be found at new position
            var resultsAtNew = _spatialHash.Query(new Vector3(50, 0, 50), 5f);
            Assert.IsTrue(resultsAtNew.Contains(item));
        }
        
        [Test]
        public void UpdatePosition_SameCell_DoesNotReinsert()
        {
            var item = new TestItem(Vector3.zero, "test");
            _spatialHash.Insert(item, Vector3.zero);
            
            // Move within same cell (cell size is 10)
            _spatialHash.UpdatePosition(item, new Vector3(1, 0, 1));
            
            Assert.AreEqual(1, _spatialHash.Count);
        }
        
        [Test]
        public void Clear_RemovesAllItems()
        {
            _spatialHash.Insert(new TestItem(Vector3.zero, "one"), Vector3.zero);
            _spatialHash.Insert(new TestItem(Vector3.one, "two"), Vector3.one);
            
            _spatialHash.Clear();
            
            Assert.AreEqual(0, _spatialHash.Count);
        }
        
        [Test]
        public void TryGetCell_ReturnsCorrectCell()
        {
            var item = new TestItem(new Vector3(15, 0, 25), "test");
            _spatialHash.Insert(item, item.Position);
            
            bool found = _spatialHash.TryGetCell(item, out Vector2Int cell);
            
            Assert.IsTrue(found);
            Assert.AreEqual(1, cell.x); // 15 / 10 = 1
            Assert.AreEqual(2, cell.y); // 25 / 10 = 2
        }
        
        [Test]
        public void CellSize_ReturnsCorrectValue()
        {
            Assert.AreEqual(10f, _spatialHash.CellSize);
        }
        
        [Test]
        public void Constructor_MinimumCellSize_Enforced()
        {
            var hash = new SpatialHash<TestItem>(0.01f);
            
            Assert.GreaterOrEqual(hash.CellSize, 0.1f);
        }
    }
}
