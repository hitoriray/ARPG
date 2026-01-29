using System;
using Arch.Core;
using FixMath;
using Framework;

namespace Battle.ECS.Core.Interfaces
{
    /// <summary>
    ///  有Ecs世界的上下文
    /// </summary>
    public interface IContext : IDisposable
    {
        public World World { get; }
    }

    /// <summary>
    ///  有类对象池的上下文
    /// </summary>
    // public interface IPooledWorldContext : IContext
    // {
    //     public ClassPool ClassPool { get; }
    // }

    /// <summary>
    /// 战斗上下文
    /// </summary>
    public interface IBattleContext : IContext
    {
        public LogicTime LogicTime { get; }
        public TSRandom Random { get; }
        public UpdateLevel UpdateLevel { get; set; }
        public MutableLiveData<BattleState> State { get; }
    }
}