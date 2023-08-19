# Third-Person Shooter Game with Intelligent Opponents
Third-person shooter game with intelligent soldiers

## Table of Contents

- [Finite State Machine](#finite-state-machine)
    - [Implementation](#finite-state-machine-implementation)
- [Agent Controller](#agent-controller)

<a name="finite-state-machine"></a>
## Finite State Machine
The core AI behavior is implemented using a Finite State Machine approach. The opponent's behavior is divided into various states, each representing a different gameplay scenario such as "Idle", "Chase", "Attack" and more.

<a name="finite-state-machine-implementation"></a>
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

<a name="agent-controller"></a>
## Agent Controller

AgentController is a script on agent's GameObject, which is used for handling agent's states and animations.

```csharp
public class AgentController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private State currentState;

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
        currentState = idleState;
    }

    // Run every frame
    public void Update()
    {
        currentState.Tick();
    }

    private void InitializeStates()
    {
        idleState = new IdleState(this);
        patrolState = new PatrolState(this);
        ...
    }

    public void ChangeState(State nextState)
    {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
```

## Path finding and movement (NavMesh & NavMeshAgent)

The Navigation Mesh, or NavMesh, is a data structure that represents walkable surfaces within the game world. It acts as a blueprint for the AI characters to navigate the environment seamlessly. With NavMesh, the opponents can find optimal paths to reach their destinations, whether it's chasing the player, taking cover, or maneuvering around obstacles.

### Generating a NavMesh

1. Mark all static objects in scene as _Static_.
2. Select all objects that should affect the navigation - walkable surfaces and obstacles.
3. Generate a NavMesh clicking _Bake_ button (open _Window > AI > Navigation_)

## Section 2
Content for Section 2.
Generated NavMesh should look something like this. Blue color represents walkable areas for agents.

![Generated NavMesh](screenshots/generated_nav_mesh.png?raw=true)

