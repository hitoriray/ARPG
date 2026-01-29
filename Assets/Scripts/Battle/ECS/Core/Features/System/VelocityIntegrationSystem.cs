using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Battle.ECS.Core.Helper;
using FixMath;

namespace Battle.ECS.System
{
    /// <summary>
    /// 速度积分系统 - 根据Velocity更新Position
    /// </summary>
    public class VelocityIntegrationSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _desc = new QueryDescription().WithAll<Position, Velocity>().WithNone<Death, SyncFromView>();

        public VelocityIntegrationSystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
            var processor = new Processor { DeltaTime = _context.LogicTime.DeltaTime };
            _context.World.InlineEntityQuery<Processor, Position, Velocity>(in _desc, ref processor);
        }

        private struct Processor : IForEachWithEntity<Position, Velocity>
        {
            public FP DeltaTime;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(Entity entity, ref Position position, ref Velocity velocity)
            {
                if (velocity.Value == TSVector3.zero) return;
                ProfilerHelper.Begin("VelocityIntegration");
                position.Set(position.Value + velocity.Value * DeltaTime);
                ProfilerHelper.End();
            }
        }
    }
}
