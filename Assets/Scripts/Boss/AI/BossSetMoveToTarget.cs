using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossSetMoveToTarget : Action
    {
        public SharedTransform Target;
        public SharedFloat StopDistance;
        public SharedFloat MaxChaseRange; // <=0 means ignore
        public SharedFloat ExitChaseRange; // <=0 means ignore (handoff to mid-range)
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
            boss.SetTarget(Target.Value);

            Vector3 toTarget = Target.Value.position - boss.transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            float exitRange = ExitChaseRange.Value;
            if (exitRange > 0f && dist <= exitRange)
            {
                boss.ClearDesiredMove();
                return TaskStatus.Success;
            }

            float maxRange = MaxChaseRange.Value;
            if (maxRange > 0f && dist > maxRange)
            {
                boss.ClearDesiredMove();
                return TaskStatus.Failure;
            }

            float stop = StopDistance.Value;
            if (stop <= 0f)
                stop = 1.5f;

            if (dist <= stop)
            {
                boss.ClearDesiredMove();
                return TaskStatus.Success;
            }

            float speedMultiplier = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(toTarget.normalized, speedMultiplier, speedParam);
            return TaskStatus.Running;
        }
    }
}
