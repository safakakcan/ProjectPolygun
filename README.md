# ProjectPolygun - FPS Game Architecture

A well-architected Unity FPS game built with SOLID principles, composition over inheritance, and optimized for 16-player
networked gameplay using Mirror.

## 🏗️ Architecture Overview

This project follows a **layered architecture** with clear separation of concerns:

- **Core Layer**: Interfaces, events, and system contracts
- **Infrastructure Layer**: Cross-cutting concerns (DI, pooling, bootstrapping)
- **Gameplay Layer**: Game-specific logic (players, weapons, health)
- **Networking Layer**: Mirror integration for multiplayer
- **UI Layer**: User interface and presentation

## 🔧 Core Systems

### 1. Event Bus System

Provides decoupled communication between systems using the publisher-subscriber pattern.

```csharp
// Subscribe to events
ServiceLocator.EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);

// Publish events
var deathEvent = new PlayerDeathEvent(playerId, killerId, weaponName, position);
ServiceLocator.EventBus.Publish(deathEvent);

// Always unsubscribe to prevent memory leaks
ServiceLocator.EventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
```

### 2. Dependency Injection

Simple IoC container for loose coupling between systems.

```csharp
// Register services
ServiceLocator.Container.Register<IWeaponSystem, WeaponSystem>();
ServiceLocator.Container.RegisterInstance<IHealthSystem>(healthSystem);

// Resolve services
var weaponSystem = ServiceLocator.Resolve<IWeaponSystem>();
```

### 3. Object Pooling

Generic pooling system for performance optimization.

```csharp
var bulletPool = new ObjectPool<GameObject>(
    createFunc: () => Instantiate(bulletPrefab),
    onGet: bullet => bullet.SetActive(true),
    onReturn: bullet => bullet.SetActive(false)
);

bulletPool.Prewarm(50); // Pre-create 50 bullets
```

## 🎮 Game Architecture

### Player System ✅ **IMPLEMENTED**

- **PlayerController**: Main player class using composition over inheritance
- **IHealthSystem + HealthSystem**: Complete health and damage management with events
- **PlayerInputHandler**: FPS input handling (WASD, mouse, jump, crouch, sprint)
- **FPSMovementSystem**: Full FPS movement with physics, jumping, crouching
- **PlayerManager**: Manages all players, spawning, respawning, and tracking (16 max)
- **Composition**: Players are composed of multiple specialized systems

### Weapon System 🔄 **INTERFACES READY**

- **IWeapon**: Base weapon interface (ready for implementation)
- **Polymorphism**: Different weapon types (hitscan, projectile) implement same interface
- **Strategy Pattern**: Weapon behavior can be swapped at runtime

### Event-Driven Design ✅ **IMPLEMENTED**

Complete event system for all major game interactions:

- **Player Events**: `PlayerJoinedEvent`, `PlayerLeftEvent`, `PlayerDeathEvent`, `PlayerRespawnEvent`
- **Combat Events**: `PlayerDamagedEvent`, `PlayerHealedEvent`, `WeaponFiredEvent`
- **Movement Events**: `PlayerJumpedEvent`, `PlayerCrouchStartedEvent`, `PlayerMovementStartedEvent`
- **State Events**: `GameStateChangedEvent`, `PlayerInvulnerabilityStartedEvent`

## 🚀 Getting Started

### 1. Initialize Core Systems

The `GameBootstrapper` automatically initializes all core systems:

```csharp
// Automatic initialization (default)
// GameBootstrapper initializes on Awake()

// Manual initialization
GameBootstrapper.Instance.Initialize();
```

### 2. Use Service Locator

Access services throughout your code:

```csharp
// Get event bus
var eventBus = ServiceLocator.EventBus;

// Resolve custom services
var playerManager = ServiceLocator.Resolve<IPlayerManager>();
```

### 3. Follow SOLID Principles

#### Single Responsibility

```csharp
// ✅ Good - Each class has one responsibility
public class WeaponController : IWeaponController
public class HealthSystem : IHealthSystem
public class MovementSystem : IMovementSystem

// ❌ Bad - God class with multiple responsibilities
public class Player : NetworkBehaviour // handles movement, health, weapons, input...
```

#### Open/Closed Principle

```csharp
// ✅ Good - Open for extension via interfaces
public interface IWeapon
{
    bool TryFire(Vector3 origin, Vector3 direction);
}

public class AssaultRifle : IWeapon { /* implementation */ }
public class Shotgun : IWeapon { /* implementation */ }
```

#### Dependency Inversion

```csharp
// ✅ Good - Depend on abstractions
public class PlayerController
{
    private readonly IHealthSystem _healthSystem;
    private readonly IWeaponSystem _weaponSystem;
    
    public PlayerController(IHealthSystem health, IWeaponSystem weapons)
    {
        _healthSystem = health;
        _weaponSystem = weapons;
    }
}
```

## 📁 Project Structure

```
Assets/Scripts/
├── Core/                    # Core systems & interfaces
│   ├── Interfaces/         # System contracts
│   ├── Events/            # Game events
│   └── Systems/           # Core implementations
├── Gameplay/              # Game-specific logic
│   ├── Player/           # Player systems
│   ├── Weapons/          # Weapon systems
│   ├── GameModes/        # Game mode logic
│   └── Utilities/        # Helper classes
├── Networking/            # Mirror networking
│   ├── Client/           # Client-side logic
│   ├── Server/           # Server-side logic
│   └── Shared/           # Shared networking code
├── UI/                    # User interface
├── Infrastructure/        # Cross-cutting concerns
└── Examples/             # Usage examples
```

## 🔄 Development Workflow

### 1. Define Interface First

```csharp
public interface INewFeature
{
    void DoSomething();
}
```

### 2. Implement Interface

```csharp
public class NewFeature : MonoBehaviour, INewFeature
{
    public void DoSomething()
    {
        // Implementation
    }
}
```

### 3. Register Service

```csharp
ServiceLocator.Container.Register<INewFeature, NewFeature>();
```

### 4. Use Events for Communication

```csharp
public class NewFeatureEvent : GameEventBase
{
    // Event properties
}

// Publish event
ServiceLocator.EventBus.Publish(new NewFeatureEvent());
```

## 🎯 Best Practices

1. **Always use interfaces** for system dependencies
2. **Subscribe/Unsubscribe** properly to prevent memory leaks
3. **Keep classes small** and focused on single responsibility
4. **Use composition** over inheritance for player/entity systems
5. **Leverage object pooling** for frequently created/destroyed objects
6. **Follow the event-driven pattern** for decoupled communication

## 🧪 Testing

### Quick Start Testing
Use the `SceneInitializer` for instant setup:
1. Add `SceneInitializer` component to any GameObject in your scene
2. Right-click and select "Quick FPS Setup" 
3. Play the scene and you'll have a working FPS player!

### Architecture Testing
Run the `ArchitectureExample` to see core systems:
1. Add `ArchitectureExample` component to any GameObject
2. Play the scene
3. Check console for event system demonstration

### Player System Testing  
Run the `PlayerSystemExample` to test gameplay:
1. Add `PlayerSystemExample` component to any GameObject
2. Play the scene to spawn players automatically
3. Use context menu options to test damage, healing, respawning
4. WASD to move, mouse to look, Space to jump, Ctrl to crouch, Shift to sprint

## 📈 Performance Targets

- **60 FPS** on mid-range hardware
- **<100ms** server response time
- **<50MB** memory usage for 16 players
- **<1MB/min** network traffic per player

## 🔜 Next Steps

1. **Install Mirror** networking package ⏳
2. ~~**Implement player movement** with client prediction~~ ✅ **COMPLETED**
3. ~~**Add health/damage** mechanics~~ ✅ **COMPLETED**
4. **Create weapon systems** with different types ⏳
5. **Build UI systems** for HUD and menus ⏳
6. **Add networking integration** for multiplayer ⏳

## 🤝 Team Development

With a 3-person team:

- **Person 1**: Core systems & networking
- **Person 2**: Gameplay mechanics (movement, weapons, health)
- **Person 3**: UI, polish & integration

This architecture supports parallel development with minimal conflicts. 