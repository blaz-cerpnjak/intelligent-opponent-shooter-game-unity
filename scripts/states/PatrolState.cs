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
            agent.ChangeState(agent.alertState);
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
