using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Config;
using UnityEngine;

namespace Battle.ECS.Core.Process
{
    /// <summary>
    /// 属性修改器
    /// </summary>
    public class AttrModifierProcess : ModifierProcess
    {
        public AttrModifierProcess(BattleContext context) : base()
        {
            Init(context);
        }
        
        public override void Apply(in Entity entity)
        {
            ref var attrModifier = ref entity.Get<AttrModifier>();
            ref var modifier = ref entity.Get<Modifier>();
            if (!modifier.Target.IsAlive())
                return;
            ref var targetAttr = ref modifier.Target.TryGetRef<Component.Attribute>(out var hasAttr);
            if (hasAttr == false)
            {
                RayDebug.Error($"Target entity does not have Attribute component.");
                return;
            }
            targetAttr.AddModifier(attrModifier.Type, attrModifier.Value, attrModifier.IsPercent);
            if (attrModifier.Type == AttributeType.MaxHP)
            {
                ApplyHealthSync(modifier.Target, ref targetAttr);
            }
            RayDebug.Log($"Applied: {attrModifier.Type} +{attrModifier.Value}");
        }
        
        public override void OnDeath(in Entity entity)
        {
            base.OnDeath(entity);
            ref var attrModifier = ref entity.Get<AttrModifier>();
            ref var modifier = ref entity.Get<Modifier>();
            if (!modifier.Target.IsAlive()) return;
            ref var targetAttr = ref modifier.Target.TryGetRef<Battle.ECS.Component.Attribute>(out var hasAttr);
            if (!hasAttr) return;
            targetAttr.RemoveModifier(attrModifier.Type, attrModifier.Value, attrModifier.IsPercent);
            
            if (attrModifier.Type == AttributeType.MaxHP)
            {
                ApplyHealthSync(modifier.Target, ref targetAttr);
            }
            
            RayDebug.Log($"Removed: {attrModifier.Type} -{attrModifier.Value}");
            
            // 标记销毁
            if (!entity.Has<Destroy>())
            {
                entity.Add(new Destroy());
            }
        }
        
        private void ApplyHealthSync(Entity target, ref Battle.ECS.Component.Attribute attr)
        {
            if (!target.Has<Health>()) return;
            ref var hp = ref target.Get<Health>();
            hp.Max = attr.MaxHp;
            // 确保当前血量不超过最大值
            if (hp.Current > hp.Max) hp.Current = hp.Max;
        }
    }
}