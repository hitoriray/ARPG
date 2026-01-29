using Arch.Core;

namespace Battle.ECS.Core.Process
{
    public interface IDeathProcess
    {
        void OnDeath(in Entity entity);
    }
}