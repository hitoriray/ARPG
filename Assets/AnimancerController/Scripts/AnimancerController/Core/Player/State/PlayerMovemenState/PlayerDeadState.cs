using Animancer;
using UnityEngine;

/// <summary>
/// 玩家死亡状态
/// 进入后播放死亡动画，禁止所有输入，动画结束后回调 OnDeathAnimationEnd
/// </summary>
public class PlayerDeadState : PlayerMovementState
{
    // 死亡专用动画 clip（在 PlayerMovementData 中配置）
    // 若未配置则使用 hurtFront 兜底
    private ClipTransition deathClip;

    public PlayerDeadState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        deathClip = playerSO?.playerMovementData?.DeathClip;
    }

    public override void OnEnter()
    {
        // 死亡状态不调用 base.OnEnter() — 不注册任何输入事件
        // 停掉所有技能
        player.ExitSkillMode();

        // 播放死亡动画
        if (deathClip != null && deathClip.Clip != null)
        {
            var state = animancer.Play(deathClip);
            // 死亡动画播完后停在最后一帧，不循环
            state.Events(this).OnEnd ??= OnDeathAnimationEnd;
        }
        else
        {
            RayDebug.Warn("[PlayerDeadState] 未配置死亡动画，直接触发 OnDeathAnimationEnd");
            OnDeathAnimationEnd();
        }

        RayDebug.Info("[PlayerDeadState] 玩家死亡！");
    }

    public override void OnUpdate()
    {
    }

    public override void OnExit()
    {
    }

    public override void OnAnimationEnd() => OnDeathAnimationEnd();

    private void OnDeathAnimationEnd()
    {
        RayDebug.Info("[PlayerDeadState] 死亡动画结束");
        player.NotifyDeathAnimationFinished();
    }
}
