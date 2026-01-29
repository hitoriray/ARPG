namespace Framework
{
    /// <summary>
    /// 观察者
    /// </summary>
    public delegate void Observer();

    /// <summary>
    /// 可观察的对象
    /// </summary>
    public interface IObservable
    {
        bool Subscribe(Observer observer);
        bool UnSubscribe(Observer observer);
    }
    
    internal struct ObserverWrapper
    {
        public readonly Observer Observer;
        public readonly int Version;

        public ObserverWrapper(Observer observer, int version)
        {
            Observer = observer;
            Version = version;
        }
    }
}