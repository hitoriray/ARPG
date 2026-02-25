using UnityEngine;
using Animancer;

namespace Enemy.Boss.State
{
    public class BossEvasionState : BossStateBase
    {
        private AnimancerState evasionState;
        
        public bool isEvading = false;
        
        // BD 可以赋予逃避方向 1=左， 2=右， 3=后
        public int evadeDirection = 3; 

        public BossEvasionState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            isEvading = true;
            if (bossMotionSource != null && bossMotionSource.playerMovementData != null)
            {
                var avoidData = bossMotionSource.playerMovementData.PlayerAvoidData;
                ClipTransition clipToPlay = avoidData.avoidBackward; // 默认向后
                
                // 根据外部指令（可能由 BD 修改）决定播放什么回避动作
                if (evadeDirection == 1) clipToPlay = avoidData.avoidLeft;
                else if (evadeDirection == 2) clipToPlay = avoidData.avoidRight;
                
                if (clipToPlay != null && clipToPlay.Clip != null)
                {
                    evasionState = animancer.Play(clipToPlay);
                    evasionState.Events(boss).OnEnd = OnEvasionComplete;
                }
                else
                {
                    // 没有配置回避动作则立刻返回
                    OnEvasionComplete();
                }
            }
        }

        public override void OnUpdate()
        {
            // 回避期间自身朝向可以保持不变或轻微锁定玩家，这里选用不更新朝向（保持撤出时的姿态）
        }

        public override void OnAnimationUpdate()
        {
            if (isEvading)
            {
                 // 从避免动作的原生动画中提取 RootMotion 位移
                 boss.UpdateCharacterMove(boss.animator.deltaPosition, boss.animator.deltaRotation);
            }
        }

        private void OnEvasionComplete()
        {
            isEvading = false;
        }

        public override void OnExit()
        {
            isEvading = false;
        }

        public override void OnAnimationEnd()
        {
            isEvading = false;
        }
    }
}
