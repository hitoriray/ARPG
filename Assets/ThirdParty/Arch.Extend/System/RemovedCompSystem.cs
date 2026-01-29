using System.Collections.Generic;
using Arch.Core;
using Arch.Extend.Collector;
namespace Arch.Extend.System
{
    /// <summary>
    /// 移除组件响应系统
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RemovedCompSystem<T> : IEventHandlerSystem, IUpdateSystem
    {
        private readonly World _world;
        private RemovedCompCollector<T> _collector;
        private readonly List<T> _buffer = new List<T>();

        public RemovedCompSystem(World world)
        {
            _world = world;
        }
        public void SubscribeEvents()
        {
            _collector = new RemovedCompCollector<T>(_world);
            _collector.Activate();
        }
        public void Update()
        {
            if (_collector.Count == 0) return;
            _buffer.Clear();
            foreach (var comp in _collector)
            {
                _buffer.Add(comp);
            }
            _collector.Clear();
            foreach (var comp in _buffer)
            {
                Execute(comp);
            }
            _buffer.Clear();
        }

        protected abstract void Execute(T component);
    }
}
