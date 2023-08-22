public class AlertState : State
{
    public float alertTime = 2f;
    float timer;

    public AlertState(AgentController agent) : base(agent) {}

    public override void Enter()
    {
        agent.aimRig.weight = 1f;
        timer = 0f;
    }

    public override void Tick()
    {
        agent.RotateTowardsTarget();

        if (agent.currentTarget == null)
        {
            if (agent.lastKnownTargetPosition == null)
            {
                agent.returnToPostState.position = agent.initialPosition;
                agent.ChangeState(agent.returnToPostState);
                return;
            }

            agent.investigationState.investigationPosition = agent.lastKnownTargetPosition;
            agent.ChangeState(agent.investigationState);
            return;
        }
       
        timer += Time.deltaTime;

        if (timer >= alertTime && soldier.currentTarget != null)
        {
            agent.ChangeState(agent.chaseTargetState);
        }
    }

    override public void Exit() {}
}
