using UnityEngine;
using Animancer;

namespace Enemy.Boss.State
{
    public class BossStrafeState : BossStateBase
    {
        private float strafeSpeed = 2.0f;
        private float rotateSpeed = 10f;

        // 立回方向：1为向右，-1为向左
        public int strafeDirection = 1;

        public BossStrafeState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            if (bossMotionSource != null && bossMotionSource.playerMovementData != null)
            {
                // 可以复用玩家锁定行走时的动画，这里为了简化演示，采用移动Loop或专用的StrafeClip
                var moveData = bossMotionSource.playerMovementData.PlayerMoveLoopData;
                animancer.Play(moveData.moveLoop);
            }

            // 每次进入立回状态时，随机决定向左还是向右绕圈
            strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        public override void OnUpdate()
        {
            if (boss.Target != null)
            {
                // 时刻保持盯住玩家（二人转）
                Vector3 lookDir = boss.Target.position - boss.transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
                }
            }
        }

        public override void OnAnimationUpdate()
        {
            if (boss.Target != null)
            {
                // 沿着自身的右侧（或左侧）进行平移
                Vector3 strafeVector = boss.transform.right * strafeDirection * strafeSpeed * Time.deltaTime;
                boss.UpdateCharacterMove(strafeVector, Quaternion.identity);
            }
        }

        public override void OnExit()
        {
        }

        public override void OnAnimationEnd()
        {
        }
    }
}
