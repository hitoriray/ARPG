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
            Add(new ShapeDetectionSystem(context));
            Add(new WeaponDetectionSystem(context));
            Add(new MovementSystem(context));
            Add(new VelocityIntegrationSystem(context));
            Add(new LookAtSystem(context));
            Add(new ResetInterpolatableStateSystem(context));
            
            Add(new DeathSystem(context));
            Add(new DestroySystem(context));
        }
    }
}
