using System.Collections;
using System.Collections.Generic;
using Arch.Core;
using Arch.Extend.EventMonitor;
namespace Arch.Extend.Collector
{
    /// <summary>
    /// 移除的组件收集器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RemovedCompCollector<T> : ICollector, IEnumerable<T>
    {
        private readonly List<T> _collectedComps = new();
        public int Count => _collectedComps.Count;

        private ComponentMonitor<T> _monitor;

        public RemovedCompCollector(World world)
        {
            _monitor = new(world, ComponentEventFlag.Removed);
        }

        public void Clear()
        {
            _collectedComps.Clear();
        }

        public void Activate()
        {
            _monitor.OnEventWithComp += OnEventCall;
        }

        public void Deactivate()
        {
            _monitor.OnEventWithComp -= OnEventCall;
        }

        private void OnEventCall(Entity entity, T comp)
        {
            _collectedComps.Add(comp);
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _collectedComps.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _collectedComps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collectedComps.GetEnumerator();
        }
    }
}
