using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Arch.Extend.System
{
    public class ProfilingSystems : Systems
    {
        private readonly List<SystemInfo> _initializedSystemInfos = new();
        private readonly List<SystemInfo> _eventHandlerSystems = new();
        private readonly List<SystemInfo> _shutdownSystemInfos = new();
        private readonly List<SystemInfo> _updateSystemInfos = new();
        private readonly List<SystemInfo> _cleanupSystemInfos = new();

        public override Systems Add(ISystem system)
        {
            if (system is IInitializeSystem initializeSystem) _initializedSystemInfos.Add(new SystemInfo(initializeSystem));
            if (system is IEventHandlerSystem eventHandlerSystem) _eventHandlerSystems.Add(new SystemInfo(eventHandlerSystem));
            if (system is IShutdownSystem shutdownSystem) _shutdownSystemInfos.Add(new SystemInfo(shutdownSystem));
            if (system is IUpdateSystem updateSystem) _updateSystemInfos.Add(new SystemInfo(updateSystem));
            if (system is ICleanupSystem lateUpdateSystem) _cleanupSystemInfos.Add(new SystemInfo(lateUpdateSystem));
            return base.Add(system);
        }

        public override void Initialize()
        {
            for (var i = 0; i < _initializedSystemInfos.Count; i++)
            {
                var systemInfo = _initializedSystemInfos[i];
                Profiler.BeginSample(systemInfo.InitName);
                ((IInitializeSystem)systemInfo.System).Initialize();
                Profiler.EndSample();
            }
        }

        public override void SubscribeEvents()
        {
            for (var i = 0; i < _eventHandlerSystems.Count; i++)
            {
                var systemInfo = _eventHandlerSystems[i];
                Profiler.BeginSample(systemInfo.SubscribeName);
                ((IEventHandlerSystem)systemInfo.System).SubscribeEvents();
                Profiler.EndSample();
            }
        }

        public override void Shutdown()
        {
            for (var i = 0; i < _shutdownSystemInfos.Count; i++)
            {
                var systemInfo = _shutdownSystemInfos[i];
                Profiler.BeginSample(systemInfo.ShutdownName);
                ((IShutdownSystem)systemInfo.System).Shutdown();
                Profiler.EndSample();
            }
        }

        public override void Update()
        {
            for (var i = 0; i < _updateSystemInfos.Count; i++)
            {
                var systemInfo = _updateSystemInfos[i];
                Profiler.BeginSample(systemInfo.UpdateName);
                var updateSystem = (IUpdateSystem)systemInfo.System;
                if (CheckUpdate(updateSystem))
                    ((IUpdateSystem)systemInfo.System).Update();
                Profiler.EndSample();
            }
        }

        public override void Cleanup()
        {
            for (var i = 0; i < _cleanupSystemInfos.Count; i++)
            {
                var systemInfo = _cleanupSystemInfos[i];
                Profiler.BeginSample(systemInfo.CleanupName);
                var cleanupSystem = (ICleanupSystem)systemInfo.System;
                if (CheckCleanup(cleanupSystem))
                    cleanupSystem.Cleanup();
                Profiler.EndSample();
            }
        }
    }
}
