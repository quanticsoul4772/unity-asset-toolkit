using System;
using System.Collections.Generic;

namespace EasyPath
{
    /// <summary>
    /// Min-heap priority queue optimized for A* pathfinding.
    /// </summary>
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> _heap;
        private Dictionary<T, int> _itemIndices;
        
        public int Count => _heap.Count;
        
        public PriorityQueue(int initialCapacity = 16)
        {
            _heap = new List<T>(initialCapacity);
            _itemIndices = new Dictionary<T, int>(initialCapacity);
        }
        
        public void Clear()
        {
            _heap.Clear();
            _itemIndices.Clear();
        }
        
        public bool Contains(T item)
        {
            return _itemIndices.ContainsKey(item);
        }
        
        public void Enqueue(T item)
        {
            _heap.Add(item);
            int index = _heap.Count - 1;
            _itemIndices[item] = index;
            HeapifyUp(index);
        }
        
        public T Dequeue()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("Priority queue is empty");
            }
            
            T item = _heap[0];
            int lastIndex = _heap.Count - 1;
            
            _heap[0] = _heap[lastIndex];
            _itemIndices[_heap[0]] = 0;
            _heap.RemoveAt(lastIndex);
            _itemIndices.Remove(item);
            
            if (_heap.Count > 0)
            {
                HeapifyDown(0);
            }
            
            return item;
        }
        
        public T Peek()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("Priority queue is empty");
            }
            return _heap[0];
        }
        
        public void UpdatePriority(T item)
        {
            if (!_itemIndices.TryGetValue(item, out int index))
            {
                return;
            }
            
            HeapifyUp(index);
            HeapifyDown(index);
        }
        
        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                
                if (_heap[index].CompareTo(_heap[parentIndex]) >= 0)
                {
                    break;
                }
                
                Swap(index, parentIndex);
                index = parentIndex;
            }
        }
        
        private void HeapifyDown(int index)
        {
            while (true)
            {
                int smallest = index;
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                
                if (leftChild < _heap.Count && _heap[leftChild].CompareTo(_heap[smallest]) < 0)
                {
                    smallest = leftChild;
                }
                
                if (rightChild < _heap.Count && _heap[rightChild].CompareTo(_heap[smallest]) < 0)
                {
                    smallest = rightChild;
                }
                
                if (smallest == index)
                {
                    break;
                }
                
                Swap(index, smallest);
                index = smallest;
            }
        }
        
        private void Swap(int a, int b)
        {
            T temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;
            
            _itemIndices[_heap[a]] = a;
            _itemIndices[_heap[b]] = b;
        }
    }
}
