using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Core.Process;
using FixMath;

namespace Battle.ECS.System
{
    /// <summary>
    /// Tick系统：负责处理周期性效果（如DoT）
    /// </summary>
    public class TickSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _tickQuery;
        private readonly List<Entity> _toTick = new();

        public TickSystem(BattleContext context)
        {
            _context = context;
            _tickQuery = new QueryDescription().WithAll<Tick, LogicProcess>().WithNone<Death, Destroy>();
        }

        public void Update()
        {
            _toTick.Clear();

            var deltaTime = _context.LogicTime.DeltaTime;
            var process = new UpdateProcessor
            {
                DeltaTime = deltaTime,
                ToTick = _toTick
            };

            _context.World.InlineEntityQuery<UpdateProcessor, Tick>(in _tickQuery, ref process);

            // 执行Tick回调
            foreach (var entity in _toTick)
            {
                if (!entity.IsAlive()) continue;

                var logicProcess = entity.Get<LogicProcess>().Value;
                if (logicProcess is ITickProcess tickProcess)
                {
                    tickProcess.OnTick(entity);
                }

                // 检查次数
                ref var tick = ref entity.Get<Tick>();
                if (tick.Count > 0)
                {
                    tick.Count--;
                    if (tick.Count <= 0)
                    {
                        entity.Remove<Tick>(); // 移除Tick组件
                    }
                }
            }
        }

        private struct UpdateProcessor : IForEachWithEntity<Tick>
        {
            public FP DeltaTime;
            public List<Entity> ToTick;

            public void Update(Entity entity, ref Tick tick)
            {
                tick.Elapsed += DeltaTime;
                var actualInterval = tick.ActualInterval;

                if (tick.Elapsed >= actualInterval)
                {
                    tick.Elapsed -= actualInterval; // 保留余数
                    ToTick.Add(entity);
                }
            }
        }
    }
}