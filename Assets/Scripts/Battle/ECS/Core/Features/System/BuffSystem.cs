using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Core.Helper;
using Battle.ECS.Core.Process;
using FixMath;

namespace Battle.ECS.System
{
    /// <summary>
    /// Buff系统：负责Buff生命周期管理
    /// </summary>
    public class BuffSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _buffQuery;
        private readonly List<(Entity, int)> _stackChangedBuffs = new();

        public BuffSystem(BattleContext context)
        {
            _context = context;
            _buffQuery = new QueryDescription().WithAll<Buff, BuffStack, BuffProperty>().WithNone<Death, Destroy>();
        }

        public void Update()
        {
            _stackChangedBuffs.Clear();

            var deltaTime = _context.LogicTime.DeltaTime;
            var process = new UpdateProcessor
            {
                DeltaTime = deltaTime,
                Context = _context,
                StackChangedBuffs = _stackChangedBuffs
            };

            _context.World.InlineEntityQuery<UpdateProcessor, Buff, BuffStack, BuffProperty>(in _buffQuery, ref process);

            // 处理堆叠变化（移除属性修正、触发回调）
            // 注意：UpdateProcessor 已经从 buffStack 中移除了过期的层，这里不需要再移除！
            foreach (var (buffEntity, removedCount) in _stackChangedBuffs)
            {
                if (!buffEntity.IsAlive()) continue;

                ref var buff = ref buffEntity.Get<Buff>();
                ref var buffStack = ref buffEntity.Get<BuffStack>();

                // 移除属性修正
                RemoveAttrModifiers(buffEntity, buff.Target, removedCount);

                // 触发OnStackRemoved回调
                if (buffEntity.Has<LogicProcess>())
                {
                    var buffProcess = buffEntity.Get<LogicProcess>().Value as BuffProcess;
                    buffProcess?.OnStackRemoved(buffEntity, removedCount);
                }

                // 如果没有堆叠了，标记死亡
                if (buffStack.Value.Count == 0)
                {
                    buffEntity.Add(new Death());
                }
            }
        }

        /// <summary>
        /// 移除属性修正
        /// </summary>
        private void RemoveAttrModifiers(Entity buffEntity, Entity target, int stackCount)
        {
            if (!target.IsAlive() || !target.Has<Battle.ECS.Component.Attribute>()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            var config = buff.Config;

            if (config.AttrModifiers == null || config.AttrModifiers.Length == 0) return;

            ref var targetAttr = ref target.Get<Battle.ECS.Component.Attribute>();

            for (int i = 0; i < stackCount; i++)
            {
                foreach (var modifier in config.AttrModifiers)
                {
                    FP value = (FP)modifier.value;
                    bool isPercent = modifier.mode == Config.AttrModifyMode.Percent;
                    targetAttr.RemoveModifier(modifier.type, value, isPercent);
                }
            }
        }

        private struct UpdateProcessor : IForEachWithEntity<Buff, BuffStack, BuffProperty>
        {
            public FP DeltaTime;
            public BattleContext Context;
            public List<(Entity, int)> StackChangedBuffs;

            public void Update(Entity entity, ref Buff buff, ref BuffStack buffStack, ref BuffProperty buffProperty)
            {
                // 检查目标是否存活
                if (!buff.Target.IsAlive() || buff.Target.Has<Death>())
                {
                    entity.Add(new Death());
                    return;
                }

                // 永久Buff不更新时间
                if (buffProperty.StackMode == BattleBuffStackMode.Permanent)
                {
                    return;
                }

                if (buff.Config.periodicEffect != null && buff.Config.tickInterval > 0)
                {
                    buff.TickTimer -= DeltaTime;
                    if (buff.TickTimer <= FP.Zero)
                    {
                        buff.TickTimer += (FP)buff.Config.tickInterval;
                        ref var logicProcess = ref entity.TryGetRef<LogicProcess>(out var hasLogicProcess);
                        if (hasLogicProcess)
                        {
                            ((BuffProcess)logicProcess.Value)?.OnTick(entity);
                        }
                    }
                }

                // 根据叠加模式更新时间
                int removedCount = 0;
                switch (buffProperty.StackMode)
                {
                    case BattleBuffStackMode.RefreshDuration:
                        removedCount = UpdateRefreshMode(ref buffStack, DeltaTime, buffProperty.Duration);
                        break;
                    case BattleBuffStackMode.IndependentDuration:
                        removedCount = UpdateIndependentMode(ref buffStack, DeltaTime);
                        break;
                    case BattleBuffStackMode.SequentialDuration:
                        removedCount = UpdateSequentialMode(ref buffStack, DeltaTime, buffProperty.Duration);
                        break;
                }

                if (removedCount > 0)
                {
                    StackChangedBuffs.Add((entity, removedCount));
                }
            }

            /// <summary>
            /// 刷新模式：所有层共享一个计时器，到期后全部移除
            /// </summary>
            private static int UpdateRefreshMode(ref BuffStack buffStack, FP deltaTime, FP duration)
            {
                if (buffStack.Value.Count == 0) return 0;

                ref var lastStack = ref buffStack.Value[^1];
                lastStack.RemainingTime -= deltaTime;

                if (lastStack.RemainingTime <= FP.Zero)
                {
                    int count = buffStack.Value.Count;
                    buffStack.Value.Clear();
                    return count; // 全部移除
                }

                return 0;
            }

            /// <summary>
            /// 独立模式：每层单独计时，过期后单独移除
            /// </summary>
            private static int UpdateIndependentMode(ref BuffStack buffStack, FP deltaTime)
            {
                int removedCount = 0;
                for (int i = buffStack.Value.Count - 1; i >= 0; i--)
                {
                    ref var stackInfo = ref buffStack.Value[i];
                    stackInfo.RemainingTime -= deltaTime;

                    if (stackInfo.RemainingTime <= FP.Zero)
                    {
                        buffStack.Value.RemoveAt(i);
                        removedCount++;
                    }
                }

                return removedCount;
            }

            /// <summary>
            /// 顺序模式：最后一层计时，过期后移除并重置下一层
            /// </summary>
            private static int UpdateSequentialMode(ref BuffStack buffStack, FP deltaTime, FP duration)
            {
                if (buffStack.Value.Count == 0) return 0;

                ref var lastStack = ref buffStack.Value[^1];
                lastStack.RemainingTime -= deltaTime;

                if (lastStack.RemainingTime <= FP.Zero)
                {
                    buffStack.Value.RemoveAt(buffStack.Value.Count - 1);

                    // 重置下一层的时间
                    if (buffStack.Value.Count > 0)
                    {
                        ref var nextStack = ref buffStack.Value[^1];
                        nextStack.RemainingTime = duration;
                    }

                    return 1;
                }

                return 0;
            }
        }
    }
}