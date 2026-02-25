using UnityEngine;
using Animancer;

namespace Enemy.Boss.State
{
    public class BossAttackState : BossStateBase
    {
        public bool isAttacking = false;

        public BossAttackState(BossStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            isAttacking = true;
            // 挑选个攻击动画，若套用Player配置则找具体的clip
            // 假设 bossMotionSource 里有 attack1Clip TODO: 需替换为工程中真实的动作字段
            if (bossMotionSource != null && bossMotionSource.playerMovementData != null)
            {
                // animancer.Play(bossMotionSource.playerMovementData.AttackClip);
                // 仅作为占位符，等同于 animancer.Play(clip);
            }
        }

        public override void OnUpdate()
        {
        }

        public override void OnAnimationUpdate()
        {
            // 若开启了 RootMotion，使用默认的原地应用
            // boss.animator.ApplyBuiltinRootMotion();
        }

        public override void OnAnimationEnd()
        {
            // 动画播放完毕时，将自身设置为非攻击状态，Behaviour Designer 的 Conditional 判断可解除拦截
            isAttacking = false;
        }

        public override void OnExit()
        {
            isAttacking = false;
        }
    }
}
