<h1 align="center">Intelligent Opponents in Unity using Finite State Machine</h1>

<br />
<p align="center">
    Third-Person Shooter Game With Intelligent Opponents
    <br />
    <br />
    <a href="https://www.youtube.com/watch?v=eTh8MVgpejQ">Gameplay Video</a>
    ·
    <a href="https://dk.um.si/IzpisGradiva.php?id=84780&lang=slv">Bachelor's Thesis</a>
</p>

## Table of Contents

- [About](#about)
- [Finite State Machine](#finite-state-machine)
    - [Base State](#fsm-base-state)
    - [Patrol State](#fsm-patrol-state)
    - [Chase Target State](#fsm-chase-state)
    - [Investigation State](#fsm-investigation-state)
    - [Attack State](#fsm-attack-state)
- [Agent Controller](#agent-controller)
- [Navigation and Movement](#navigation-and-movement)
    - [Generating NavMesh](#nav-mesh)
    - [NavMeshAgent](#nav-mesh-agent)
    - [Movement](#movement)
- [Player Detection](#player-detection)
    - [Implementation](#player-detection-implementation)
- [Sound Detection](#sound-detection)
    - [Implementation](#sound-detection-implementation)

<a name="about"></a>
## About
For my bachelor's thesis, I developed intelligent opponents within a third-person shooter game. This was achieved by utilizing Finite State Machine (FSM) approach. The FSM approach allowed for the dynamic control of the opponents' behaviors, enabling them to adapt and respond intelligently to different in-game situations. In this document I will explain how to achieve intelligent behaviour with the help of FSM and Unity engine.

<br>
<p align="center">
    <img src="screenshots/thumbnail.png?raw=true" alt="FSM Example">
</p>

<hr>

<a name="finite-state-machine"></a>
## Finite State Machine
The core AI behavior is implemented using a Finite State Machine approach. The approach is to divide the behaviour of an agent into several different states. For example: patrolling state, chasing target state and attacking state. Between these states we define transitions or conditions and actions that the agent will perform in a given state.

<br>
<p align="center">
    <img src="screenshots/fsm_example.png?raw=true" alt="FSM Example" height="350">
</p>

<a name="fsm-base-state"></a>
### Base State 
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

### Changing Current State
In AgentController script we store _currentState_. We developed a method _ChangeState_, which updates current state. First it executes _Exit()_ method from current State, than it changes the old state with new one and finally it executes _Enter()_ method from new state.

```csharp
public void ChangeState(State nextState)
{
    currentState.Exit();
    currentState = newState;
    currentState.Enter();
}
```

<a name="fsm-patrol-state"></a>
### Patrol State:
In the patrol state, the agent walks between the points that we predefine. At each point, the agent jumps for 2 seconds.

```csharp
public class PatrolState : State
{
    private int currentWaypointIndex;
    private float timer;
    private bool isReversedPatrol;

    public PatrolState(AgentController agent) : base(agent) {}

    public override void Enter() {
        agent.navMeshAgent.enabled = true;
        agent.navMeshAgent.speed = 3f;
        currentWaypointIndex = 0;
        timer = 0f;
    }

    // Logic
    public override Tick() {
        if (agent.currentTarget != null)
        {
            agent.ChangeState(agent.chaseTargetState);
            return;
        }

        if (!agent.navMeshAgent.pathPending && agent.navMeshAgent.remainingDistance < agent.navMeshAgent.stoppingDistance && !agent.navMeshAgent.hasPath)
        {
            timer += Time.deltaTime;

            // We want agent to wait, before going to the next waypoint
            if (timer >= 2f)
            {
                GoToNextWaypoint();
                timer = 0f;
            }
        }
    }

    public override Exit() {} // Not needed in this state

    private void GoToNextWaypoint()
    {
        // Assign Transform[] patrolWaypoints from scene
        if (agent.patrolWaypoints.Length == 0) {
            return;
        }

        agent.navMeshAgent.destination = agent.patrolWaypoints[currentWaypointIndex].position;

        // We save position, if the agent will need to return back (e.g. he leaves position, to pursue the target)
        agent.initialPosition = agent.patrolWaypoints[currentWaypointIndex].position;

        if (currentWaypointIndex >= agent.patrolWaypoints.Length - 1) {
            isReversedPatrol = true;
        } else if (currentWaypointIndex == 0) {
            isReversedPatrol = false;
        }

        if (isReversedPatrol) {
            currentWaypointIndex--;
        } else {
            currentWaypointIndex++;
        }
    }
}
```

<hr>

<a name="fsm-chase-state"></a>
### Chase Target State:
In a target chase state, the agent follows the player and tries to reach the minimum attacking distance.

```csharp
public class ChaseTargetState : State
{
    public ChaseTargetState(AgentController agent) : base(agent) {}

    public override void Enter()
    {
        agent.navMeshAgent.enabled = true;
        agent.navMeshAgent.speed = 5f;
        agent.navMeshAgent.destination = agent.currentTarget.transform.position;
    }

    public override void Tick()
    {
        if (agent.currentTarget == null)
        {
            agent.investigationState.investigationPosition = soldier.lastKnownTargetPosition;
            agent.ChangeState(soldier.investigationState);
            return;
        }

        if (IsTargetInAttackRange())
        {
            agent.ChangeState(soldier.attackState);
            return;
        }

        MoveTowardsCurrentTarget();
    }

    private void MoveTowardsCurrentTarget()
    {
        agent.navMeshAgent.enabled = true;
        agent.navMeshAgent.destination = agent.currentTarget.transform.position;
    }

    private bool IsTargetInAttackRange()
    {
        return agent.distanceFromCurrentTarget <= 50f;
    }

}
```

<hr>

<a name="fsm-investigation-state"></a>
### Investigation State:
If agent looses the target, he should try to find it. In investigation state the agent first goes to the last know target position. Then it moves between random generated points and looks for target. After certain time passes, agent returns to his previous position.

```csharp
public class InvestigationState : State
{
    public float investigationRadius = 10f;
    public float investigationTime = 15f;

    public Vector3 investigationPosition;
    private float elapsedTime;
    private bool firstPointVisited;

    public InvestigationState(AgentController agent) : base(agent) {}

    public override void Enter()
    {
        elapsedTime = 0f; 
        firstPointVisited = false;

        agent.aimRig.weight = 1f; // soldier is aiming
        agent.navMeshAgent.destination = investigationPosition;
        agent.navMeshAgent.speed = soldier.config.speed;
    }

    public override void Tick()
    {
        if (agent.currentTarget != null)
        {
            if (agent.health.currentHealth <= 90)
            {
                agent.ChangeState(soldier.attackState);
                return;
            }

            agent.ChangeState(soldier.alertState);
            return;
        }

        if (firstPointVisited) 
        {
            elapsedTime += Time.deltaTime;
        }

        if (elapsedTime >= investigationTime)
        {
            agent.returnToPostState.position = soldier.initialPosition;
            agent.returnToPostState.rotation = soldier.initialRotation;
            agent.ChangeState(soldier.returnToPostState);
            return;
        }

        // Check if the agent has reached the destination point
        if (agent.navMeshAgent.remainingDistance <= agent.navMeshAgent.stoppingDistance)
        {
            firstPointVisited = true;
            agent.navMeshAgent.destination = GetRandomDestinationPoint();
        }
    }

    public override void Exit() {}

    private Vector3 GetRandomDestinationPoint()
    {
        // Calculate a new destination point around the center point
        Vector2 randomCircle = Random.insideUnitCircle * investigationRadius;
        Vector3 randomDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);
        Vector3 destinationPoint = investigationPosition + randomDirection;

        // Project the destination point onto the NavMesh
        NavMeshHit navMeshHit;
        NavMesh.SamplePosition(destinationPoint, out navMeshHit, investigationRadius, NavMesh.AllAreas);

        return navMeshHit.position;
    }
}
```

<hr>

<a name="fsm-attack-state"></a>
### Attack State:
In a attack state agents shoots at target.

```csharp
public class AttackState : State
{
    public AttackState(AgentController agent) : base(agent) {}

    override public void Enter()
    {
        soldier.aimRig.weight = 1f;
        soldier.weaponIK.isAiming = true;
        soldier.navMeshAgent.enabled = false;
        soldier.weaponIK.SetFiring(true);
    }

    override public void Tick()
    {
        agent.RotateTowardsTarget();

        if (agent.currentTarget == null)
        {
            soldier.investigationState.investigationPosition = soldier.lastKnownTargetPosition;
            soldier.ChangeState(soldier.investigationState);
            return;
        }

        if (agent.distanceFromCurrentTarget > 50f)
        {
            agent.ChangeState(agent.chaseTargetState);
            return;
        }
    }

    override public void Exit()
    {
        soldier.weaponIK.SetFiring(false);
        soldier.aimRig.weight = 0f;
        soldier.weaponIK.isAiming = false;
        soldier.navMeshAgent.enabled = true;
    }
}
```

<hr>

<a name="agent-controller"></a>
## Agent Controller
AgentController is a script on agent's GameObject, which is used for handling agent's states and animations.

```csharp
public class AgentController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private State currentState;

    [Header("Agent")]
    public Transform[] patrolWaypoints;
    public Vector3 initialPosition;
    public Quaternion initialRotation;

    [Header("Agent States")]
    public PatrolState patrolState;
    public ChaseTargetState chaseTargetState;
    public AttackState attackState;

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
        patrolState = new PatrolState(this);
        chaseTargetState = new ChaseTargetState(this);
        attackState = new AttackState(this);
    }

    public void ChangeState(State nextState)
    {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void RotateTowardsTarget()
    {
        if (currentTarget == null) return;
        Vector3 direction = currentTarget.position - transform.position; // Calculate the direction from the agent's position to the target's position
        direction.y = 0; // Ignore the vertical component of the direction
        transform.LookAt(transform.position + direction); // Rotate the agent towards the target
    }
}
```

<hr>

<a name="navigation-and-movement"></a>
## Navigation and Movement (NavMesh & NavMeshAgent)

The Navigation Mesh, or NavMesh, is a data structure that represents walkable surfaces within the game world. It acts as a blueprint for the AI characters to navigate the environment seamlessly. With NavMesh, the opponents can find optimal paths to reach their destinations, whether it's chasing the player, taking cover, or maneuvering around obstacles.

<a name="nav-mesh"></a>
### Generating a NavMesh

1. Mark all static objects in scene as _Static_.
2. Select all objects that should affect the navigation - walkable surfaces and obstacles.
3. Generate a NavMesh clicking _Bake_ button (open _Window > AI > Navigation_)

Generated NavMesh should look something like this. Blue color represents walkable areas for agents.

<p align="center">
    <img src="screenshots/generated_nav_mesh.png?raw=true" alt="Generated NavMesh" height="400">
</p>

<a name="nav-mesh-agent"></a>
### Adding a NavMeshAgent

NavMeshAgent is used for moving your object and navigating it on the NavMesh.

Here's how you can add and configure a NavMeshAgent for it:

1. **Select the GameObject**: Click on the AI opponent GameObject in the Unity Scene Hierarchy that you want to enable navigation for.
2. **Add NavMeshAgent Component**: In the Inspector window, click on the "Add Component" button. Search for "NavMeshAgent" and select it from the list to add the NavMeshAgent component to the GameObject.
3. **Configuring NavMeshAgent**:
    - _Speed_: Adjust the "Speed" parameter to set the movement speed of the AI opponent. This determines how fast the agent moves along the NavMesh.
    - _Stopping_ Distance: Set the "Stopping Distance" parameter to determine how close the AI opponent gets to its destination before stopping.
    - _Acceleration_: You can adjust the "Acceleration" parameter to control how quickly the AI opponent accelerates and decelerates while moving.

<p align="center">
    <img src="screenshots/nav_mesh_agent.png?raw=true" alt="NavMeshAgent Component" height="700">
</p>

<a name="movement"></a>
### Movement

NavMeshAgent is responsible for moving the GameObject. You just have to set the destintation.

```csharp
navMeshAgent.destination = currentTarget.transform.position; // Agent will start moving towards the destination
```

<hr>

<a name="player-detection"></a>
## Player Detection
In order for the agent to detect the player, it must initially verify whether the player is within its field of vision (FOV) and ensure that no obstacles obstruct the line of sight between them.

<br>
<p align="center">
    <img src="screenshots/player_detection_example.png?raw=true" alt="NavMeshAgent Component" height="300">
</p>

<a name="player-detection-implementation"></a>
### Implementation
We have implemented a function called _SearchForTarget_, tasked with determining whether a target is present and setting the _currentTarget_ variable accordingly. This function is executed at intervals of 1 second using Coroutines.

<br>

We start this Coroutine inside the Start method of AgentController.

```csharp
public void Start()
{
    StartCoroutine(SearchForTarget());
}
```

```csharp
private IEnumerator SearchForTarget()
{
    WaitForSeconds waitTime = new WaitForSeconds(1f);
    
    while (!isDead)
    {
        yield return waitTime;

        // Find all objects around agent's position
        // Detection layer is a Layer Mash set to "Player", because we only want to detect the player (ignore other layers)
        Collider[] colliders = Physics.OverlapSphere(transform.position, 40f, config.detectionLayer);

        if (colliders.Length <= 0)
        {
            currentTarget = null;
            yield return waitTime;
        }

        foreach (Collider collider in colliders)
        {
            // If object has component PlayerController (script on player's game object), we detected the player
            if (collider.TryGetComponent(out PlayerController player))
            {
                Vector3 directionToTarget = (player.transform.position - transform.position).normalized;

                // Check FOV
                if (Vector3.Angle(transform.forward, directionToTarget) < 100)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, player.transform.position);
                    Vector3 startPoint = new Vector3(transform.position.x, config.characterEyeLevel, transform.position.z);

                    // Check if there is a obstacle between agent and player
                    // Obstacle layer is layer with everything instead of Player
                    if (Physics.Raycast(startPoint, directionToTarget, distanceToTarget, config.obstacleLayer))
                    {
                        currentTarget = null;
                        break;
                    } 

                    currentTarget = player.transform;
                    lastKnownTargetPosition = currentTarget.position;
                    break;
                } else {
                     currentTarget = null;
                     break;
                }
            } else {
                currentTarget = null;
            }
        }
    }
}
```

<hr>

<a name="sound-detection"></a>
## Sound Detection
Sound detection works similar as player detection. When we play a certain sound effect on a game object, for example explosion, we must notify all agents in certain radius around that object. Each agent than responds to that sound.

<a name="sound-detection-implementation"></a>
### Implementation

```csharp
// Class for representing information about sound
public class MySound
{
    public readonly Vector3 position;
    public readonly float range;

    public MySound(Vector3 position, float range)
    {
        this.position = position;
        this.range = range;
    }
}
```

<br>

```csharp
// Every object, that can response to sound, must implement this interface
public interface IHear
{
    void RespondToSound(MySound sound);
}
```

<br>

```csharp
// Function for notifying agents nearby
// We call this function, when we play some sound effect in scene
public static class MySounds
{
    public static void MakeSound(MySound sound) 
    {
        int layerMask = 1 << 15; // 15 = Agent's Layer Mask (filter, for faster and more optimal search)
        Collider[] colliders = Physics.OverlapSphere(sound.position, sound.range, layerMask);

        foreach (Collider collider in colliders) 
        {
            if (collider.TryGetComponent(out IHear hearer))
            {
                hearer.RespondToSound(sound);
            }
        }
    }
}
```
