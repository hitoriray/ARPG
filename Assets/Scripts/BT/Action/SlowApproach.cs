using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class SlowApproach : BossActionBase
    {
        public SharedTransform Target;
        public SharedFloat StopDistance;
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss() || Target.Value == null)
                return TaskStatus.Failure;

            boss.AI.FaceTarget = true;

            Vector3 toTarget = Target.Value.position - boss.transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            float stop = StopDistance.Value > 0f ? StopDistance.Value : 2.5f;
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
