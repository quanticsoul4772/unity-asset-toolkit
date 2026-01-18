using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain
{
    /// <summary>
    /// Static registry for tracking active NPCs in the scene.
    /// Provides O(1) access to all NPCs of a specific type instead of expensive FindObjectsOfType calls.
    /// </summary>
    /// <typeparam name="T">The type of NPC being registered.</typeparam>
    public static class NPCRegistry<T> where T : NPCBrainController
    {
        private static readonly List<T> _instances = new List<T>();
        private static readonly HashSet<T> _instanceSet = new HashSet<T>();
        private static T[] _cachedArray;
        private static bool _isDirty = true;
        
        /// <summary>All active instances of this NPC type.</summary>
        public static IReadOnlyList<T> Instances => _instances;
        
        /// <summary>Number of active instances.</summary>
        public static int Count => _instances.Count;
        
        /// <summary>
        /// Gets all instances as an array. Caches the array for performance.
        /// </summary>
        public static T[] GetAll()
        {
            if (_isDirty || _cachedArray == null)
            {
                _cachedArray = _instances.ToArray();
                _isDirty = false;
            }
            return _cachedArray;
        }
        
        /// <summary>
        /// Registers an NPC instance. Call from Awake() or OnEnable().
        /// </summary>
        public static void Register(T instance)
        {
            if (instance == null) return;
            // Use HashSet for O(1) lookup instead of List.Contains() which is O(n)
            if (_instanceSet.Add(instance))
            {
                _instances.Add(instance);
                _isDirty = true;
            }
        }
        
        /// <summary>
        /// Unregisters an NPC instance. Call from OnDestroy() or OnDisable().
        /// </summary>
        public static void Unregister(T instance)
        {
            if (instance == null) return;
            if (_instanceSet.Remove(instance))
            {
                _instances.Remove(instance);
                _isDirty = true;
            }
        }
        
        /// <summary>
        /// Clears all registered instances. Call on scene unload.
        /// </summary>
        public static void Clear()
        {
            _instances.Clear();
            _instanceSet.Clear();
            _cachedArray = null;
            _isDirty = true;
        }
        
        /// <summary>
        /// Finds the nearest instance to a position.
        /// </summary>
        public static T FindNearest(Vector3 position, float maxDistance = float.MaxValue)
        {
            T nearest = null;
            float nearestDistSqr = maxDistance * maxDistance;
            
            // Use for loop instead of foreach to avoid enumerator allocation
            for (int i = 0; i < _instances.Count; i++)
            {
                var instance = _instances[i];
                if (instance == null || !instance.gameObject.activeSelf) continue;
                
                float distSqr = (instance.transform.position - position).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = instance;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Gets all instances within a radius of a position.
        /// </summary>
        public static void GetInRadius(Vector3 position, float radius, List<T> results)
        {
            results.Clear();
            float radiusSqr = radius * radius;
            
            // Use for loop instead of foreach to avoid enumerator allocation
            for (int i = 0; i < _instances.Count; i++)
            {
                var instance = _instances[i];
                if (instance == null || !instance.gameObject.activeSelf) continue;
                
                float distSqr = (instance.transform.position - position).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    results.Add(instance);
                }
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instances.Clear();
            _instanceSet.Clear();
            _cachedArray = null;
            _isDirty = true;
        }
    }
}
