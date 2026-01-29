namespace Battle.ECS.Core.Process
{
    public abstract class ProcessBase : IProcess
    {
        protected BattleContext Context { get; private set; }
        public void Init(BattleContext context)
        {
            Context = context;
        }
    }
}