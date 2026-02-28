using Animancer;
using UnityEngine;

namespace Boss
{
    public class BossDeadState : BossStateBase
    {
        private Animancer.ClipTransition deathClip;

        public BossDeadState(BossController boss) : base(boss)
        {
            deathClip = playerSO?.playerMovementData?.DeathClip;
        }

        public override void OnEnter()
        {
            boss.ClearDesiredMove();
            boss.disableRootMotion = true;

            // 通知 SpawnPoint 敌人已死亡（触发刷新倒计时）
            boss.GetComponent<Enemy.EnemyDeathListener>()?.NotifyDied();

            if (deathClip != null && deathClip.Clip != null)
            {
                var state = animancer.Play(deathClip);
                state.Events(this).OnEnd ??= OnDeathAnimationEnd;
            }
            else
            {
                RayDebug.Warn("[BossDeadState] 未配置死亡动画");
                OnDeathAnimationEnd();
            }

            RayDebug.Info("[BossDeadState] Boss 死亡！");
        }

        public override void OnUpdate() { }  // 禁止 AI 驱动

        public override void OnExit() { }

        public override void OnAnimationEnd() => OnDeathAnimationEnd();

        private void OnDeathAnimationEnd()
        {
            // 延迟 2 秒后销毁 Boss（方便看到死亡姿势）
            Object.Destroy(boss.gameObject, 2f);
        }
    }
}
