using Arch.Core;
using Arch.Extend.EventMonitor;
using Arch.Extend.Matcher;

namespace Arch.Extend.EventTrigger
{
    /// <summary>
    /// 事件触发器 - 当监听的事件发生时设置触发标记
    /// </summary>
    public class EventTrigger
    {
        private readonly EntityMatcher _matcher;
        private readonly IEventMonitor[] _eventMonitors;
        private bool _isTriggered;

        public bool IsTriggered => _isTriggered;

        public EventTrigger(params IEventMonitor[] eventMonitors)
        {
            _eventMonitors = eventMonitors;
        }

        public EventTrigger(QueryDescription desc, params IEventMonitor[] eventMonitors)
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
            _isTriggered = true;
        }

        /// <summary>
        /// 重置触发标记
        /// </summary>
        public void Reset()
        {
            _isTriggered = false;
        }
    }
}
