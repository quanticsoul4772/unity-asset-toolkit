using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Demo showcasing resource gathering: GatheringState, ReturningState, and ResourceNode.
    /// Simulates a colony/RTS resource collection loop.
    /// Uses the new Unity Input System.
    /// </summary>
    public class ResourceGatheringDemo : SwarmDemoController
    {
        [Header("Resource Settings")]
        [SerializeField] private int _resourceNodeCount = 3;
        [SerializeField] private float _resourceAmount = 100f;
        [SerializeField] private float _harvestRate = 10f;
        [SerializeField] private bool _resourcesRespawn = true;
        [SerializeField] private float _respawnTime = 15f;
        
        [Header("Base Settings")]
        [SerializeField] private Vector3 _basePosition = new Vector3(0, 0, -10);
        [SerializeField] private Color _baseColor = new Color(0.2f, 0.5f, 1f);
        
        [Header("Worker Settings")]
        [SerializeField] private float _carryCapacity = 10f;
        
        // Tracked objects
        private List<ResourceNode> _resourceNodes = new List<ResourceNode>();
        private GameObject _baseObject;
        
        protected override void Start()
        {
            _demoTitle = "SwarmAI - Resource Gathering Demo";
            _spawnCenter = _basePosition + Vector3.forward * 3f;
            
            base.Start();
            
            CreateBase();
            CreateResourceNodes();
            SetupWorkers();
        }
        
        protected override void Update()
        {
            base.Update();
            
            HandleGatheringInput();
        }
        
        private void CreateBase()
        {
            _baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _baseObject.name = "HomeBase";
            _baseObject.transform.position = _basePosition + Vector3.up * 0.5f;
            _baseObject.transform.localScale = new Vector3(4f, 1f, 4f);
            
            // Set material color
            var renderer = _baseObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = _baseColor;
            }
            
            // Add a trigger collider for deposit detection
            var collider = _baseObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }
            
            Debug.Log($"[ResourceGatheringDemo] Base created at {_basePosition}");
        }
        
        private void CreateResourceNodes()
        {
            // Create resource nodes in a semicircle around the base
            float radius = 15f;
            float angleSpan = 120f; // degrees
            float startAngle = 90f - angleSpan / 2f;
            
            for (int i = 0; i < _resourceNodeCount; i++)
            {
                float angle = startAngle + (angleSpan / (_resourceNodeCount - 1)) * i;
                if (_resourceNodeCount == 1) angle = 90f;
                
                float rad = angle * Mathf.Deg2Rad;
                Vector3 position = _basePosition + new Vector3(
                    Mathf.Cos(rad) * radius,
                    0f,
                    Mathf.Sin(rad) * radius
                );
                
                CreateResourceNode(position, i);
            }
        }
        
        private void CreateResourceNode(Vector3 position, int index)
        {
            // Create visual
            GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nodeObj.name = $"ResourceNode_{index}";
            nodeObj.transform.position = position + Vector3.up * 0.5f;
            nodeObj.transform.localScale = new Vector3(2f, 1f, 2f);
            
            // Set color based on index
            Color[] resourceColors = new Color[]
            {
                new Color(0.8f, 0.6f, 0.2f), // Gold
                new Color(0.6f, 0.8f, 0.3f), // Green
                new Color(0.7f, 0.4f, 0.7f), // Purple
                new Color(0.3f, 0.7f, 0.8f), // Cyan
            };
            
            var renderer = nodeObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = resourceColors[index % resourceColors.Length];
            }
            
            // Add ResourceNode component and configure with demo settings
            ResourceNode node = nodeObj.AddComponent<ResourceNode>();
            node.Configure(_resourceAmount, _harvestRate, _resourcesRespawn, _respawnTime);
            
            // Subscribe to events
            node.OnResourceHarvested += (amount) => { };
            node.OnDepleted += () => Debug.Log($"[ResourceGatheringDemo] {nodeObj.name} depleted!");
            node.OnRespawned += () => Debug.Log($"[ResourceGatheringDemo] {nodeObj.name} respawned!");
            
            _resourceNodes.Add(node);
        }
        
        private void SetupWorkers()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                // Find nearest resource and start gathering
                var nearestResource = ResourceNode.FindNearestAvailable(agent.Position);
                if (nearestResource != null)
                {
                    agent.SetState(new GatheringState(nearestResource, _basePosition, _carryCapacity));
                }
                
                // Track deposits
                // Note: In a real implementation, you'd hook into ReturningState.OnResourcesDeposited
            }
            
            Debug.Log($"[ResourceGatheringDemo] {_agents.Count} workers assigned to gather");
        }
        
        private void HandleGatheringInput()
        {
            // G - Send all workers to gather
            if (SwarmDemoInput.ActionGPressed)
            {
                SendAllToGather();
            }
            
            // H - Send all workers home
            if (SwarmDemoInput.ActionHPressed)
            {
                SendAllHome();
            }
            
            // Left Click - Send clicked worker to clicked resource
            if (SwarmDemoInput.ClickPressed && Camera.main != null)
            {
                HandleClickAssignment();
            }
            
            // N - Spawn new resource node at random position
            if (SwarmDemoInput.ActionNPressed)
            {
                Vector3 randomPos = _basePosition + new Vector3(
                    Random.Range(-15f, 15f),
                    0f,
                    Random.Range(5f, 20f)
                );
                CreateResourceNode(randomPos, _resourceNodes.Count);
                Debug.Log("[ResourceGatheringDemo] New resource node spawned");
            }
        }
        
        private void SendAllToGather()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                var nearestResource = ResourceNode.FindNearestAvailable(agent.Position);
                if (nearestResource != null)
                {
                    agent.SetState(new GatheringState(nearestResource, _basePosition, _carryCapacity));
                }
            }
            
            Debug.Log("[ResourceGatheringDemo] All workers sent to gather");
        }
        
        private void SendAllHome()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                // If they're carrying anything, return it
                var currentState = agent.CurrentState;
                if (currentState is GatheringState gatherState && gatherState.CurrentCarry > 0)
                {
                    agent.SetState(new ReturningState(_basePosition, gatherState.CurrentCarry));
                }
                else
                {
                    agent.SetTarget(_basePosition);
                }
            }
            
            Debug.Log("[ResourceGatheringDemo] All workers returning home");
        }
        
        private void HandleClickAssignment()
        {
            if (Camera.main == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(SwarmDemoInput.MousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Check if clicked on a resource node
                ResourceNode clickedNode = hit.collider.GetComponent<ResourceNode>();
                if (clickedNode != null && !clickedNode.IsDepleted)
                {
                    // Find nearest idle worker and assign
                    SwarmAgent nearestWorker = null;
                    float nearestDist = float.MaxValue;
                    
                    foreach (var agent in _agents)
                    {
                        if (agent == null) continue;
                        if (agent.CurrentState is IdleState || agent.CurrentState is GatheringState)
                        {
                            float dist = Vector3.Distance(agent.Position, clickedNode.Position);
                            if (dist < nearestDist)
                            {
                                nearestDist = dist;
                                nearestWorker = agent;
                            }
                        }
                    }
                    
                    if (nearestWorker != null)
                    {
                        nearestWorker.SetState(new GatheringState(clickedNode, _basePosition, _carryCapacity));
                        Debug.Log($"[ResourceGatheringDemo] Worker assigned to {clickedNode.name}");
                    }
                }
            }
        }
        
        protected override void DrawDemoControls()
        {
            GUILayout.Label("Gathering Controls:", _labelStyle);
            GUILayout.Label("• G - Send all to gather");
            GUILayout.Label("• H - Send all home");
            GUILayout.Label("• N - Spawn new resource");
            GUILayout.Label("• Click resource - Assign nearest worker");
        }
        
        protected override void DrawStats()
        {
            base.DrawStats();
            
            // Count states
            int gatheringCount = 0;
            int returningCount = 0;
            int idleCount = 0;
            float totalCarrying = 0f;
            
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                var state = agent.CurrentState;
                if (state is GatheringState gs)
                {
                    gatheringCount++;
                    totalCarrying += gs.CurrentCarry;
                }
                else if (state is ReturningState rs)
                {
                    returningCount++;
                    totalCarrying += rs.CarryAmount;
                }
                else if (state is IdleState)
                {
                    idleCount++;
                }
            }
            
            GUILayout.Space(5);
            GUILayout.Label("Worker States:", _labelStyle);
            GUILayout.Label($"• Gathering: {gatheringCount}");
            GUILayout.Label($"• Returning: {returningCount}");
            GUILayout.Label($"• Idle: {idleCount}");
            GUILayout.Label($"• Total Carrying: {totalCarrying:F1}");
            
            GUILayout.Space(5);
            GUILayout.Label("Resources:", _labelStyle);
            
            int activeNodes = 0;
            float totalRemaining = 0f;
            foreach (var node in _resourceNodes)
            {
                if (node != null && !node.IsDepleted)
                {
                    activeNodes++;
                    totalRemaining += node.CurrentAmount;
                }
            }
            
            GUILayout.Label($"• Active Nodes: {activeNodes}/{_resourceNodes.Count}");
            GUILayout.Label($"• Total Remaining: {totalRemaining:F0}");
        }
        
        private void OnDrawGizmos()
        {
            // Draw base
            Gizmos.color = _baseColor;
            Gizmos.DrawWireCube(_basePosition + Vector3.up * 0.5f, new Vector3(4f, 1f, 4f));
            
            // Draw lines from workers to their targets
            if (_agents != null)
            {
                foreach (var agent in _agents)
                {
                    if (agent == null) continue;
                    
                    var state = agent.CurrentState;
                    if (state is GatheringState gs && gs.TargetResource != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(agent.Position, gs.TargetResource.Position);
                    }
                    else if (state is ReturningState)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(agent.Position, _basePosition);
                    }
                }
            }
        }
    }
}
