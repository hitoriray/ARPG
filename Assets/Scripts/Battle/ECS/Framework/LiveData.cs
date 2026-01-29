namespace Framework
{
    /// <summary>
    /// LiveData是一个可以观察的数据持有类
    /// 当数据更新时，会通知所有观察者。
    /// </summary>
    public abstract class LiveData : IObservable
    {
        private Subject _subject;
        public int DataVersion { get; private set; }

        public bool Subscribe(Observer observer)
        {
            _subject ??= new Subject(GetType());
            return _subject.Subscribe(observer);
        }

        public bool UnSubscribe(Observer observer)
        {
            return _subject != null && _subject.UnSubscribe(observer);
        }

        public void ClearObservers()
        {
            _subject?.ClearObservers();
        }

        protected virtual void OnDataUpdate()
        {
            DataVersion++;
            _subject?.Publish();
        }

        /// <summary>
        /// 手动触发数据更新事件
        /// </summary>
        public void Update()
        {
            OnDataUpdate();
        }
    }
    
    /// <summary>
    /// 一个可修改的LiveData，他明确持有一个T类型的数据并规范了数据的更新方法。
    /// 当数据更新时，会通知所有观察者。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LiveData<T> : LiveData, IDataWrapper<T>
    {
        public abstract bool SetData(T data);
    }
    
    public interface IDataWrapper<in T>
    {
        bool SetData(T data);
    }
}