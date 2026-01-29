using UnityEngine;
namespace Arch.Extend.System
{
    /// <summary>
    /// 系统基接口
    /// </summary>
    public interface ISystem
    {
    }

    /// <summary>
    /// 初始化系统 - 战斗开始时调用一次
    /// </summary>
    public interface IInitializeSystem : ISystem
    {
        void Initialize();
    }

    /// <summary>
    /// 卸载系统 - 战斗结束时调用一次
    /// </summary>
    public interface IShutdownSystem : ISystem
    {
        void Shutdown();
    }

    /// <summary>
    /// 主循环系统 - 每帧调用
    /// </summary>
    public interface IUpdateSystem : ISystem
    {
        void Update();
    }

    /// <summary>
    /// 清理系统 - 通常在主循环系统之后调用
    /// </summary>
    public interface ICleanupSystem : ISystem
    {
        void Cleanup();
    }

    /// <summary>
    /// 事件处理系统 - 订阅事件
    /// </summary>
    public interface IEventHandlerSystem : ISystem
    {
        void SubscribeEvents();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Gizmos绘制系统 - Editor专用
    /// </summary>
    public interface IDrawGizmosSystem : ISystem
    {
        void OnDrawGizmos();
    }

    public interface IOnGUISystem : ISystem
    {
        void OnGUI();
    }
#endif
}
