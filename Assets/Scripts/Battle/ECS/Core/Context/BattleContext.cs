using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Extend.EntityIndex;
using Battle.ECS.Core.Interfaces;
using FixMath;
using Framework;

namespace Battle.ECS.Core
{
    /// <summary>
    /// 战斗上下文基类 - 持有所有战斗相关数据
    /// </summary>
    public abstract class BattleContext : IBattleContext
    {
        public World World { get; protected set; }
        // TODO:类对象池，可以用JKFrame中的代替
        // public ClassPool ClassPool { get; } = new ClassPool();
        public LogicTime LogicTime { get; protected set; } //逻辑时间
        public TSRandom Random { get; } //随机数生成器
        public UpdateLevel UpdateLevel { get; set; } // 更新等级
        public MutableLiveData<BattleState> State { get; } //战斗状态

        // TODO: 我觉得不需要用到这些吧
        // public UniformGridMgr UniformGridMgr { get; protected set; } //网格管理器
        // public SeekGrid SeekGrid { get; protected set; } //搜索用的临时网格
        // public Aoi Aoi { get; }
        // private TSBBox2D _liveArea;
        // public ref TSBBox2D LiveArea => ref _liveArea;
        // public PrimaryEntityIndex<int, Vehicle> VehicleIndex { get; protected set; }
        // public MultiEntityIndex<string, HeroInfo> HeroInfoIndex { get; protected set; }
        public PrimaryEntityIndex<int, Component.PlayerComp> PlayerIndex { get; protected set; }
        
        /// <summary>飘字服务，由外部（如GameSceneManager）在创建Runner后注入</summary>
        public IDamageNumberService DamageNumberService { get; set; }
        
        protected BattleContext(int randomSeed, FP logicDeltaTime)
        {
            World = World.Create();
            LogicTime = new LogicTime(logicDeltaTime);
            Random = TSRandom.New(randomSeed);
            State = new MutableLiveData<BattleState>();
            PlayerIndex = new PrimaryEntityIndex<int, Component.PlayerComp>(World, (in Component.PlayerComp playerComp) => playerComp.PlayerId);
        }

        protected BattleContext(int randomSeed) : this(randomSeed, FP.FromString("0.05"))
        {
        }

        public virtual void Dispose()
        {
            PlayerIndex = null;
            World.Dispose();
            World = null;
        }

        public bool IsDisposed()
        {
            return World == null;
        }
    }
}
