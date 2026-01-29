namespace Framework
{
    public interface IReadonlyLiveData<out T> : IObservable
    {
        T Value { get; }
    }
}