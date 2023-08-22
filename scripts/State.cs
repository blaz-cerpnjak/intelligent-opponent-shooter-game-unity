/* 
 * This is the base class for all future states 
 */
public class State
{
    protected readonly AgentController agent;

    public AgentController(AgentController agent)
    {
        this.agent = agent;
    }

    public virtual void Enter() {}

    public virtual void Tick() {}

    public virtual void Exit() {}
}
