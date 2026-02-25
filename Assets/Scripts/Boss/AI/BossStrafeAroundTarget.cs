using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossStrafeAroundTarget : Action
    {
        public SharedTransform Target;
        public SharedFloat Duration;
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;
        public SharedBool Clockwise;
        public SharedBool RandomizeClockwise;
        public SharedFloat BreakRange; // <=0 means ignore

        private BossController boss;
        private float startTime;
        private bool cachedClockwise;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
            startTime = Time.time;
            if (RandomizeClockwise.Value)
                cachedClockwise = Random.value < 0.5f;
            else
                cachedClockwise = Clockwise.Value;
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null || Target.Value == null)
                return TaskStatus.Failure;

            boss.AI.FaceTarget = true;

            Vector3 toTarget = Target.Value.position - boss.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return TaskStatus.Failure;

            float dist = toTarget.magnitude;
            float breakRange = BreakRange.Value;
            if (breakRange > 0f && dist <= breakRange)
            {
                boss.ClearDesiredMove();
                return TaskStatus.Success;
            }

            Vector3 tangent = cachedClockwise
                ? Vector3.Cross(toTarget.normalized, Vector3.up)
                : Vector3.Cross(Vector3.up, toTarget.normalized);

            float speedMult = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;

            boss.SetDesiredMove(tangent, speedMult, speedParam);

            float duration = Duration.Value;
            if (duration <= 0f)
                duration = 0.9f;

            if (Time.time - startTime >= duration)
            {
                boss.ClearDesiredMove();
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
        }
    }
}
