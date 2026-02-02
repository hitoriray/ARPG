using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Core.Helper;
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
                StackChangedBuffs = _stackChangedBuffs
            };

            _context.World.InlineEntityQuery<UpdateProcessor, Buff, BuffStack, BuffProperty>(in _buffQuery, ref process);

            // 处理堆叠变化
            foreach (var (buffEntity, removedCount) in _stackChangedBuffs)
            {
                if (!buffEntity.IsAlive()) continue;
                BuffHelper.RemoveStack(_context, buffEntity, removedCount);
            }
        }

        private struct UpdateProcessor : IForEachWithEntity<Buff, BuffStack, BuffProperty>
        {
            public FP DeltaTime;
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