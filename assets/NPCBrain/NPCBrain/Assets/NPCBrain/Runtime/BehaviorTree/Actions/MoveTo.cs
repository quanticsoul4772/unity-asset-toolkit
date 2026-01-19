using System;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBrain.BehaviorTree.Actions
{
    public class MoveTo : BTNode
    {
        private readonly Func<Vector3> _targetGetter;
        private readonly float _arrivalDistanceSqr;
        private readonly float _moveSpeed;
        private readonly float _timeout;
        
        private float _startTime;
        private NavMeshAgent _cachedNavAgent;
        private CharacterController _cachedCharController;
        private bool _navAgentCached;
        private bool _charControllerCached;
        
        public MoveTo(Func<Vector3> targetGetter, float arrivalDistance, float moveSpeed, float timeout)
        {
            _targetGetter = targetGetter ?? throw new ArgumentNullException(nameof(targetGetter));
            _arrivalDistanceSqr = arrivalDistance * arrivalDistance;
            _moveSpeed = moveSpeed;
            _timeout = timeout;
            Name = "MoveTo";
        }
        
        public MoveTo(Func<Vector3> targetGetter, float arrivalDistance, float moveSpeed)
            : this(targetGetter, arrivalDistance, moveSpeed, 30f)
        {
        }
        
        public MoveTo(Func<Vector3> targetGetter, float arrivalDistance)
            : this(targetGetter, arrivalDistance, 5f, 30f)
        {
        }
        
        public MoveTo(Func<Vector3> targetGetter)
            : this(targetGetter, 0.5f, 5f, 30f)
        {
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            _startTime = Time.time;
            
            if (!_navAgentCached)
            {
                _cachedNavAgent = brain.GetComponent<NavMeshAgent>();
                _navAgentCached = true;
            }
            
            if (!_charControllerCached)
            {
                _cachedCharController = brain.GetComponent<CharacterController>();
                _charControllerCached = true;
            }
        }
        
        protected override void OnExit(NPCBrainController brain)
        {
            if (_cachedNavAgent != null && _cachedNavAgent.isOnNavMesh)
            {
                _cachedNavAgent.ResetPath();
            }
        }
        
        public override void Reset()
        {
            base.Reset();
            _navAgentCached = false;
            _cachedNavAgent = null;
            _charControllerCached = false;
            _cachedCharController = null;
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            Vector3 target = _targetGetter();
            Vector3 currentPos = brain.transform.position;
            float distanceSqr = (currentPos - target).sqrMagnitude;
            
            if (distanceSqr <= _arrivalDistanceSqr)
            {
                return NodeStatus.Success;
            }
            
            if (Time.time - _startTime > _timeout)
            {
                return NodeStatus.Failure;
            }
            
            if (_cachedNavAgent != null && _cachedNavAgent.isOnNavMesh)
            {
                return MoveViaNavMesh(_cachedNavAgent, target);
            }
            
            // Use CharacterController if available (handles collision detection)
            if (_cachedCharController != null)
            {
                return MoveViaCharacterController(_cachedCharController, brain.transform, target);
            }
            
            return MoveDirectly(brain.transform, target, brain.name);
        }
        
        private NodeStatus MoveViaNavMesh(NavMeshAgent agent, Vector3 target)
        {
            agent.SetDestination(target);
            
            if (agent.pathPending)
            {
                return NodeStatus.Running;
            }
            
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return NodeStatus.Failure;
            }
            
            if (agent.remainingDistance * agent.remainingDistance <= _arrivalDistanceSqr && !agent.pathPending)
            {
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
        
        /// <summary>
        /// Moves the NPC using CharacterController.Move() which respects collisions with walls and obstacles.
        /// </summary>
        private NodeStatus MoveViaCharacterController(CharacterController controller, Transform transform, Vector3 target)
        {
            Vector3 currentPos = transform.position;
            Vector3 toTarget = target - currentPos;
            toTarget.y = 0f; // Keep movement horizontal
            float distance = toTarget.magnitude;

            if (distance < 0.01f)
            {
                return NodeStatus.Running;
            }

            Vector3 direction = toTarget / distance;
            Vector3 movement = direction * _moveSpeed * Time.deltaTime;
            
            // Add gravity to keep grounded
            if (!controller.isGrounded)
            {
                movement.y = -9.81f * Time.deltaTime;
            }

            controller.Move(movement);

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            return NodeStatus.Running;
        }
        
        /// <summary>
        /// Moves the given transform a single step toward the target position and orients it to face the movement direction.
        /// WARNING: This method does NOT respect collisions - NPCs will walk through walls!
        /// Use CharacterController or NavMeshAgent for proper collision handling.
        /// </summary>
        /// <param name="transform">The transform to move and rotate.</param>
        /// <param name="target">The destination position to approach.</param>
        /// <param name="debugName">Optional label used for debugging or tracing.</param>
        /// <returns>`NodeStatus.Running` while the node drives the transform toward the target.</returns>
        private NodeStatus MoveDirectly(Transform transform, Vector3 target, string debugName = "")
        {
            // Performance: Cache position to reduce property access overhead
            Vector3 currentPos = transform.position;
            Vector3 toTarget = target - currentPos;
            float distance = toTarget.magnitude;

            // Avoid division by zero and normalize efficiently
            Vector3 direction = distance > 0.0001f ? toTarget / distance : Vector3.zero;
            Vector3 movement = direction * _moveSpeed * Time.deltaTime;

            transform.position = currentPos + movement;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            return NodeStatus.Running;
        }
    }
}