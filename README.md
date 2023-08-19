# Intelligent Opponents in Unity using Finit State Machine

## Table of Contents

- [Finite State Machine](#finite-state-machine)
    - [Implementation](#finite-state-machine-implementation)
- [Agent Controller](#agent-controller)
- [Navigation and Movement](#navigation-and-movement)

<a name="finite-state-machine"></a>
## Finite State Machine
The core AI behavior is implemented using a Finite State Machine approach. The approach is to divide the behaviour of an agent into several different states. For example: patrolling state, chasing target state and attacking state. Between these states we define transitions or conditions and actions that the agent will perform in a given state.

 <img src="screenshots/fsm_example.png?raw=true" alt="FSM Example" height="350">

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

<a name="navigation-and-movement"></a>
## Navigation and Movement (NavMesh & NavMeshAgent)

The Navigation Mesh, or NavMesh, is a data structure that represents walkable surfaces within the game world. It acts as a blueprint for the AI characters to navigate the environment seamlessly. With NavMesh, the opponents can find optimal paths to reach their destinations, whether it's chasing the player, taking cover, or maneuvering around obstacles.

<a name="generating-nav-mesh"></a>
### Generating a NavMesh

1. Mark all static objects in scene as _Static_.
2. Select all objects that should affect the navigation - walkable surfaces and obstacles.
3. Generate a NavMesh clicking _Bake_ button (open _Window > AI > Navigation_)

Generated NavMesh should look something like this. Blue color represents walkable areas for agents.

<img src="screenshots/generated_nav_mesh.png?raw=true" alt="Generated NavMesh" height="400">

<a name="generating-nav-mesh"></a>
### Adding a NavMeshAgent

NavMeshAgent is used for moving your object and navigating it on the NavMesh.

Here's how you can add and configure a NavMeshAgent for it:

1. **Select the GameObject**: Click on the AI opponent GameObject in the Unity Scene Hierarchy that you want to enable navigation for.
2. **Add NavMeshAgent Component**: In the Inspector window, click on the "Add Component" button. Search for "NavMeshAgent" and select it from the list to add the NavMeshAgent component to the GameObject.
3. **Configuring NavMeshAgent**:
    - _Speed_: Adjust the "Speed" parameter to set the movement speed of the AI opponent. This determines how fast the agent moves along the NavMesh.
    - _Stopping_ Distance: Set the "Stopping Distance" parameter to determine how close the AI opponent gets to its destination before stopping. This prevents the agent from overshooting the target.
    - _Acceleration_: You can adjust the "Acceleration" parameter to control how quickly the AI opponent accelerates and decelerates while moving.

![NavMeshAgent](screenshots/nav_mesh_agent.png?raw=true)

