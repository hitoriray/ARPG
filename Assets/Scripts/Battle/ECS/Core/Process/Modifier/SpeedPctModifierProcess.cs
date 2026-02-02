using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using UnityEngine;

namespace Battle.ECS.Core.Process
{
    /// <summary>
    /// 速度百分比修改器处理
    /// </summary>
    public class SpeedPctModifierProcess : ProcessBase, IDeathProcess
    {
        public SpeedPctModifierProcess(BattleContext context) : base()
        {
            Init(context);
        }
        public void Apply(in Entity entity)
        {
            ref var speedModifier = ref entity.Get<SpeedPctModifier>();
            ref var modifier = ref entity.Get<Modifier>();
            if (!modifier.Target.IsAlive()) return;
            ref var move = ref modifier.Target.TryGetRef<Move>(out var hasMove);
            if (!hasMove) return;
            move.SpeedPct += speedModifier.Value;
            RayDebug.Log($"Applied: SpeedPct +{speedModifier.Value}");
        }
        public void OnDeath(in Entity entity)
        {
            ref var speedModifier = ref entity.Get<SpeedPctModifier>();
            ref var modifier = ref entity.Get<Modifier>();
            if (!modifier.Target.IsAlive()) return;
            ref var move = ref modifier.Target.TryGetRef<Move>(out var hasMove);
            if (!hasMove) return;
            move.SpeedPct -= speedModifier.Value;
            RayDebug.Log($"Removed: SpeedPct -{speedModifier.Value}");
            if (!entity.Has<Destroy>())
            {
                entity.Add(new Destroy());
            }
        }
    }
}