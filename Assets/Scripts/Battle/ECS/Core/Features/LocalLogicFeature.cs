using Battle.ECS.Core;
using Battle.ECS.System;

namespace Battle.ECS.Features
{
    /// <summary>
    /// 本地战斗逻辑Feature
    /// </summary>
    public sealed class LocalLogicFeature : LogicFeatureBase
    {
        public LocalLogicFeature(BattleContext context) : base(context)
        {
            Add(new ViewToLogicSyncSystem(context));
            
            // ===== 新增 Buff 系统 =====
            Add(new System.BuffSystem(context));        // Buff 生命周期管理
            Add(new TickSystem(context));        // Tick 周期效果
            // ==========================
            
            Add(new ShapeDetectionSystem(context));
            Add(new WeaponDetectionSystem(context));
            Add(new DamageSystem(context));        // 伤害计算（防御/扣血/死亡判定）
            Add(new DamageNumberSystem(context));  // 飘字请求消费 → DamageNumberManager
            Add(new MovementSystem(context));
            Add(new VelocityIntegrationSystem(context));
            Add(new LookAtSystem(context));
            Add(new ResetInterpolatableStateSystem(context));
            
            Add(new DeathSystem(context));
            Add(new DestroySystem(context));
        }
    }
}
