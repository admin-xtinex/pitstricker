# Pit Striker — C# Coding Standards & Best Practices

## 1. Principles
* **Single Responsibility Principle (SRP):** Each script must perform one distinct duty.
* **Zero Allocation in Per-Frame Loops:** Never allocate memory (`new`, string concatenation, LINQ) inside `Update()` or `FixedUpdate()`.
* **Explicit Serialization:** Avoid `public` fields for Inspector exposure; use `[SerializeField] private`.

## 2. Naming Conventions Matrix

| Element | Format | Example |
| :--- | :--- | :--- |
| Classes & Structs | PascalCase | `MarbleController`, `TurnState` |
| Interfaces | IPascalCase | `IStrikable`, `ITurnListener` |
| Methods | PascalCase (Verb-first) | `LaunchMarble()`, `ResetMatch()` |
| Properties | PascalCase | `public bool IsMoving { get; private set; }` |
| Serialized Fields | `_camelCase` with attribute | `[SerializeField] private float _strikeForce;` |
| Private Fields | `_camelCase` | `private float _currentSpeed;` |
| Constants | UPPER_SNAKE_CASE | `private const float STOP_THRESHOLD = 0.05f;` |
| Events & Delegates | PascalCase (Prefix `On`) | `public event Action<int> OnTurnChanged;` |

## 3. Unity Lifecycle Rules
* **Physics in `FixedUpdate`:** All rigidbody force additions and velocity checks must reside in `FixedUpdate()`.
* **Input in `Update`:** Screen touch capture and drag calculations occur in `Update()`.
* **Component Caching:** Always cache component references in `Awake()`. Never call `GetComponent()` in `Update()`.
* **Event Unsubscription:** Always unsubscribe from events in `OnDisable()` or `OnDestroy()` to prevent memory leaks:
  ```csharp
  private void OnEnable() => TurnManager.OnTurnChanged += HandleTurnChanged;
  private void OnDisable() => TurnManager.OnTurnChanged -= HandleTurnChanged;
  ```
