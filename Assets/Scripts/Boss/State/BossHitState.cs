using Animancer;
using UnityEngine;

namespace Boss
{
    /// <summary>
    /// Boss受伤状态 — 4方向受伤动画 + 硬直
    /// </summary>
    public class BossHitState : BossStateBase
    {
        private HurtData hurtData;
        private bool animationEnded;

        public BossHitState(BossController boss) : base(boss)
        {
            hurtData = playerSO != null ? playerSO.playerMovementData?.PlayerHurtData : null;
        }

        public override void OnEnter()
        {
            boss.ClearDesiredMove();
            animationEnded = false;

            // 计算受击方向（世界空间→Boss本地空间）
            Vector3 hitDirWorld = boss.LastHitDirection;
            Vector3 localDir = boss.transform.InverseTransformDirection(hitDirWorld);

            // 选择对应方向的动画
            ClipTransition clip = hurtData != null ? hurtData.GetClipByDirection(localDir) : null;

            if (clip != null && clip.Clip != null)
            {
                var state = animancer.Play(clip);
                state.Events(this).OnEnd ??= OnHurtAnimationEnd;
            }
            else
            {
                RayDebug.Warn("[BossHitState] 缺少受伤动画配置，直接回 Idle");
                animationEnded = true;
            }
        }

        public override void OnUpdate()
        {
            // 硬直期间不响应 AI 指令
            if (animationEnded)
            {
                boss.MovementStateMachine.ChangeState(boss.MovementStateMachine.idleState);
            }
        }

        public override void OnExit()
        {
            animationEnded = false;
        }

        public override void OnAnimationEnd()
        {
            OnHurtAnimationEnd();
        }

        private void OnHurtAnimationEnd()
        {
            animationEnded = true;
        }
    }
}
