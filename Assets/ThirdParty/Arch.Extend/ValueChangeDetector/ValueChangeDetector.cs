using System.Collections.Generic;
namespace Arch.Extend.EventTrigger
{
    /// <summary>
    ///  变量变化触发检测
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueChangeDetector<T>
    {
        private T _lastValue;

        public ValueChangeDetector(T initialValue)
        {
            _lastValue = initialValue;
        }

        public bool IsChanged(T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(_lastValue, newValue))
                return false;
            _lastValue = newValue;
            return true;
        }
    }
}
