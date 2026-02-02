using Arch.Core;
using Arch.Core.Extensions;
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
                ExecuteEffect(buffEntity, config.startEffect);
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
            var config = buff.Config;

            // 执行周期效果
            if (config.periodicEffect != null)
            {
                ExecuteEffect(buffEntity, config.periodicEffect);
            }

            Debug.Log($"[{nameof(BuffProcess)}] {nameof(OnTick)}: {config.buffName}");
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
                ExecuteEffect(buffEntity, config.endEffect);
            }

            // 从目标的BuffList中移除
            if (buff.Target.IsAlive() && buff.Target.Has<BuffList>())
            {
                ref var buffList = ref buff.Target.Get<BuffList>();
                buffList.Remove(buffEntity);

                // 更新事件计数器
                BuffHelper.UpdateEventCounters(buffEntity, ref buffList, false);
            }

            // 销毁特效
            if (buff.Vfx.IsAlive())
            {
                buff.Vfx.Add(new Death());
            }

            // 添加Deth组件
            buffEntity.Add(new Death());

            Debug.Log($"[{nameof(BuffProcess)}] Buff销毁: {config.buffName}");
        }

        /// <summary>
        /// 执行效果（兼容旧系统）
        /// </summary>
        private void ExecuteEffect(Entity buffEntity, Config.BuffEffectDataBase effectData)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            var target = buff.Target;

            if (!target.IsAlive()) return;
            
            if (effectData is Config.SimpleBuffEffectData simpleEffect)
            {
                // TODO: 根据效果类型执行具体逻辑
                // 这里需要对接你的属性系统
                switch (simpleEffect.type)
                {
                    case BuffEffectType.Hp:
                        ApplyHpChange(target, (FP)simpleEffect.value);
                        break;
                }
                Debug.Log($"[BuffProcess] 执行效果: {simpleEffect.type} = {simpleEffect.value}");
            }
        }

        private void ApplyHpChange(Entity target, FP value)
        {
            if (!target.Has<Health>()) return;

            ref var hp = ref target.Get<Health>();
            hp.Current += value;

            // 限制在 [0, MaxHp] 范围
            if (target.Has<Battle.ECS.Component.Attribute>())
            {
                ref var attr = ref target.Get<Battle.ECS.Component.Attribute>();
                hp.Current = TSMath.Clamp(hp.Current, FP.Zero, attr.MaxHp);
            }

            // 如果血量归零，触发死亡
            if (hp.Current <= FP.Zero && !target.Has<Death>())
            {
                target.Add(new Death());
            }
        }
    }
}