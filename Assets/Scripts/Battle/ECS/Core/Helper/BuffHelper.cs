using System;
using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Battle.ECS.Core.Process;
using Battle.ECS.View.Helper;
using Config;
using FixMath;
using UnityEngine;

namespace Battle.ECS.Core.Helper
{
    using Attribute = Battle.ECS.Component.Attribute;
    public static class BuffHelper
    {
        /// <summary>
        /// 添加Buff到目标实体
        /// </summary>
        public static Entity AddBuff(BattleContext context, string track, Entity caster, Entity target, BuffConfig buffConfig, int stackCount = 1)
        {
            // 检查目标是否有效
            if (!target.IsAlive() || target.Has<Death>() || target.Has<Destroy>())
            {
                RayDebug.Error($"{track ?? nameof(AddBuff)}: target is invalid or dead.");
                return Entity.Null;
            }

            // 检查BuffConfig是否有效
            if (buffConfig == null)
            {
                RayDebug.Error($"{track ?? nameof(AddBuff)}: BuffConfig is null.");
                return Entity.Null;
            }
            
            // 获取或创建BuffList
            ref var buffList = ref GetOrCreateBuffList(target);

            // 检查是否已存在相同Buff
            var existingBuff = buffList.GetBuff(buffConfig.buffId);
            if (existingBuff.IsAlive())
            {
                // 叠加已有Buff
                AddStack(context, existingBuff, caster, stackCount);
                return existingBuff;
            }

            // 创建新Buff Entity
            return CreateNewBuff(context, caster, target, buffConfig, stackCount);
        }

        /// <summary>
        /// 创建新的Buff Entity
        /// </summary>
        private static Entity CreateNewBuff(BattleContext context, Entity caster, Entity target, BuffConfig config, int stackCount)
        {
            var duration = (FP)config.duration;

            // 创建Buff Entity
            var buffEntity = context.World.Create(
                new Buff(config, caster) { Target = target },
                new BuffProperty
                {
                    Duration = duration,
                    DurationPct = FP.One,
                    MaxStack = config.maxStack,
                    StackMode = config.stackMode,
                    OverflowPolicy = config.overflowPolicy,
                    SpeedPctModifier = config.speedPctModifier
                },
                new BuffStack(config.maxStack)
            );

            // 添加到目标的BuffList
            ref var buffList = ref target.Get<BuffList>();
            buffList.AddBuff(buffEntity);

            // 初始化堆叠
            for (int i = 0; i < stackCount; i++)
            {
                InternalAddStack(buffEntity, caster, duration);
            }

            // 添加Tick组件（如果有周期效果）
            if (config.tickInterval > 0)
            {
                buffEntity.Add(new Tick((FP)config.tickInterval, config.tickCount));
            }

            // 添加Process
            var buffProcess = new BuffProcess(context);
            buffEntity.Add(new LogicProcess(buffProcess));

            // 应用开始属性修正
            ApplyStartAttrModifiers(buffEntity, target, stackCount);
            
            // 触发OnCreate回调
            buffProcess.OnCreate(buffEntity);

            return buffEntity;
        }

        /// <summary>
        /// 添加堆叠
        /// </summary>
        public static void AddStack(BattleContext context, Entity buffEntity, Entity caster, int count)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            ref var buffProperty = ref buffEntity.Get<BuffProperty>();
            ref var buffStack = ref buffEntity.Get<BuffStack>();

            int addedCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (buffStack.Value.Count >= buffProperty.MaxStack)
                {
                    // 处理溢出
                    HandleOverflow(buffEntity, caster, buffProperty.Duration);
                }
                else
                {
                    InternalAddStack(buffEntity, caster, buffProperty.Duration);
                    addedCount++;
                }
            }

            // 根据叠加模式刷新时间
            if (addedCount > 0 && buffProperty.StackMode == BattleBuffStackMode.RefreshDuration)
            {
                // RefreshDuration模式：叠加时刷新所有层的时间
                RefreshAllStackTime(ref buffStack, buffProperty.Duration);
            }

            if (addedCount > 0)
            {
                // 应用属性修正
                ApplyStartAttrModifiers(buffEntity, buff.Target, addedCount);

                // 触发OnStackAdded回调
                if (buffEntity.Has<LogicProcess>())
                {
                    var process = buffEntity.Get<LogicProcess>().Value as BuffProcess;
                    process?.OnStackAdded(buffEntity, addedCount);
                }
            }
        }

        /// <summary>
        /// 刷新所有层的剩余时间（用于RefreshDuration模式）
        /// </summary>
        private static void RefreshAllStackTime(ref BuffStack buffStack, FP duration)
        {
            for (int i = 0; i < buffStack.Value.Count; i++)
            {
                ref var stackInfo = ref buffStack.Value[i];
                stackInfo.RemainingTime = duration;
            }
        }

        /// <summary>
        /// 内部添加堆叠（不检查上限）
        /// </summary>
        private static void InternalAddStack(Entity buffEntity, Entity caster, FP duration)
        {
            ref var buffStack = ref buffEntity.Get<BuffStack>();
            buffStack.Add(new BuffStackInfo
            {
                Caster = caster,
                RemainingTime = duration
            });
        }

        /// <summary>
        /// 处理溢出策略
        /// </summary>
        private static void HandleOverflow(Entity buffEntity, Entity caster, FP duration)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            ref var buffProperty = ref buffEntity.Get<BuffProperty>();
            ref var buffStack = ref buffEntity.Get<BuffStack>();

            switch (buffProperty.OverflowPolicy)
            {
                case BattleBuffOverflowPolicy.ReplaceOldest:
                    // 先撤销被替换层的属性
                    RemoveStartAttrModifiers(buffEntity, buff.Target, 1);
                    buffStack.RemoveFirst();
                    InternalAddStack(buffEntity, caster, duration);
                    break;

                case BattleBuffOverflowPolicy.ReplaceLowestPriority:
                    // TODO: 实现优先级查找
                    RemoveStartAttrModifiers(buffEntity, buff.Target, 1);
                    buffStack.RemoveLast();
                    InternalAddStack(buffEntity, caster, duration);
                    break;

                case BattleBuffOverflowPolicy.DiscardNewest:
                    // 不添加新层
                    break;
            }
        }

        /// <summary>
        /// 移除Buff堆叠
        /// </summary>
        public static void RemoveStack(BattleContext context, Entity buffEntity, int count)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            ref var buffStack = ref buffEntity.Get<BuffStack>();

            int removedCount = Mathf.Min(count, buffStack.Value.Count);
            for (int i = 0; i < removedCount; i++)
            {
                buffStack.RemoveLast();
            }

            // 移除属性修正（每层移除时都要撤销StartAttrModifiers）
            RemoveStartAttrModifiers(buffEntity, buff.Target, removedCount);

            // 触发回调
            if (buffEntity.Has<LogicProcess>())
            {
                var process = buffEntity.Get<LogicProcess>().Value as BuffProcess;
                process?.OnStackRemoved(buffEntity, removedCount);
            }

            // 如果没有堆叠了，标记死亡
            if (buffStack.Value.Count == 0)
            {
                buffEntity.Add(new Death());
            }
        }

        /// <summary>
        /// 应用开始属性修正
        /// </summary>
        /// <param name="buffEntity"></param>
        /// <param name="target"></param>
        /// <param name="stackCount"></param>
        public static void ApplyStartAttrModifiers(Entity buffEntity, Entity target, int stackCount)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            var config = buff.Config;
            if (config.StartAttrModifiers == null || config.StartAttrModifiers.Length == 0)
                return;
            ref var targetAttr = ref target.TryGetRef<Attribute>(out var hasAttr);
            if (hasAttr == false)
                return;
            for (int i = 0; i < stackCount; i++)
            {
                foreach (var modifier in config.StartAttrModifiers)
                {
                    targetAttr.AddModifier(modifier.type, (FP)modifier.value, modifier.mode == AttrModifyMode.Percent);
                }
            }
        }

        /// <summary>
        /// 应用属性修正
        /// </summary>
        public static void ApplyPeriodicAttrModifiers(Entity buffEntity, Entity target, int stackCount)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            var config = buff.Config;

            if (config.PeriodicAttrModifier == null || config.PeriodicAttrModifier.Length == 0) return;
            ref var targetAttr = ref target.TryGetRef<Attribute>(out var hasAttr);
            if (hasAttr == false)
                return;
            ref var health = ref target.TryGetRef<Health>(out var hasHealth);
            for (int i = 0; i < stackCount; i++)
            {
                foreach (var modifier in config.PeriodicAttrModifier)
                {
                    if (modifier.type == AttributeType.HP && hasHealth)
                    {
                        var value = modifier.mode == AttrModifyMode.Percent ? health.Max * (FP)modifier.value / 100 : (FP)modifier.value;
                        health.Current = TSMath.Clamp(health.Current + value, FP.Zero, health.Max);
                        if (health.Current <= FP.Zero && target.Has<Death>() == false)
                        {
                            target.Add(new Death());
                        }
                    }
                    else
                    {
                        targetAttr.AddModifier(modifier.type, (FP)modifier.value, modifier.mode == AttrModifyMode.Percent);
                    }
                }
            }
        }
        
        /// <summary>
        /// 移除开始属性修正
        /// </summary>
        public static void RemoveStartAttrModifiers(Entity buffEntity, Entity target, int stackCount)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            var config = buff.Config;

            if (config.StartAttrModifiers == null || config.StartAttrModifiers.Length == 0) return;
            ref var targetAttr = ref target.TryGetRef<Attribute>(out var hasAttr);
            if (hasAttr == false)
                return;
            for (int i = 0; i < stackCount; i++)
            {
                foreach (var modifier in config.StartAttrModifiers)
                {
                    targetAttr.RemoveModifier(modifier.type, (FP)modifier.value, modifier.mode == AttrModifyMode.Percent);
                }
            }
        }
        
        // RemovePeriodicAttrModifiers已删除 - 周期效果直接修改HP等值，无需撤销

        /// <summary>
        /// 获取或创建BuffList
        /// </summary>
        private static ref BuffList GetOrCreateBuffList(in Entity entity)
        {
            if (entity.IsAlive() == false) throw new InvalidOperationException($"{nameof(GetOrCreateBuffList)}: {nameof(entity)} is not alive.");
            if (!entity.Has<BuffList>())
            {
                entity.Add(new BuffList(4));
            }

            return ref entity.Get<BuffList>();
        }
    }
}