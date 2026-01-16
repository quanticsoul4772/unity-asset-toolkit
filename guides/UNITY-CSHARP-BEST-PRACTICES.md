# Unity C# Best Practices Guide

A comprehensive reference for writing clean, performant C# code in Unity.

## Table of Contents
- [MonoBehaviour Lifecycle](#monobehaviour-lifecycle)
- [Coroutines](#coroutines)
- [Memory Management](#memory-management)
- [Performance Optimization](#performance-optimization)
- [Code Organization](#code-organization)
- [Common Patterns](#common-patterns)

---

## MonoBehaviour Lifecycle

Understanding the execution order is critical for avoiding bugs.

### Initialization Phase
```csharp
// Called once when script instance is loaded (before Start)
// Use for: Setting references, initializing variables
void Awake()
{
    // Cache component references here
    _rigidbody = GetComponent<Rigidbody>();
    _agent = GetComponent<NavMeshAgent>();
}

// Called once before the first Update (after all Awake calls)
// Use for: Setup that depends on other objects being initialized
void Start()
{
    // Safe to reference other objects here
    _target = FindObjectOfType<Player>().transform;
}

// Called when object becomes active
void OnEnable()
{
    // Subscribe to events here
    GameEvents.OnPlayerDeath += HandlePlayerDeath;
}

// Called when object becomes inactive
void OnDisable()
{
    // Unsubscribe from events here (prevent memory leaks!)
    GameEvents.OnPlayerDeath -= HandlePlayerDeath;
}
```

### Update Phase
```csharp
// Called every frame - use for input and non-physics logic
// Framerate dependent!
void Update()
{
    // Input handling
    if (Input.GetKeyDown(KeyCode.Space))
        Jump();
    
    // Visual updates, UI, timers
    _timer += Time.deltaTime;
}

// Called at fixed intervals (default 0.02s) - use for physics
// Consistent timing regardless of framerate
void FixedUpdate()
{
    // Physics calculations
    _rigidbody.AddForce(moveDirection * speed);
}

// Called after all Update calls - use for camera follow
void LateUpdate()
{
    // Camera positioning (after character has moved)
    transform.position = _target.position + offset;
}
```

### Execution Order Diagram
```
Awake() → OnEnable() → Start() → [FixedUpdate → Update → LateUpdate] loop
                                              ↓
                               OnDisable() → OnDestroy()
```

### Best Practices
- ✅ Cache component references in `Awake()`
- ✅ Use `Start()` for cross-object references
- ✅ Always unsubscribe events in `OnDisable()`
- ✅ Use `FixedUpdate()` for physics, `Update()` for input
- ❌ Never call `GetComponent<T>()` in `Update()`
- ❌ Don't assume execution order between different scripts

---

## Coroutines

Coroutines allow spreading work across frames without blocking.

### Basic Usage
```csharp
// Starting a coroutine
StartCoroutine(MoveToPosition(targetPos));

// Store reference to stop later
private Coroutine _moveCoroutine;
_moveCoroutine = StartCoroutine(MoveToPosition(targetPos));
StopCoroutine(_moveCoroutine);

// The coroutine method
private IEnumerator MoveToPosition(Vector3 target)
{
    while (Vector3.Distance(transform.position, target) > 0.1f)
    {
        transform.position = Vector3.MoveTowards(
            transform.position, 
            target, 
            speed * Time.deltaTime
        );
        yield return null; // Wait one frame
    }
}
```

### Yield Instructions
```csharp
// Wait one frame
yield return null;

// Wait for seconds (affected by Time.timeScale)
yield return new WaitForSeconds(2f);

// Wait for seconds (ignores Time.timeScale)
yield return new WaitForSecondsRealtime(2f);

// Wait until condition is true
yield return new WaitUntil(() => _isReady);

// Wait while condition is true
yield return new WaitWhile(() => _isLoading);

// Wait for end of frame (after rendering)
yield return new WaitForEndOfFrame();

// Wait for physics update
yield return new WaitForFixedUpdate();

// Wait for another coroutine to complete
yield return StartCoroutine(OtherCoroutine());
```

### Avoiding Garbage Collection
```csharp
// BAD - Creates garbage every iteration
private IEnumerator BadCoroutine()
{
    while (true)
    {
        yield return new WaitForSeconds(1f); // Allocates!
    }
}

// GOOD - Cache WaitForSeconds
private WaitForSeconds _waitOneSecond = new WaitForSeconds(1f);

private IEnumerator GoodCoroutine()
{
    while (true)
    {
        yield return _waitOneSecond; // No allocation!
    }
}
```

### Common Coroutine Patterns
```csharp
// Delayed action
private IEnumerator DelayedAction(float delay, System.Action action)
{
    yield return new WaitForSeconds(delay);
    action?.Invoke();
}

// Timed loop with cleanup
private IEnumerator TimedBehavior(float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        // Do work...
        elapsed += Time.deltaTime;
        yield return null;
    }
    // Cleanup when done
}

// Sequence of actions
private IEnumerator ActionSequence()
{
    yield return StartCoroutine(Phase1());
    yield return StartCoroutine(Phase2());
    yield return StartCoroutine(Phase3());
}
```

---

## Memory Management

### Object Pooling
Avoid `Instantiate()` and `Destroy()` at runtime - use pooling instead.

```csharp
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _pool = new Queue<T>();
    private readonly T _prefab;
    private readonly Transform _parent;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;
        
        for (int i = 0; i < initialSize; i++)
        {
            CreateNew();
        }
    }

    public T Get()
    {
        T obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }

    private T CreateNew()
    {
        T obj = Object.Instantiate(_prefab, _parent);
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
        return obj;
    }
}
```

### Avoiding Garbage Collection
```csharp
// BAD - String concatenation creates garbage
string status = "Health: " + health + " / " + maxHealth;

// GOOD - Use StringBuilder for repeated concatenation
private StringBuilder _sb = new StringBuilder();
_sb.Clear();
_sb.Append("Health: ").Append(health).Append(" / ").Append(maxHealth);
string status = _sb.ToString();

// BAD - LINQ creates garbage
var enemies = FindObjectsOfType<Enemy>().Where(e => e.IsAlive).ToList();

// GOOD - Reuse lists, avoid LINQ in hot paths
private List<Enemy> _enemyCache = new List<Enemy>();
void UpdateEnemyList()
{
    _enemyCache.Clear();
    foreach (var enemy in _allEnemies)
    {
        if (enemy.IsAlive)
            _enemyCache.Add(enemy);
    }
}

// BAD - Boxing value types
object boxed = 42;

// GOOD - Use generics to avoid boxing
void ProcessValue<T>(T value) { }
```

### Component Caching
```csharp
public class EnemyController : MonoBehaviour
{
    // Cache in fields - assigned in Awake()
    private Transform _transform;
    private Rigidbody _rigidbody;
    private NavMeshAgent _agent;
    private Animator _animator;

    private void Awake()
    {
        _transform = transform; // Even transform should be cached!
        _rigidbody = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Use cached references - no GetComponent calls!
        _transform.position = CalculatePosition();
    }
}
```

---

## Performance Optimization

### Update Optimization
```csharp
// BAD - Checking distance every frame for all enemies
void Update()
{
    foreach (var enemy in enemies)
    {
        float dist = Vector3.Distance(transform.position, enemy.position);
        if (dist < attackRange)
            Attack(enemy);
    }
}

// GOOD - Use sqrMagnitude (avoids square root)
void Update()
{
    float sqrRange = attackRange * attackRange;
    foreach (var enemy in enemies)
    {
        float sqrDist = (transform.position - enemy.position).sqrMagnitude;
        if (sqrDist < sqrRange)
            Attack(enemy);
    }
}

// BETTER - Stagger updates across frames
private int _updateIndex = 0;
private const int ENEMIES_PER_FRAME = 10;

void Update()
{
    float sqrRange = attackRange * attackRange;
    int count = Mathf.Min(ENEMIES_PER_FRAME, enemies.Count - _updateIndex);
    
    for (int i = 0; i < count; i++)
    {
        var enemy = enemies[_updateIndex + i];
        // Process enemy...
    }
    
    _updateIndex = (_updateIndex + count) % enemies.Count;
}
```

### Physics Optimization
```csharp
// Use layers to limit collision checks
// In Project Settings > Physics, disable unnecessary layer collisions

// Use NonAlloc methods to avoid garbage
private Collider[] _hitBuffer = new Collider[32];

void CheckNearby()
{
    int count = Physics.OverlapSphereNonAlloc(
        transform.position, 
        radius, 
        _hitBuffer,
        layerMask
    );
    
    for (int i = 0; i < count; i++)
    {
        ProcessCollider(_hitBuffer[i]);
    }
}

// Raycast NonAlloc version
private RaycastHit[] _rayHits = new RaycastHit[10];

void CastRay()
{
    int count = Physics.RaycastNonAlloc(origin, direction, _rayHits, maxDistance);
    // Process hits...
}
```

### Spatial Partitioning
```csharp
// Simple grid-based spatial hash for fast neighbor lookups
public class SpatialHash<T> where T : class
{
    private readonly Dictionary<int, List<T>> _cells = new Dictionary<int, List<T>>();
    private readonly float _cellSize;

    public SpatialHash(float cellSize)
    {
        _cellSize = cellSize;
    }

    public void Insert(T item, Vector3 position)
    {
        int hash = GetHash(position);
        if (!_cells.TryGetValue(hash, out var list))
        {
            list = new List<T>();
            _cells[hash] = list;
        }
        list.Add(item);
    }

    public void GetNearby(Vector3 position, List<T> results)
    {
        results.Clear();
        // Check surrounding cells
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector3 offset = new Vector3(x * _cellSize, 0, z * _cellSize);
                int hash = GetHash(position + offset);
                if (_cells.TryGetValue(hash, out var list))
                {
                    results.AddRange(list);
                }
            }
        }
    }

    private int GetHash(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / _cellSize);
        int z = Mathf.FloorToInt(position.z / _cellSize);
        return x * 73856093 ^ z * 19349663; // Hash combining
    }
}
```

---

## Code Organization

### Folder Structure
```
Assets/
├── Scripts/
│   ├── Core/           # Managers, game state
│   ├── AI/             # AI behaviors, pathfinding
│   ├── Player/         # Player controllers
│   ├── UI/             # UI controllers
│   ├── Utils/          # Helper classes
│   └── Editor/         # Editor scripts (special folder!)
├── Prefabs/
├── Scenes/
├── Materials/
├── Textures/
└── Audio/
```

### Naming Conventions
```csharp
// Classes: PascalCase
public class EnemyController { }
public class PathfindingManager { }

// Interfaces: IPascalCase
public interface IPoolable { }
public interface IDamageable { }

// Methods: PascalCase
public void CalculatePath() { }
private void UpdateState() { }

// Properties: PascalCase
public float Speed { get; set; }
public bool IsAlive { get; private set; }

// Fields: _camelCase for private, camelCase for public
private float _moveSpeed;
private Transform _target;
public float moveSpeed; // Prefer properties over public fields!

// Constants: PascalCase or UPPER_CASE
private const float MaxSpeed = 10f;
private const int MAX_ENEMIES = 100;

// Local variables: camelCase
float distance = Vector3.Distance(a, b);
var nearestEnemy = FindNearest();
```

### Script Template
```csharp
using UnityEngine;

namespace YourNamespace
{
    /// <summary>
    /// Brief description of what this class does.
    /// </summary>
    public class YourClassName : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Settings")]
        [SerializeField] private float _speed = 5f;
        [SerializeField] private Transform _target;
        
        [Header("References")]
        [SerializeField] private GameObject _prefab;
        #endregion

        #region Private Fields
        private Rigidbody _rigidbody;
        private bool _isInitialized;
        #endregion

        #region Properties
        public float Speed => _speed;
        public bool IsReady => _isInitialized;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized) return;
            UpdateBehavior();
        }
        #endregion

        #region Public Methods
        public void SetTarget(Transform target)
        {
            _target = target;
        }
        #endregion

        #region Private Methods
        private void CacheComponents()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Initialize()
        {
            _isInitialized = true;
        }

        private void UpdateBehavior()
        {
            // Main logic here
        }
        #endregion
    }
}
```

---

## Common Patterns

### Singleton (Use Sparingly!)
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

### Event System
```csharp
// Define events in a static class
public static class GameEvents
{
    public static event System.Action OnGameStart;
    public static event System.Action OnGameEnd;
    public static event System.Action<int> OnScoreChanged;
    public static event System.Action<Enemy> OnEnemyKilled;

    public static void TriggerGameStart() => OnGameStart?.Invoke();
    public static void TriggerGameEnd() => OnGameEnd?.Invoke();
    public static void TriggerScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public static void TriggerEnemyKilled(Enemy enemy) => OnEnemyKilled?.Invoke(enemy);
}

// Subscribe in components
public class ScoreDisplay : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.OnScoreChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int score)
    {
        // Update UI
    }
}
```

### Interface-Based Design
```csharp
public interface IDamageable
{
    void TakeDamage(float amount);
    bool IsAlive { get; }
}

public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

public class Enemy : MonoBehaviour, IDamageable, IPoolable
{
    public bool IsAlive => _health > 0;
    private float _health;

    public void TakeDamage(float amount)
    {
        _health -= amount;
        if (_health <= 0)
            Die();
    }

    public void OnSpawn()
    {
        _health = 100f;
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
    }
}
```

---

## Quick Reference Card

| Do | Don't |
|----|-------|
| Cache components in `Awake()` | Call `GetComponent` in `Update()` |
| Use `sqrMagnitude` for distance | Use `Vector3.Distance` in hot paths |
| Pool frequently spawned objects | `Instantiate`/`Destroy` at runtime |
| Unsubscribe events in `OnDisable` | Leave event subscriptions dangling |
| Use `NonAlloc` physics methods | Create arrays in physics checks |
| Cache `WaitForSeconds` | Create new yield instructions each time |
| Use layers for physics filtering | Check all colliders unnecessarily |
| Spread expensive work across frames | Do heavy work in single `Update` |
