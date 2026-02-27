using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Boss;
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

        [BehaviorDesigner.Runtime.Tasks.Tooltip("A* 路径失效持续多久（秒）后尝试攀爬")]
        public SharedFloat ClimbAttemptDelay = new SharedFloat { Value = 0.5f };

        private BossClimbDetector climbDetector;

        // A* 路径失效计时
        private float pathFailedStartTime = -1f;
        private bool climbAttemptedThisBlock; // 本次阻断中只尝试一次

        public override void OnStart()
        {
            base.OnStart();
            climbDetector = GetComponent<BossClimbDetector>();
            pathFailedStartTime = -1f;
            climbAttemptedThisBlock = false;
        }

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss() || Target.Value == null)
                return TaskStatus.Failure;

            boss.AI.FaceTarget = true;

            // 停止判断用水平距离，避免高台导致永远触达不了
            Vector3 toTarget = Target.Value.position - boss.transform.position;
            Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
            float dist = toTargetFlat.magnitude;

            float stop = StopDistance.Value > 0f ? StopDistance.Value : 2.5f;
            if (dist <= stop)
            {
                boss.ClearDesiredMove();
                astarMover?.ClearDestination();
                pathFailedStartTime = -1f;
                climbAttemptedThisBlock = false;
                return TaskStatus.Success;
            }

            // ── A* 路径方向计算 ────────────────────────────────────
            Vector3 moveDir = Vector3.zero;
            bool hasPathDir = false;

            if (astarMover != null)
            {
                astarMover.SetDestination(Target.Value.position);
                moveDir = astarMover.DesiredDirection;
                hasPathDir = moveDir.sqrMagnitude > 0.0001f;
            }
            else
            {
                // 降级：直线方向
                moveDir = toTargetFlat.normalized;
                hasPathDir = true;
            }

            // ── A* 失效时的攀爬尝试（方案 A）───────────────────────
            if (!hasPathDir && climbDetector != null &&
                boss.MovementStateMachine?.climbState != null)
            {
                // 记录路径失效起始时间
                if (pathFailedStartTime < 0f)
                {
                    pathFailedStartTime = Time.time;
                    climbAttemptedThisBlock = false;
                }

                float delay = ClimbAttemptDelay.Value > 0f ? ClimbAttemptDelay.Value : 0.5f;
                bool delayPassed = Time.time - pathFailedStartTime >= delay;

                if (delayPassed && !climbAttemptedThisBlock)
                {
                    climbAttemptedThisBlock = true;

                    if (climbDetector.TryDetectClimbable(boss.transform.forward))
                    {
                        // 将检测结果写入 reusableData，供 BossClimbState 使用
                        boss.ReusableData.hit = climbDetector.WallHit;
                        boss.ReusableData.vaultPos = climbDetector.VaultPos;
                        boss.ReusableData.ObstructHeightLevel = climbDetector.HeightLevel;
                        boss.ReusableData.ClimbType = climbDetector.ClimbType;

                        boss.ClearDesiredMove();
                        astarMover?.ClearDestination();
                        boss.MovementStateMachine.ChangeState(boss.MovementStateMachine.climbState);
                        return TaskStatus.Running; // 攀爬状态自己会结束，行为树继续后续决策
                    }
                    // 检测不到可爬障碍物，重置计时，等待再次尝试
                    pathFailedStartTime = -1f;
                    climbAttemptedThisBlock = false;
                }

                boss.ClearDesiredMove();
                return TaskStatus.Running;
            }

            // 有路径方向时重置攀爬计时
            if (hasPathDir)
            {
                pathFailedStartTime = -1f;
                climbAttemptedThisBlock = false;
            }

            // ── 正常移动 ───────────────────────────────────────────
            float speedMult = MoveSpeedMultiplier.Value > 0f ? MoveSpeedMultiplier.Value : 1f;
            float speedParam = MoveSpeedParam.Value > 0f ? MoveSpeedParam.Value : 1f;
            boss.SetDesiredMove(moveDir, speedMult, speedParam);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            boss?.ClearDesiredMove();
            astarMover?.ClearDestination();
            pathFailedStartTime = -1f;
            climbAttemptedThisBlock = false;
        }
    }
}


