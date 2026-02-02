using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Battle.ECS.Core.Process;
using Config;
using FixMath;

namespace Battle.ECS.Core.Helper
{
    public class ModifierHelper
    {
        public static Entity CreateAttrModifier(BattleContext context, string track, Entity source, Entity target, AttributeType type, FP value, bool isPercent = false)
        {
            if (!source.IsAlive())
            {
                RayDebug.Error($"{track}: source entity is not alive.");
                return Entity.Null;
            }

            if (!target.IsAlive())
            {
                RayDebug.Error($"{track}: target entity is not alive.");
                return Entity.Null;
            }

            var attribute = target.TryGetRef<Component.Attribute>(out var hasAttr);
            if (hasAttr == false)
            {
                RayDebug.Error($"{track}: target entity does not have Attribute component.");
                return Entity.Null;
            }

            var process = new AttrModifierProcess(context);
            var entity = context.World.Create(
                new Modifier(source, target),
                new AttrModifier(type, value, isPercent),
                new LogicProcess(process));
            entity.AddDebugInfo($"modifier_attr_{type}_{value}");
            process.Apply(entity);
            return entity;
        }
        
        /// <summary>
        /// 创建速度百分比修改器
        /// </summary>
        public static Entity CreateSpeedPctModifier(
            BattleContext context,
            string track,
            Entity source,
            Entity target,
            FP value)
        {
            track ??= nameof(ModifierHelper);
            
            if (!source.IsAlive())
            {
                RayDebug.Error($"{track}: source entity is not alive.");
                return Entity.Null;
            }
            if (!target.IsAlive())
            {
                RayDebug.Error($"{track}: target entity is not alive.");
                return Entity.Null;
            }
            if (!target.Has<Move>())
            {
                RayDebug.Error($"{track}: target entity does not have Move component.");
                return Entity.Null;
            }
            var process = new SpeedPctModifierProcess(context);
            var modifierEntity = context.World.Create(
                new Modifier(source, target),
                new SpeedPctModifier(value),
                new LogicProcess(process)
            );
            modifierEntity.AddDebugInfo($"modifier_speedpct_{value}");
            
            process.Apply(modifierEntity);
            
            return modifierEntity;
        }
        /// <summary>
        /// 移除所有来源为指定实体的Modifier
        /// </summary>
        public static void RemoveAllModifiersBySource(BattleContext context, Entity sourceEntity)
        {
            var query = new QueryDescription().WithAll<Modifier>();
            context.World.Query(in query, (Entity e, ref Modifier m) =>
            {
                if (m.Source == sourceEntity && !e.Has<Death>())
                {
                    e.Add(new Death());
                }
            });
        }
    }
}