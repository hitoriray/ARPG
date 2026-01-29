using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using FixMath;

namespace Battle.ECS.System
{
    /// <summary>
    /// LookAtSystem 处理实体的总是朝向目标
    /// </summary>
    public class LookAtSystem : IUpdateSystem
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _desc = new QueryDescription().WithAll<Position, LookAt, Rotation>().WithNone<Death, SyncFromView>();

        public LookAtSystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
            var processor = new Process();
            _context.World.InlineEntityQuery<Process, Position, LookAt, Rotation>(in _desc, ref processor);
        }

        private struct Process : IForEachWithEntity<Position, LookAt, Rotation>
        {
            public void Update(Entity entity, ref Position position, ref LookAt lookAt, ref Rotation rotation)
            {
                var targetPosition = GetTargetPosition(ref lookAt);
                var direction = targetPosition - position.Value;
                direction.y = FP.Zero;
                if (direction.sqrMagnitude < FP.Epsilon) return;
                rotation.Set(TSQuaternion.LookRotation(direction));
                entity.Update<Rotation>();
            }
            
            private TSVector3 GetTargetPosition(ref LookAt lookAt)
            {
                if (lookAt.Target.IsAlive() == false)
                    return lookAt.TargetPos;
                ref var targetPosition = ref lookAt.Target.TryGetRef<Position>(out var hasPosition);
                if (hasPosition == false) return lookAt.TargetPos;
                lookAt.TargetPos = targetPosition.Value; //更新目标位置
                return lookAt.TargetPos;
            }
        }
    }
}
