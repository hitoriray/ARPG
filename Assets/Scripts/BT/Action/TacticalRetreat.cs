using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class TacticalRetreat : BossActionBase
    {
        public SharedTransform Target;
        public SharedFloat DesiredDistance;
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss() || Target.Value == null)
                return TaskStatus.Failure;

            boss.AI.FaceTarget = true;

            Vector3 away = boss.transform.position - Target.Value.position;
            away.y = 0f;
            float dist = away.magnitude;

            float desired = DesiredDistance.Value > 0f ? DesiredDistance.Value : 6f;
            if (dist >= desired)
            {
                boss.ClearDesiredMove();
                return TaskStatus.Success;
            }

            float speedMult = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(away.normalized, speedMult, speedParam);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
        }
    }
}
