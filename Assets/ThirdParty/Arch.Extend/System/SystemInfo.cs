namespace Arch.Extend.System
{
    public class SystemInfo
    {
        public readonly string Name;
        public readonly string InitName;
        public readonly string SubscribeName;
        public readonly string UpdateName;
        public readonly string ShutdownName;
        public readonly string CleanupName;
        private readonly ISystem _system;

        public SystemInfo(ISystem system)
        {
            _system = system;
            Name = system.GetType().Name;
            InitName = Name + "_Init";
            SubscribeName = Name + "_Subscribe";
            UpdateName = Name + "_Update";
            ShutdownName = Name + "_Shutdown";
            CleanupName = Name + "_Cleanup";
        }

        public ISystem System => _system;
    }
}
