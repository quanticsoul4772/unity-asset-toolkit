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
        private bool _navAgentCached;
        
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
        
        private NodeStatus MoveDirectly(Transform transform, Vector3 target, string debugName = "")
        {
            Vector3 direction = (target - transform.position).normalized;
            Vector3 movement = direction * _moveSpeed * Time.deltaTime;
            
            transform.position += movement;
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            return NodeStatus.Running;
        }
    }
}
