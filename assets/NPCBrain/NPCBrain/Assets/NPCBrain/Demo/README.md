# NPCBrain Demo Scenes

## Week 3: Utility AI + Criticality Validation

### TestScene

The TestScene demonstrates the core Week 3 functionality:

1. **Utility AI Action Selection** - NPCs choose between Patrol, Wander, and Idle based on utility scores
2. **Criticality Temperature** - Affects the randomness of action selection via softmax
3. **Entropy Tracking** - System monitors action variety and adjusts temperature automatically
4. **Behavior Variation** - Watch NPCs naturally vary their behavior over time

### Creating the Test Scene

1. In Unity, go to **NPCBrain → Create Test Scene**
2. Press **Play** to start the simulation
3. Observe the debug panel showing each NPC's:
   - Current action
   - Temperature (randomness level)
   - Entropy (action variety measurement)
   - Inertia (tendency to stick with current action)

### What to Look For

- **Low Entropy**: When an NPC repeatedly chooses the same action, entropy drops and temperature increases
- **Temperature Rise**: Higher temperature makes the NPC more likely to try different actions
- **Natural Variation**: Over time, the system self-balances between exploitation and exploration

### Color Coding

- 🔵 Blue sphere = Patrol action
- 🟡 Yellow sphere = Wander action  
- ⚪ Gray sphere = Idle action

## Running Integration Tests

```
Window → General → Test Runner → PlayMode → Run All
```

Key test files:
- `BehaviorTreeIntegrationTests.cs` - Full BT execution tests
- `PerceptionIntegrationTests.cs` - SightSensor detection tests
- `UtilityCriticalityIntegrationTests.cs` - Week 3 specific tests
