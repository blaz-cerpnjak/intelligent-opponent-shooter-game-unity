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
