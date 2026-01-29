using System.Runtime.CompilerServices;
using Arch.Extend.System;

namespace Battle.ECS.Core
{
    /// <summary>
    /// 更新等级枚举 - 控制系统暂停粒度
    /// </summary>
    public enum UpdateLevel
    {
        /// <summary>
        /// 随逻辑时间更新，含各种技能相关逻辑
        /// </summary>
        LogicTime,
        
        /// <summary>
        /// 技能无关逻辑，但在暂停时需要停止
        /// </summary>
        LogicFree,
        
        // ---- add before this line ----
        
        /// <summary>
        /// 总是更新，如各种渲染系统
        /// </summary>
        Always,
    }
    
    /// <summary>
    /// 更新策略接口
    /// </summary>
    public interface IUpdatePolicy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        UpdateLevel GetUpdateLevel();
    }

    /// <summary>
    /// 逻辑时间策略
    /// </summary>
    public struct GameLogic : IUpdatePolicy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UpdateLevel GetUpdateLevel() => UpdateLevel.LogicTime;
    }
    
    /// <summary>
    /// 逻辑自由策略
    /// </summary>
    public struct GameFree : IUpdatePolicy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UpdateLevel GetUpdateLevel() => UpdateLevel.LogicFree;
    }

    /// <summary>
    /// 总是更新策略
    /// </summary>
    public struct Always : IUpdatePolicy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UpdateLevel GetUpdateLevel() => UpdateLevel.Always;
    }

    /// <summary>
    /// 带UpdateLevel的系统接口
    /// </summary>
    public interface IUpdateLevelSystem : IUpdateSystem
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        UpdateLevel GetUpdateLevel();
    }

    /// <summary>
    /// 泛型UpdateLevel系统接口
    /// </summary>
    public interface IUpdateLevelSystem<TCheckPolicy> : IUpdateLevelSystem 
        where TCheckPolicy : struct, IUpdatePolicy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        UpdateLevel IUpdateLevelSystem.GetUpdateLevel() => default(TCheckPolicy).GetUpdateLevel();
    }
}
