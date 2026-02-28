using UnityEngine;

namespace Boss
{
    public class BossMoveState : BossMovementState
    {
        public BossMoveState(BossController boss) : base(boss) { }

        public override void OnEnter()
        {
            if (playerSO == null)
                return;

            animancer.Play(playerSO.playerMovementData.PlayerMoveLoopData.moveLoop);
            if (boss.CharacterConfig != null)
                boss.applyFullRootMotion = boss.CharacterConfig.ApplyRootMotionForMove;
        }

        public override void OnUpdate()
        {
            Vector3 dir = boss.AI.MoveDir;
            if (dir.sqrMagnitude <= 0.0001f)
                return;

            if (boss.AI.FaceTarget && boss.AI.Target != null)
            {
                Vector3 toTarget = boss.AI.Target.position - boss.transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    UpdateRotation(toTarget);
                else
                    UpdateRotation(dir);
            }
            else
            {
                UpdateRotation(dir);
            }
            float speedParam = boss.AI.MoveSpeedParam > 0f ? boss.AI.MoveSpeedParam : 1f;
            UpdateSpeedParam(speedParam);

            // 模拟“锁敌移动”参数，支持侧移/后退而不改变朝向
            if (reusableData != null)
            {
                if (boss.AI.FaceTarget && boss.AI.Target != null)
                {
                    reusableData.lockValueParameter.TargetValue = 1f;
                    Vector3 local = boss.transform.InverseTransformDirection(dir.normalized);
                    reusableData.lock_X_ValueParameter.TargetValue = local.x * speedParam;
                    reusableData.lock_Y_ValueParameter.TargetValue = local.z * speedParam;
                }
                else
                {
                    reusableData.lockValueParameter.TargetValue = 0f;
                    reusableData.lock_X_ValueParameter.TargetValue = 0f;
                    reusableData.lock_Y_ValueParameter.TargetValue = 0f;
                }
            }
        }

        public override void OnExit()
        {
            boss.applyFullRootMotion = false;
        }
    }
}
