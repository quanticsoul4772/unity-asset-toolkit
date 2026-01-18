# NPCBrain Components

This folder contains reusable game components for the Cops and Robbers demo and similar scenarios.

## Components

### LootPoint
A stealable objective that can be picked up by RobberNPCs. Automatically emits an alarm sound when stolen.

```csharp
// Create a loot point
var loot = LootPoint.Create(position, value: 500, parent: transform);
loot.OnStolen += (lootPoint, thief) => Debug.Log("Loot stolen!");
```

### EscapeZone
A zone where robbers can escape with stolen loot. Tracks escaped robbers and their total value.

```csharp
// Create an escape zone
var zone = EscapeZone.Create(position, radius: 5f, parent: transform);
zone.OnRobberEscaped += (robber, lootValue) => Debug.Log($"Escaped with ${lootValue}!");
```

### CoverPoint
A hiding spot where robbers can take cover from cops. Provides concealment from line-of-sight detection.

```csharp
// Create a cover point
var cover = CoverPoint.Create(position, parent: transform);
if (cover.CanHide(robberGameObject))
{
    cover.TryHide(robberGameObject);
}
```

## Usage

These components are used by the `CopsAndRobbersDemoSetup` to create an interactive heist scenario.
See the Demo folder for complete examples.
