using System;
using Arch.Core;
namespace Arch.Extend.EventMonitor
{
    [Flags]
    public enum ComponentEventFlag
    {
        Added = 1 << 0,
        Set = 1 << 1,
        Removed = 1 << 2,
    }

    /// <summary>
    ///  组件监视器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ComponentMonitor<T> : IEventMonitor
    {
        public event Action<Entity> OnEvent;
        public event Action<Entity, T> OnEventWithComp;

        public ComponentMonitor(World world, ComponentEventFlag flag)
        {
            if ((flag & ComponentEventFlag.Added) != 0)
                world.SubscribeComponentAdded<T>(OnEventCall);
            if ((flag & ComponentEventFlag.Set) != 0)
                world.SubscribeComponentSet<T>(OnEventCall);
            if ((flag & ComponentEventFlag.Removed) != 0)
                world.SubscribeComponentRemoved<T>(OnEventCall);
        }

        private void OnEventCall(in Entity entity, ref T comp)
        {
            OnEvent?.Invoke(entity);
            OnEventWithComp?.Invoke(entity, comp);
        }
    }
}
