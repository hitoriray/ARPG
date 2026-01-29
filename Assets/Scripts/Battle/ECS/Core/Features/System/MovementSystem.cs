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
    /// 移动系统 - 处理实体移动
    /// </summary>
    public class MovementSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _moveWithoutRotationDesc = new QueryDescription().WithAll<Position, Move, Velocity>().WithNone<ForbidMove, Death, SyncFromView>();
        private readonly QueryDescription _moveDesc = new QueryDescription().WithAll<Position, Move, Velocity, Rotation>().WithNone<ForbidMove, LookAt, Death, ForbidRotateOnMove, SyncFromView>();
        
        public MovementSystem(BattleContext context)
        {
            _context = context;
        }
        
        public void Update()
        {
            var processor = new MoveProcessor();
            _context.World.InlineEntityQuery<MoveProcessor, Position, Move, Velocity>(in _moveWithoutRotationDesc, ref processor);
            _context.World.InlineEntityQuery<MoveProcessor, Position, Move, Velocity, Rotation>(in _moveDesc, ref processor);
        }

        private struct MoveProcessor : IForEachWithEntity<Position, Move, Velocity>, IForEachWithEntity<Position, Move, Velocity, Rotation>
        {
            //移动
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(Entity entity, ref Position position, ref Move move, ref Velocity velocity)
            {
                if (move.IsMoving() == false)
                {
                    velocity.Value = TSVector3.zero;
                    return;
                }
                ProfilerHelper.Begin("MoveProcessor");
                velocity.Value = move.ActualSpeed * move.Direction;
                ProfilerHelper.End();
            }

            //3d旋转的移动
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(Entity entity, ref Position position, ref Move move, ref Velocity velocity, ref Rotation rotation)
            {
                if (move.IsMoving() == false) return;
                // 更新旋转
                ProfilerHelper.Begin("LookRotation-3D");
                rotation.Set(TSQuaternion.LookRotation(move.Direction));
                entity.Update<Rotation>();
                ProfilerHelper.End();
            }
        }
    }
}

