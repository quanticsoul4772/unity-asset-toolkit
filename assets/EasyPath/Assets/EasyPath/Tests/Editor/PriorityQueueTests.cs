using NUnit.Framework;
using UnityEngine;
using EasyPath;

namespace EasyPath.Tests.Editor
{
    /// <summary>
    /// Unit tests for the PriorityQueue class.
    /// </summary>
    public class PriorityQueueTests
    {
        [Test]
        public void Constructor_CreatesEmptyQueue()
        {
            var queue = new PriorityQueue<PathNode>(16);
            
            Assert.AreEqual(0, queue.Count);
        }
        
        [Test]
        public void Enqueue_IncreasesCount()
        {
            var queue = new PriorityQueue<PathNode>(16);
            var node = new PathNode(0, 0, true, Vector3.zero) { GCost = 10, HCost = 5 };
            
            queue.Enqueue(node);
            
            Assert.AreEqual(1, queue.Count);
        }
        
        [Test]
        public void Dequeue_ReturnsLowestPriorityFirst()
        {
            var queue = new PriorityQueue<PathNode>(16);
            
            var highNode = new PathNode(0, 0, true, Vector3.zero) { GCost = 50, HCost = 50 }; // FCost = 100
            var lowNode = new PathNode(1, 1, true, Vector3.zero) { GCost = 5, HCost = 5 };   // FCost = 10
            var midNode = new PathNode(2, 2, true, Vector3.zero) { GCost = 25, HCost = 25 }; // FCost = 50
            
            queue.Enqueue(highNode);
            queue.Enqueue(lowNode);
            queue.Enqueue(midNode);
            
            Assert.AreEqual(lowNode, queue.Dequeue());  // FCost 10
            Assert.AreEqual(midNode, queue.Dequeue());  // FCost 50
            Assert.AreEqual(highNode, queue.Dequeue()); // FCost 100
        }
        
        [Test]
        public void Dequeue_DecreasesCount()
        {
            var queue = new PriorityQueue<PathNode>(16);
            var node = new PathNode(0, 0, true, Vector3.zero) { GCost = 10, HCost = 5 };
            
            queue.Enqueue(node);
            queue.Dequeue();
            
            Assert.AreEqual(0, queue.Count);
        }
        
        [Test]
        public void Clear_ResetsCount()
        {
            var queue = new PriorityQueue<PathNode>(16);
            
            queue.Enqueue(new PathNode(0, 0, true, Vector3.zero) { GCost = 10, HCost = 5 });
            queue.Enqueue(new PathNode(1, 1, true, Vector3.zero) { GCost = 20, HCost = 10 });
            queue.Enqueue(new PathNode(2, 2, true, Vector3.zero) { GCost = 30, HCost = 15 });
            
            queue.Clear();
            
            Assert.AreEqual(0, queue.Count);
        }
        
        [Test]
        public void Contains_ReturnsTrueForExistingNode()
        {
            var queue = new PriorityQueue<PathNode>(16);
            var node = new PathNode(5, 5, true, Vector3.zero) { GCost = 10, HCost = 5 };
            
            queue.Enqueue(node);
            
            Assert.IsTrue(queue.Contains(node));
        }
        
        [Test]
        public void Contains_ReturnsFalseForNonExistingNode()
        {
            var queue = new PriorityQueue<PathNode>(16);
            var nodeA = new PathNode(0, 0, true, Vector3.zero) { GCost = 10, HCost = 5 };
            var nodeB = new PathNode(1, 1, true, Vector3.zero) { GCost = 20, HCost = 10 };
            
            queue.Enqueue(nodeA);
            
            Assert.IsFalse(queue.Contains(nodeB));
        }
        
        [Test]
        public void UpdatePriority_ReordersProperly()
        {
            var queue = new PriorityQueue<PathNode>(16);
            
            var nodeA = new PathNode(0, 0, true, Vector3.zero) { GCost = 50, HCost = 50 }; // FCost = 100
            var nodeB = new PathNode(1, 1, true, Vector3.zero) { GCost = 25, HCost = 25 }; // FCost = 50
            
            queue.Enqueue(nodeA);
            queue.Enqueue(nodeB);
            
            // Update nodeA to have lower cost
            nodeA.GCost = 5;
            nodeA.HCost = 5; // FCost = 10
            queue.UpdatePriority(nodeA);
            
            Assert.AreEqual(nodeA, queue.Dequeue()); // Now nodeA should come first
        }
        
        [Test]
        public void MultipleEnqueueDequeue_MaintainsOrder()
        {
            var queue = new PriorityQueue<PathNode>(16);
            
            // Add nodes with various costs
            for (int i = 10; i >= 1; i--)
            {
                var node = new PathNode(i, i, true, Vector3.zero) { GCost = i * 10, HCost = i * 5 };
                queue.Enqueue(node);
            }
            
            int previousFCost = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                Assert.GreaterOrEqual(node.FCost, previousFCost);
                previousFCost = node.FCost;
            }
        }
        
        [Test]
        public void LargeCapacity_HandledCorrectly()
        {
            var queue = new PriorityQueue<PathNode>(1000);
            
            for (int i = 0; i < 500; i++)
            {
                var node = new PathNode(i, i, true, Vector3.zero) { GCost = i, HCost = 0 };
                queue.Enqueue(node);
            }
            
            Assert.AreEqual(500, queue.Count);
            
            // Verify ordering
            int expected = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                Assert.AreEqual(expected, node.GCost);
                expected++;
            }
        }
    }
}
