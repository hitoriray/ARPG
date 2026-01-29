using Arch.Extend.System;

namespace Battle.ECS.View
{
    /// <summary>
    /// 装载视图系统
    /// </summary>
    public interface ILoadViewSystem : ISystem
    {
        void LoadView(BattleViewReference viewReference);
    }

    /// <summary>
    /// 卸载视图系统
    /// </summary>
    public interface IUnloadViewSystem : ISystem
    {
        void UnloadView();
    }
}