using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossRetreatFromTarget : Action
    {
        public SharedTransform Target;
        public SharedFloat DesiredDistance;
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;

        private BossController boss;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null || Target.Value == null)
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
