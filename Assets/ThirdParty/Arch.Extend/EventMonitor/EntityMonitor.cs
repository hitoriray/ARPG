using System;
using Arch.Core;
namespace Arch.Extend.EventMonitor
{
    [Flags]
    public enum EntityEventFlag
    {
        Created = 1 << 0,
        Destroyed = 1 << 1,
    }

    /// <summary>
    /// 实体监视器
    /// </summary>
    public class EntityMonitor : IEventMonitor
    {
        public event Action<Entity> OnEvent;
        public EntityMonitor(World world, EntityEventFlag flag)
        {
            if ((flag & EntityEventFlag.Created) != 0)
                world.SubscribeEntityCreated(OnEventCall);
            if ((flag & EntityEventFlag.Destroyed) != 0)
                world.SubscribeEntityDestroyed(OnEventCall);
        }
        private void OnEventCall(in Entity entity)
        {
            OnEvent?.Invoke(entity);
        }
    }
}
