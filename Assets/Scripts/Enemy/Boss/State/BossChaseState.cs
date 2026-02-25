using UnityEngine;
using Animancer;

namespace Enemy.Boss.State
{
    public class BossChaseState : BossStateBase
    {
        private float runSpeed = 4.0f;
        private float rotateSpeed = 10f;
        private bool isStarting = false;
        private AnimancerState startState;

        public BossChaseState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            // 通过 Animancer 的 SmoothedFloatParameter 设置 Speed = 2（跑步）
            // 这和 Player 端 UpdateSpeed() 的机制完全一致
            if (boss.SpeedParameter != null)
                boss.SpeedParameter.TargetValue = 2f;

            if (bossMotionSource != null && bossMotionSource.playerMovementData != null)
            {
                var moveStartData = bossMotionSource.playerMovementData.PlayerMoveStartData;
                
                if (boss.Target != null)
                {
                    Vector3 lookDir = boss.Target.position - boss.transform.position;
                    lookDir.y = 0;
                    float targetAngle = Vector3.SignedAngle(boss.transform.forward, lookDir, Vector3.up);

                    // 8方向判定
                    if (targetAngle < 22.5f && targetAngle >= -22.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_F);
                    }
                    else if (targetAngle >= 22.5f && targetAngle < 67.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_R45);
                    }
                    else if (targetAngle >= 67.5f && targetAngle < 112.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_R90);
                    }
                    else if (targetAngle >= 112.5f && targetAngle < 157.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_R135);
                    }
                    else if (targetAngle >= 157.5f || targetAngle < -157.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_R180);
                    }
                    else if (targetAngle >= -157.5f && targetAngle < -112.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_L135);
                    }
                    else if (targetAngle >= -112.5f && targetAngle < -67.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_L90);
                    }
                    else if (targetAngle >= -67.5f && targetAngle < -22.5f)
                    {
                        startState = animancer.Play(moveStartData.moveStart_L45);
                    }

                    isStarting = true;
                    if (startState != null)
                    {
                        startState.Events(boss).OnEnd = OnMoveStartEnd;
                    }
                    else
                    {
                        OnMoveStartEnd();
                    }
                }
                else
                {
                    OnMoveStartEnd();
                }
            }
        }

        private void OnMoveStartEnd()
        {
            isStarting = false;
            if (bossMotionSource != null && bossMotionSource.playerMovementData != null)
            {
                animancer.Play(bossMotionSource.playerMovementData.PlayerMoveLoopData.moveLoop);
                // 播放 moveLoop 后再次确认 Speed 参数（让 BlendTree 处于跑步区间）
                if (boss.SpeedParameter != null)
                    boss.SpeedParameter.TargetValue = 2f;
            }
        }

        public override void OnUpdate()
        {
            if (boss.Target != null)
            {
                Vector3 lookDir = boss.Target.position - boss.transform.position;
                lookDir.y = 0;
                
                // 起步阶段使用动作提供的RootMotion；只有进入Loop循环后，才主动进行平滑跟踪旋转
                if (!isStarting && lookDir.sqrMagnitude > 0.1f)
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
                if (isStarting)
                {
                     // 手动应用起步动画自带的位移和转向（让转向更加自然，且符合RootMotion）
                     boss.UpdateCharacterMove(boss.animator.deltaPosition, boss.animator.deltaRotation);
                }
                else
                {
                     // Loop阶段如果是用自己的逻辑持续贴近：
                     boss.UpdateCharacterMove(boss.transform.forward * runSpeed * Time.deltaTime, Quaternion.identity);
                }
            }
        }

        public override void OnExit()
        {
            isStarting = false;
        }

        public override void OnAnimationEnd()
        {
        }
    }
}
