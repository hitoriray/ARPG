namespace Arch.Extend.Collector
{
    public interface ICollector
    {
        int Count { get; }
        void Clear();
        void Activate();
        void Deactivate();
    }
}
