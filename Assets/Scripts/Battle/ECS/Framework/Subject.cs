using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Framework
{
    /// <summary>
    /// Subject是一个特殊类型的可观察对象
    /// 它充当了一个观察者和可观察对象之间的中介，用于处理订阅和发布事件。
    /// </summary>
    public class Subject : IObservable
    {
#if XY_DEV&&SUBJECT_LOG
        private static readonly IGameLogger log = GameLogFactory.GetLogger(nameof(Subject));
#endif
        //版本号,每次发布后版本号会增加
        public int Version { get; private set; }

        //所有观察者
        private List<ObserverWrapper> _observers;

        private string _name;

#if XY_DEV&&SUBJECT_LOG
        //发布者
        private StackTrace publisher;
#endif
        public Subject(string name)
        {
            _name = name;
        }

        public Subject(Type type)
        {
            _name = type.Name;
        }

        /// <summary>
        /// 添加观察者
        /// </summary>
        /// <param name="observer"></param>
        /// <returns>第一次注册返回true</returns>
        public bool Subscribe(Observer observer)
        {
            if (observer == null) return false;
            _observers ??= ListPool<ObserverWrapper>.Get();

            for (var index = 0; index < _observers.Count; index++)
            {
                //已存在
                var observerWrapper = _observers[index];
                if (observerWrapper.Observer == observer)
                {
                    observerWrapper = new ObserverWrapper(observer, Version);
                    _observers[index] = observerWrapper;
                    return false;
                }
            }

            //不存在
            _observers.Add(new ObserverWrapper(observer, Version));
            return true;
        }

        /// <summary>
        /// 移除观察者
        /// </summary>
        /// <param name="observer"></param>
        /// <returns>注销成功返回true</returns>
        public bool UnSubscribe(Observer observer)
        {
            if (observer == null || _observers == null) return false;
            var removed = false;

            for (var index = 0; index < _observers.Count; index++)
            {
                var observerWrapper = _observers[index];
                if (observerWrapper.Observer == observer)
                {
                    removed = true;
                    _observers.RemoveAt(index);
                    break;
                }
            }

            if (_observers.Count != 0) return removed;
            //所有观察者都被移除了，释放容器
            ClearObservers();
            return removed;
        }

        /// <summary>
        /// 清空所有观察者
        /// </summary>
        public void ClearObservers()
        {
            if (_observers == null) return;
            _observers.Clear();
            if (_observers.Capacity > 4) return; //如果容量大于4，则不释放容器，防止把池内的容器撑大
            ListPool<ObserverWrapper>.Release(_observers);
            _observers = null;
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish()
        {
#if XY_DEV&&SUBJECT_LOG
            publisher = new StackTrace(true);
#endif
            Version++;
            // TODO: 这里需要改
            // IocRepository.Get<SubjectModule>().Publish(this);
        }

#if XY_DEV&&SUBJECT_LOG
        //打印发布者
        public void PrintPublisher()
        {
            if (publisher == null) return;
            log.DebugFormat("Subject:{0}  version:{1}\n{2}", _name, version, publisher);
            publisher = null;
        }
#endif

        //收集所有观察者，交给SubjectHandler统一处理
        internal void CollectObservers(HashSet<Observer> allObservers)
        {
            if (_observers == null) return;
            foreach (var wrapper in _observers)
            {
                //观察者添加时的版本小于当前版本说明是在发布之前添加的，才需要执行
                if (wrapper.Version < Version)
                    allObservers.Add(wrapper.Observer);
            }
        }
    }
}