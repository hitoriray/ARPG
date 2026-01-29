using Battle.ECS.Core.Process;

namespace Battle.ECS.Component
{
    public readonly struct LogicProcess
    {
        public readonly IProcess Value;
        public LogicProcess(IProcess value)
        {
            Value = value;
        }
    }
}