using Arch.Core;
using Arch.Core.Extensions;
using Attribute;
using Battle.ECS.Component;
using Battle.ECS.Core.Helper;
using Config;
using FixMath;
using UnityEngine;

namespace Battle.ECS.Core.Process
{
    public class BuffProcess : ProcessBase, IDeathProcess, ITickProcess
    {
        public BuffProcess(BattleContext context) : base()
        {
            Init(context);
        }

        /// <summary>
        /// Buff创建时回调
        /// </summary>
        public void OnCreate(in Entity buffEntity)
        {
            if (buffEntity.IsAlive() == false) return;
            ref var buff = ref buffEntity.TryGetRef<Buff>(out var hasBuff);
            if (hasBuff == false)
                return;
            
            var config = buff.Config;
            if (config.startEffect != null)
            {
                ExecuteEffect(nameof(OnCreate), buffEntity, config.startEffect);
            }

            Debug.Log($"[BuffProcess] Buff创建: {config.buffName} (ID:{config.buffId})");
        }

        /// <summary>
        /// Buff堆叠添加时回调
        /// </summary>
        public void OnStackAdded(in Entity buffEntity, int count)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            Debug.Log($"[BuffProcess] Buff叠加: {buff.Config.buffName} +{count}层");
        }

        /// <summary>
        /// Buff堆叠移除时回调
        /// </summary>
        public void OnStackRemoved(in Entity buffEntity, int count)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            Debug.Log($"[BuffProcess] Buff减少: {buff.Config.buffName} -{count}层");
        }

        /// <summary>
        /// Buff Tick时回调（周期性效果）
        /// </summary>
        public void OnTick(in Entity buffEntity)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            ref var buffStack = ref buffEntity.Get<BuffStack>();
            var config = buff.Config;

            // 执行周期效果（根据当前层数执行多次）
            if (config.periodicEffect != null)
            {
                int stackCount = buffStack.Value.Count;
                for (int i = 0; i < stackCount; i++)
                {
                    ExecuteEffect(nameof(OnTick), buffEntity, config.periodicEffect);
                }
                Debug.Log($"[{nameof(BuffProcess)}] {nameof(OnTick)}: {config.buffName} x{stackCount}层");
            }
        }

        /// <summary>
        /// Buff死亡时回调（堆叠为0或目标死亡）
        /// </summary>
        public void OnDeath(in Entity buffEntity)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            var config = buff.Config;

            // 执行结束效果
            if (config.endEffect != null)
            {
                ExecuteEffect(nameof(OnDeath), buffEntity, config.endEffect);
            }

            // 从目标的BuffList中移除
            if (buff.Target.IsAlive() && buff.Target.Has<BuffList>())
            {
                ref var buffList = ref buff.Target.Get<BuffList>();
                buffList.Remove(buffEntity);
            }

            // 销毁特效
            if (buff.Vfx.IsAlive())
            {
                buff.Vfx.Add(new Death());
            }

            // 标记销毁（不需要再添加Death，因为已经触发OnDeath了）
            if (!buffEntity.Has<Destroy>())
            {
                buffEntity.Add(new Destroy());
            }

            Debug.Log($"[{nameof(BuffProcess)}] Buff销毁: {config.buffName}");
        }

        /// <summary>
        /// 执行效果（兼容旧系统）
        /// </summary>
        private void ExecuteEffect(string track, Entity buffEntity, BuffEffectDataBase effectData)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            var target = buff.Target;

            if (!target.IsAlive()) return;
            
            if (effectData is SimpleBuffEffectData simpleEffect)
            {
                FP value = (FP)simpleEffect.value;
                ref var buffStack = ref buffEntity.TryGetRef<BuffStack>(out var hasBuffStack);
                if (hasBuffStack == false)
                    return;
                int stackCount = buffStack.Value.Count;
                Debug.Log($"{track ?? nameof(ExecuteEffect)}: buffType={simpleEffect.type}, value={simpleEffect.value}, stackCount={stackCount}");
                switch (simpleEffect.type)
                {
                    case BuffEffectType.Hp:
                        ApplyHpChange(target, value * stackCount);
                        break;
                    case BuffEffectType.AttackFixed:
                        ApplyAttributeModifier(target, AttributeType.Attack, value * stackCount, false);
                        break;
                    case BuffEffectType.AttackMultiplier:
                        ApplyAttributeModifier(target, AttributeType.Attack, value * stackCount, true);
                        break;
                    // TODO: 其他类型
                }
                Debug.Log($"[BuffProcess] 执行效果: {simpleEffect.type} = {simpleEffect.value}");
            }
        }

        private void ApplyHpChange(Entity target, FP value)
        {
            ref var health = ref target.TryGetRef<Health>(out var hasHealth);
            if (hasHealth == false)
                return;

            health.Current += value;
            // 限制在 [0, MaxHp] 范围
            if (target.Has<Battle.ECS.Component.Attribute>())
            {
                ref var attr = ref target.Get<Battle.ECS.Component.Attribute>();
                health.Current = TSMath.Clamp(health.Current, FP.Zero, attr.MaxHp);
            }

            // 如果血量归零，触发死亡
            if (health.Current <= FP.Zero && !target.Has<Death>())
            {
                target.Add(new Death());
            }
        }

        private void ApplyAttributeModifier(Entity target, AttributeType type, FP value, bool isPercent)
        {
            ref var attr = ref target.TryGetRef<Component.Attribute>(out var hasAttr);
            if (hasAttr == false)
                return;
            attr.AddModifier(type, value, isPercent);
        }
    }
}