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
        public SharedFloat MaxChaseRange;    // <=0 means ignore
        public SharedFloat ExitChaseRange;   // <=0 means ignore (handoff to mid-range)
        public SharedFloat MoveSpeedMultiplier;
        public SharedFloat MoveSpeedParam;

        private BossController boss;
        private BossAStarMover astarMover; // A* 寻路中间层（可选）

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
            astarMover = GetComponent<BossAStarMover>(); // 若未挂则为 null，自动降级
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null || Target.Value == null)
                return TaskStatus.Failure;

            boss.AI.FaceTarget = true;
            boss.SetTarget(Target.Value);

            // 距离判断始终使用水平距离（忽略高度差），避免高台导致判定失效
            Vector3 toTarget = Target.Value.position - boss.transform.position;
            Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
            float dist = toTargetFlat.magnitude;

            // ── 范围检查（按原逻辑不变）────────────────────────────
            float exitRange = ExitChaseRange.Value;
            if (exitRange > 0f && dist <= exitRange)
            {
                ClearMoveAndPath();
                return TaskStatus.Success;
            }

            float maxRange = MaxChaseRange.Value;
            if (maxRange > 0f && dist > maxRange)
            {
                ClearMoveAndPath();
                return TaskStatus.Failure;
            }

            float stop = StopDistance.Value <= 0f ? 1.5f : StopDistance.Value;
            if (dist <= stop)
            {
                ClearMoveAndPath();
                return TaskStatus.Success;
            }

            // ── 计算移动方向：优先使用 A* 路径方向，无则降级直线 ────
            Vector3 moveDir;
            if (astarMover != null)
            {
                astarMover.SetDestination(Target.Value.position);
                moveDir = astarMover.DesiredDirection;

                // A* 路径暂时无方向（路径计算中），等待下帧
                if (moveDir.sqrMagnitude < 0.0001f)
                {
                    boss.ClearDesiredMove();
                    return TaskStatus.Running;
                }
            }
            else
            {
                // 降级：直线方向（原有逻辑）
                moveDir = toTargetFlat.normalized;
            }

            float speedMultiplier = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(moveDir, speedMultiplier, speedParam);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
            astarMover?.ClearDestination();
        }

        // 统一的清除辅助方法，供多个退出分支复用
        private void ClearMoveAndPath()
        {
            boss?.ClearDesiredMove();
            astarMover?.ClearDestination();
        }
    }
}

