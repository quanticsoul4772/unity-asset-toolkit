using System.Collections.Generic;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.UtilityAI;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Test scene controller demonstrating Utility AI + Criticality behavior variation.
    /// NPCs choose between Patrol, Wander, and Idle based on utility scores.
    /// Criticality adjusts selection randomness over time.
    /// </summary>
    public class TestSceneController : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private int _npcCount = 3;
        [SerializeField] private float _spawnRadius = 5f;
        
        [Header("Behavior Weights")]
        [SerializeField] private float _patrolWeight = 0.6f;
        [SerializeField] private float _wanderWeight = 0.3f;
        [SerializeField] private float _idleWeight = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;
        
        private readonly List<TestNPC> _npcs = new List<TestNPC>();
        private float _lastDebugTime;
        
        private void Start()
        {
            SpawnNPCs();
        }
        
        private void SpawnNPCs()
        {
            for (int i = 0; i < _npcCount; i++)
            {
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * _spawnRadius;
                spawnPos.y = 0f;
                
                var npcObject = CreateNPCObject($"NPC_{i}", spawnPos);
                var npc = npcObject.AddComponent<TestNPC>();
                npc.Initialize(_patrolWeight, _wanderWeight, _idleWeight);
                _npcs.Add(npc);
            }
        }
        
        private GameObject CreateNPCObject(string name, Vector3 position)
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = name;
            npc.transform.position = position;
            npc.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Add visual indicator for current action
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "ActionIndicator";
            indicator.transform.SetParent(npc.transform);
            indicator.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            indicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            // Remove colliders to avoid physics issues
            Destroy(npc.GetComponent<Collider>());
            Destroy(indicator.GetComponent<Collider>());
            
            return npc;
        }
        
        private void OnGUI()
        {
            if (!_showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 350, 400));
            GUILayout.Label("<b>NPCBrain Test Scene - Week 3 Validation</b>");
            GUILayout.Label("Demonstrating Utility AI + Criticality behavior variation");
            GUILayout.Space(10);
            
            foreach (var npc in _npcs)
            {
                if (npc == null) continue;
                
                GUILayout.BeginVertical("box");
                GUILayout.Label($"<b>{npc.name}</b>");
                GUILayout.Label($"  Current Action: {npc.CurrentActionName}");
                GUILayout.Label($"  Temperature: {npc.Temperature:F2}");
                GUILayout.Label($"  Entropy: {npc.Entropy:F2}");
                GUILayout.Label($"  Inertia: {npc.Inertia:F2}");
                GUILayout.EndVertical();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("<i>Watch temperature adjust based on action variety.</i>");
            GUILayout.Label("<i>Low entropy -> Higher temp -> More exploration</i>");
            GUILayout.Label("<i>High entropy -> Lower temp -> More exploitation</i>");
            
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// Test NPC that uses UtilitySelector for action selection.
    /// </summary>
    public class TestNPC : NPCBrainController
    {
        private UtilitySelector _utilitySelector;
        private string _currentActionName = "None";
        private Renderer _indicatorRenderer;
        
        private readonly Color _patrolColor = Color.blue;
        private readonly Color _wanderColor = Color.yellow;
        private readonly Color _idleColor = Color.gray;
        
        public string CurrentActionName => _currentActionName;
        public float Temperature => Criticality?.Temperature ?? 1f;
        public float Entropy => Criticality?.Entropy ?? 0f;
        public float Inertia => Criticality?.Inertia ?? 0.5f;
        
        public void Initialize(float patrolWeight, float wanderWeight, float idleWeight)
        {
            // Get indicator renderer
            var indicator = transform.Find("ActionIndicator");
            if (indicator != null)
            {
                _indicatorRenderer = indicator.GetComponent<Renderer>();
            }
        
            // Create utility actions
            var patrolAction = new UtilityAction(
                "Patrol",
                CreatePatrolBehavior(),
                new ConstantConsideration("PatrolDesire", patrolWeight)
            );
            
            var wanderAction = new UtilityAction(
                "Wander",
                CreateWanderBehavior(),
                new ConstantConsideration("WanderDesire", wanderWeight)
            );
            
            var idleAction = new UtilityAction(
                "Idle",
                new Wait(2f),
                new ConstantConsideration("IdleDesire", idleWeight)
            );
            
            // Create utility selector (the brain uses this as its behavior tree)
            _utilitySelector = new UtilitySelector(patrolAction, wanderAction, idleAction);
            SetBehaviorTree(_utilitySelector);
        }
        
        private BTNode CreatePatrolBehavior()
        {
            // Simple patrol: move to random point, wait, repeat
            return new Sequence(
                new MoveTo(() => GetRandomPoint(3f), 0.5f, 2f),
                new Wait(1f)
            );
        }
        
        private BTNode CreateWanderBehavior()
        {
            // Wander: move to nearby random point quickly
            return new MoveTo(() => GetRandomPoint(1.5f), 0.3f, 1.5f);
        }
        
        private Vector3 GetRandomPoint(float radius)
        {
            Vector3 randomDir = Random.insideUnitSphere * radius;
            randomDir.y = 0f;
            return transform.position + randomDir;
        }
        
        private void LateUpdate()
        {
            // Update current action name for debug display
            if (_utilitySelector?.CurrentAction != null)
            {
                string newAction = _utilitySelector.CurrentAction.Name;
                if (newAction != _currentActionName)
                {
                    _currentActionName = newAction;
                    UpdateIndicatorColor();
                }
            }
            else
            {
                _currentActionName = "Selecting...";
            }
        }
        
        private void UpdateIndicatorColor()
        {
            if (_indicatorRenderer == null) return;
            
            Color color = _currentActionName switch
            {
                "Patrol" => _patrolColor,
                "Wander" => _wanderColor,
                "Idle" => _idleColor,
                _ => Color.white
            };
            
            _indicatorRenderer.material.color = color;
        }
    }
}
