public class AttackState : State
{
    public AttackState(AgentController agent) : base(agent) {}

    override public void Enter()
    {
        soldier.aimRig.weight = 1f;
        soldier.weaponIK.isAiming = true;
        soldier.navMeshAgent.enabled = false;
        soldier.weaponIK.SetFiring(true);
        // TODO - Implemente shooting logic
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
