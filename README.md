# Third-person shooter game with intelligent opponents
Third-person shooter game with intelligent soldiers

## Finite State Machine
The core AI behavior is implemented using a Finite State Machine approach. The opponent's behavior is divided into various states, each representing a different gameplay scenario such as "Idle", "Chase", "Attack" and more.

### Implementation 

The State class is the base class for representing states. Each state inherits from this class and receives a reference to an AgentController script.

```csharp
public class State
{
    public readonly AgentController agent;

    public State(AgentController agent) {
        this.agent = agent;
    }

    public virtual void Enter() {} // Method is run only once, when this state begins

    public virtual void Tick() {} // Method is being run every frame. It's used for executing logic.

    public virtual void Exit() {} // Method is run only once, when this state ends
}
```

Example for PatrolState:

```csharp
public class PatrolState : State
{
    public PatrolState(AgentController agent) : base(agent) {}

    public override void Enter() {
        // Initialize variables
    }

    public override Tick() {
        // Logic
        if (targetVisible) {
            agent.ChangeState(agent.ChaseTargetState);
        }
    }

    public override Exit() {
        // Reset variables
    }
}
```

Changing states:

```csharp
public void ChangeState(State nextState)
{
    currentState.Exit();
    currentState = newState;
    currentState.Enter();
}
```

## Agent Controller

AgentController is a script on agent's GameObject, which is used for handling agent's states and animations.

```csharp
public class AgentController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;

    public State idleState;
    public State patrolState;
    ...

    public void Awake() {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    // Run before first frame
    public void Start()
    {
        InitializeStates();
    }

    // Run every frame
    public void Update()
    {

    }

    private void InitializeStates()
    {
        idleState = new IdleState(this);
        patrolState = new PatrolState(this);
        ...
    }
}
```
