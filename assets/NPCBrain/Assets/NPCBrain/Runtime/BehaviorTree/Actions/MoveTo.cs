using System;
using UnityEngine;
using UnityEngine.AI;

namespace NPCBrain.BehaviorTree.Actions
{
    public class MoveTo : BTNode
    {
        private readonly Func<Vector3> _targetGetter;
        private readonly float _arrivalDistance;
        private readonly float _moveSpeed;
        private readonly float _timeout;
        
        private float _startTime;
        private bool _isMoving;
        
        public MoveTo(Func<Vector3> targetGetter, float arrivalDistance = 0.5f, float moveSpeed = 5f, float timeout = 30f)
        {
            _targetGetter = targetGetter;
            _arrivalDistance = arrivalDistance;
            _moveSpeed = moveSpeed;
            _timeout = timeout;
        }
        
        public override void OnEnter(NPCBrainController brain)
        {
            _startTime = Time.time;
            _isMoving = true;
        }
        
        public override void OnExit(NPCBrainController brain)
        {
            _isMoving = false;
        }
        
        public override NodeStatus Tick(NPCBrainController brain)
        {
            if (!_isMoving)
            {
                OnEnter(brain);
            }
            
            Vector3 target = _targetGetter();
            Vector3 currentPos = brain.transform.position;
            float distance = Vector3.Distance(currentPos, target);
            
            if (distance <= _arrivalDistance)
            {
                _isMoving = false;
                return NodeStatus.Success;
            }
            
            if (Time.time - _startTime > _timeout)
            {
                _isMoving = false;
                return NodeStatus.Failure;
            }
            
            NavMeshAgent navAgent = brain.GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                return MoveViaNavMesh(navAgent, target);
            }
            
            return MoveDirectly(brain.transform, target);
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
            
            if (agent.remainingDistance <= _arrivalDistance && !agent.pathPending)
            {
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
        
        private NodeStatus MoveDirectly(Transform transform, Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            transform.position += direction * _moveSpeed * Time.deltaTime;
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            return NodeStatus.Running;
        }
    }
}
