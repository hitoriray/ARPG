using Arch.Core;
using Battle.ECS.Core.Interfaces;
using Battle.ECS.View.LogicToView;

namespace Battle.ECS.View.Context
{
    // 客户端上下文，同时有视图世界和逻辑世界
    public interface IClientContext : IContext
    {
        ViewContext ViewContext { get; } // 视图上下文
        World ViewWorld { get; } // 视图世界
        float InterpolationFactor { get; set; } // 插值因子
        BiDictionary<Entity, Entity> ViewEntityMap { get; } // 逻辑实体和视图实体的双向映射
        bool LoadedView { get; set; } // 是否加载了视图
        bool IsSubscribedEvent { get; set; } // 是否已订阅事件,viewWorld可能复用,复用前必须先设置此值并且在系统里进行判断
    }

    /// <summary>
    /// 战斗客户端上下文
    /// </summary>
    public interface IBattleClientContext : IBattleContext, IClientContext
    {
        // TODO: 应该不需要这个
        // IBattleEnvironment Environment { get; } // 战斗运行环境
        
        BattlePauseController PauseController { get; }
    }
}