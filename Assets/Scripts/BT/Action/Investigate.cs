using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class Investigate : BossActionBase
    {
        public SharedTransform Target;
        public SharedVector3 Point;
        public SharedBool UsePoint;
        public SharedFloat StopDistance;
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            Vector3 targetPos;
            if (UsePoint.Value)
            {
                targetPos = Point.Value;
            }
            else
            {
                if (Target.Value == null)
                    return TaskStatus.Failure;
                targetPos = Target.Value.position;
            }

            // 停止判断用水平距离
            Vector3 toTarget = targetPos - boss.transform.position;
            Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
            float dist = toTargetFlat.magnitude;

            float stop = StopDistance.Value > 0f ? StopDistance.Value : 1.2f;
            if (dist <= stop)
            {
                boss.ClearDesiredMove();
                astarMover?.ClearDestination();
                return TaskStatus.Success;
            }

            // 优先用 A* 路径方向，没有则降级为直线
            Vector3 moveDir;
            if (astarMover != null)
            {
                astarMover.SetDestination(targetPos);
                moveDir = astarMover.DesiredDirection;

                if (moveDir.sqrMagnitude < 0.0001f)
                {
                    boss.ClearDesiredMove();
                    return TaskStatus.Running;
                }
            }
            else
            {
                moveDir = toTargetFlat.normalized;
            }

            float speedMult = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(moveDir, speedMult, speedParam);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
            astarMover?.ClearDestination();
        }
    }
}

