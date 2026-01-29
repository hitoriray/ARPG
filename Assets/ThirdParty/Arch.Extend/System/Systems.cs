using System.Collections.Generic;

namespace Arch.Extend.System
{
    /// <summary>
    /// 系统容器类，管理五种系统类型的生命周期
    /// </summary>
    public class Systems : IInitializeSystem, IShutdownSystem, IUpdateSystem, ICleanupSystem, IEventHandlerSystem
    {
        private readonly List<IInitializeSystem> _initializeSystems = new();
        private readonly List<IEventHandlerSystem> _eventHandlerSystems = new();
        private readonly List<IShutdownSystem> _shutdownSystems = new();
        private readonly List<IUpdateSystem> _updateSystems = new();
        private readonly List<ICleanupSystem> _cleanupSystems = new();

        /// <summary>
        /// 添加系统到容器
        /// </summary>
        public virtual Systems Add(ISystem system)
        {
            if (system is IInitializeSystem initSystem)
                _initializeSystems.Add(initSystem);
            if (system is IEventHandlerSystem eventSystem)
                _eventHandlerSystems.Add(eventSystem);
            if (system is IShutdownSystem shutdownSystem)
                _shutdownSystems.Add(shutdownSystem);
            if (system is IUpdateSystem updateSystem)
                _updateSystems.Add(updateSystem);
            if (system is ICleanupSystem cleanupSystem)
                _cleanupSystems.Add(cleanupSystem);
            return this;
        }

        /// <summary>
        /// 初始化所有系统
        /// </summary>
        public virtual void Initialize()
        {
            for (int i = 0; i < _initializeSystems.Count; i++)
            {
                _initializeSystems[i].Initialize();
            }
        }

        /// <summary>
        /// 订阅所有事件
        /// </summary>
        public virtual void SubscribeEvents()
        {
            for (int i = 0; i < _eventHandlerSystems.Count; i++)
            {
                _eventHandlerSystems[i].SubscribeEvents();
            }
        }

        /// <summary>
        /// 卸载所有系统
        /// </summary>
        public virtual void Shutdown()
        {
            for (int i = 0; i < _shutdownSystems.Count; i++)
            {
                _shutdownSystems[i].Shutdown();
            }
        }

        /// <summary>
        /// 主循环更新
        /// </summary>
        public virtual void Update()
        {
            for (int i = 0; i < _updateSystems.Count; i++)
            {
                var updateSystem = _updateSystems[i];
                if (CheckUpdate(updateSystem) == false)
                    continue;
                updateSystem.Update();
            }
        }

        /// <summary>
        /// 检查系统是否应该更新（子类可重写实现UpdateLevel机制）
        /// </summary>
        protected virtual bool CheckUpdate(IUpdateSystem system)
        {
            return true;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public virtual void Cleanup()
        {
            for (int i = 0; i < _cleanupSystems.Count; i++)
            {
                var cleanupSystem = _cleanupSystems[i];
                if (CheckCleanup(cleanupSystem) == false)
                    continue;
                cleanupSystem.Cleanup();
            }
        }

        /// <summary>
        /// 检查系统是否应该清理
        /// </summary>
        protected virtual bool CheckCleanup(ICleanupSystem system)
        {
            return true;
        }
    }
}
