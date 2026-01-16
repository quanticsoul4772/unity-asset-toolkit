using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Enumeration of possible agent states.
    /// </summary>
    public enum AgentStateType
    {
        /// <summary>Agent is idle, not performing any action.</summary>
        Idle,
        /// <summary>Agent is moving toward a destination.</summary>
        Moving,
        /// <summary>Agent is seeking a specific target.</summary>
        Seeking,
        /// <summary>Agent is fleeing from a threat.</summary>
        Fleeing,
        /// <summary>Agent is gathering resources.</summary>
        Gathering,
        /// <summary>Agent is attacking a target.</summary>
        Attacking,
        /// <summary>Agent is returning to base/home.</summary>
        Returning,
        /// <summary>Agent is following a leader or formation.</summary>
        Following,
        /// <summary>Agent is patrolling waypoints.</summary>
        Patrolling,
        /// <summary>Agent is dead/inactive.</summary>
        Dead
    }
    
    /// <summary>
    /// Abstract base class for agent states in the finite state machine.
    /// Derive from this class to create custom states.
    /// </summary>
    public abstract class AgentState
    {
        /// <summary>
        /// The type of this state.
        /// </summary>
        public AgentStateType Type { get; protected set; }
        
        /// <summary>
        /// The agent this state belongs to.
        /// </summary>
        public SwarmAgent Agent { get; private set; }
        
        /// <summary>
        /// Time when this state was entered.
        /// </summary>
        public float EnterTime { get; private set; }
        
        /// <summary>
        /// How long this state has been active.
        /// </summary>
        public float Duration => Time.time - EnterTime;
        
        /// <summary>
        /// Initialize the state with its agent.
        /// Called by SwarmAgent when setting a new state.
        /// </summary>
        internal void Initialize(SwarmAgent agent)
        {
            Agent = agent;
        }
        
        /// <summary>
        /// Called when entering this state.
        /// Override to perform setup logic.
        /// </summary>
        public virtual void Enter()
        {
            EnterTime = Time.time;
        }
        
        /// <summary>
        /// Called every frame while in this state.
        /// Override to perform update logic.
        /// </summary>
        public virtual void Execute()
        {
        }
        
        /// <summary>
        /// Called every fixed update while in this state.
        /// Override for physics-related logic.
        /// </summary>
        public virtual void FixedExecute()
        {
        }
        
        /// <summary>
        /// Called when exiting this state.
        /// Override to perform cleanup logic.
        /// </summary>
        public virtual void Exit()
        {
        }
        
        /// <summary>
        /// Check if this state should transition to another state.
        /// Override to implement transition logic.
        /// </summary>
        /// <returns>The new state to transition to, or this state to stay.</returns>
        public virtual AgentState CheckTransitions()
        {
            return this;
        }
        
        /// <summary>
        /// Handle a message received by the agent.
        /// Override to respond to specific message types.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <returns>True if the message was handled.</returns>
        public virtual bool HandleMessage(SwarmMessage message)
        {
            return false;
        }
    }
}
