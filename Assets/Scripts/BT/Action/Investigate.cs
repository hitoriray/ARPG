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

            Vector3 toTarget = targetPos - boss.transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            float stop = StopDistance.Value > 0f ? StopDistance.Value : 1.2f;
            if (dist <= stop)
            {
                boss.ClearDesiredMove();
                return TaskStatus.Success;
            }

            float speedMult = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(toTarget.normalized, speedMult, speedParam);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
        }
    }
}
