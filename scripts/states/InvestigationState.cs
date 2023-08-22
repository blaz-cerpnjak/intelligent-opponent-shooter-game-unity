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
