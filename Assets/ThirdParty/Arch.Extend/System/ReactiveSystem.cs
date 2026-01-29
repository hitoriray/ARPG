using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.Collector;
namespace Arch.Extend.System
{
    /// <summary>
    /// 响应式系统
    /// </summary>
    public abstract class ReactiveSystem : IEventHandlerSystem, IUpdateSystem
    {
        private EntityCollector _collector;
        private readonly List<Entity> _buffer = new List<Entity>();

        public virtual void SubscribeEvents()
        {
            _collector = GetCollector();
            _collector.Activate();
        }

        public virtual void Update()
        {
            if (_collector.Count == 0) return;
            _buffer.Clear();
            foreach (var entity in _collector)
            {
                _buffer.Add(entity);
            }
            _collector.Clear();
            foreach (var entity in _buffer)
            {
                if (Filter(entity))
                    Execute(entity);
            }
            _buffer.Clear();
        }

        protected abstract EntityCollector GetCollector();
        protected abstract void Execute(Entity entity);
        protected virtual bool Filter(Entity entity)
        {
            if (entity.IsAlive()) return true;
            return false;
        }
    }
}
