using System.Collections;
using System.Collections.Generic;
using Arch.Core;
using Arch.Extend.EventMonitor;
using Arch.Extend.Matcher;
namespace Arch.Extend.Collector
{
    /// <summary>
    ///  实体收集器
    /// </summary>
    public class EntityCollector : ICollector, IEnumerable<Entity>
    {
        private readonly EntityMatcher _matcher;
        private readonly IEventMonitor[] _eventMonitors;
        private readonly HashSet<Entity> _collectedEntities = new();
        public int Count => _collectedEntities.Count;

        public EntityCollector(params IEventMonitor[] eventMonitors)
        {
            _eventMonitors = eventMonitors;
        }

        public EntityCollector(QueryDescription desc, params IEventMonitor[] eventMonitors)
        {
            _matcher = new EntityMatcher(desc);
            _eventMonitors = eventMonitors;
        }

        public void Activate()
        {
            foreach (var monitor in _eventMonitors)
            {
                monitor.OnEvent += OnEventCall;
            }
        }

        public void Deactivate()
        {
            foreach (var monitor in _eventMonitors)
            {
                monitor.OnEvent -= OnEventCall;
            }
        }

        private void OnEventCall(Entity entity)
        {
            if (_matcher != null && _matcher.Matches(entity) == false)
                return;
            _collectedEntities.Add(entity);
        }

        public void Clear()
        {
            _collectedEntities.Clear();
        }

        public HashSet<Entity>.Enumerator GetEnumerator()
        {
            return _collectedEntities.GetEnumerator();
        }

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
        {
            return _collectedEntities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collectedEntities.GetEnumerator();
        }
    }
}
