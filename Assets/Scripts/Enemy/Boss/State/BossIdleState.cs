using UnityEngine;
using Animancer;
using RayPlayer; 

namespace Enemy.Boss.State
{
    public class BossIdleState : BossStateBase
    {
        public BossIdleState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            // Debug.Log("Boss Enter Idle State");
            if (bossMotionSource != null && bossMotionSource.playerMovementData != null)
            {
                // 可以复用玩家的Idle配置，或者自己给Boss配置专有的
                animancer.Play(bossMotionSource.playerMovementData.PlayerIdleData.idle);
            }
        }

        public override void OnUpdate()
        {
            boss.verticalSpeed -= boss.gravity * Time.deltaTime; 
        }

        public override void OnAnimationUpdate()
        {
            // Idle 阶段如果没有特殊的 RootMotion 处理，则禁用根运动位移
            boss.UpdateCharacterMove(Vector3.zero, Quaternion.identity);
        }

        public override void OnExit()
        {
        }

        public override void OnAnimationEnd()
        {
        }
    }
}
