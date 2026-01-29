using Arch.Core;

namespace Battle.ECS.Core.Process
{
    public interface ITickProcess
    {
        void OnTick(in Entity entity);
    }
}