using NUnit.Framework;
using UnityEngine;
using EasyPath;

namespace EasyPath.Tests.Editor
{
    /// <summary>
    /// Unit tests for the PathNode class.
    /// </summary>
    public class PathNodeTests
    {
        [Test]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var node = new PathNode(5, 10, true, new Vector3(1, 0, 2));
            
            Assert.AreEqual(5, node.X);
            Assert.AreEqual(10, node.Y);
            Assert.IsTrue(node.IsWalkable);
            Assert.AreEqual(new Vector3(1, 0, 2), node.WorldPosition);
        }
        
        [Test]
        public void Position_ReturnsCorrectVector2Int()
        {
            var node = new PathNode(3, 7, true, Vector3.zero);
            
            Assert.AreEqual(new Vector2Int(3, 7), node.Position);
        }
        
        [Test]
        public void Reset_SetsGCostToMaxValue()
        {
            var node = new PathNode(0, 0, true, Vector3.zero);
            node.GCost = 100;
            node.HCost = 50;
            node.Parent = new PathNode(1, 1, true, Vector3.zero);
            
            node.Reset();
            
            Assert.AreEqual(int.MaxValue, node.GCost);
            Assert.AreEqual(0, node.HCost);
            Assert.IsNull(node.Parent);
        }
        
        [Test]
        public void FCost_ReturnsSumOfGAndHCost()
        {
            var node = new PathNode(0, 0, true, Vector3.zero);
            node.GCost = 30;
            node.HCost = 20;
            
            Assert.AreEqual(50, node.FCost);
        }
        
        [Test]
        public void CompareTo_ComparesbyFCostThenHCost()
        {
            var nodeA = new PathNode(0, 0, true, Vector3.zero) { GCost = 10, HCost = 20 }; // FCost = 30
            var nodeB = new PathNode(1, 1, true, Vector3.zero) { GCost = 20, HCost = 20 }; // FCost = 40
            var nodeC = new PathNode(2, 2, true, Vector3.zero) { GCost = 15, HCost = 15 }; // FCost = 30, lower HCost
            
            Assert.Less(nodeA.CompareTo(nodeB), 0); // A < B (lower FCost)
            Assert.Greater(nodeB.CompareTo(nodeA), 0); // B > A
            Assert.Less(nodeC.CompareTo(nodeA), 0); // C < A (same FCost, lower HCost)
        }
        
        [Test]
        public void Equals_ReturnsTrueForSameCoordinates()
        {
            var nodeA = new PathNode(5, 10, true, Vector3.zero);
            var nodeB = new PathNode(5, 10, false, new Vector3(100, 100, 100));
            
            Assert.IsTrue(nodeA.Equals(nodeB));
        }
        
        [Test]
        public void Equals_ReturnsFalseForDifferentCoordinates()
        {
            var nodeA = new PathNode(5, 10, true, Vector3.zero);
            var nodeB = new PathNode(5, 11, true, Vector3.zero);
            
            Assert.IsFalse(nodeA.Equals(nodeB));
        }
        
        [Test]
        public void GetHashCode_SameForEqualNodes()
        {
            var nodeA = new PathNode(5, 10, true, Vector3.zero);
            var nodeB = new PathNode(5, 10, false, Vector3.one);
            
            Assert.AreEqual(nodeA.GetHashCode(), nodeB.GetHashCode());
        }
        
        [Test]
        public void GetHashCode_UniqueForDifferentPositions()
        {
            var nodeA = new PathNode(0, 1, true, Vector3.zero);
            var nodeB = new PathNode(1, 0, true, Vector3.zero);
            
            Assert.AreNotEqual(nodeA.GetHashCode(), nodeB.GetHashCode());
        }
        
        [Test]
        public void MovementPenalty_DefaultsToZero()
        {
            var node = new PathNode(0, 0, true, Vector3.zero);
            
            Assert.AreEqual(0, node.MovementPenalty);
        }
        
        [Test]
        public void MovementPenalty_CanBeModified()
        {
            var node = new PathNode(0, 0, true, Vector3.zero);
            node.MovementPenalty = 15;
            
            Assert.AreEqual(15, node.MovementPenalty);
        }
        
        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var node = new PathNode(3, 7, true, Vector3.zero);
            node.GCost = 10;
            node.HCost = 5;
            
            StringAssert.Contains("3", node.ToString());
            StringAssert.Contains("7", node.ToString());
        }
    }
}
