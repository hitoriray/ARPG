using Animancer;
using UnityEngine;

/// <summary>
/// 玩家受伤状态 — 4方向受伤动画 + 硬直
/// 进入时根据 reusableData.lastHitDirection 选择方向动画播放
/// 动画结束后自动回到 Idle
/// </summary>
public class PlayerHurtState : PlayerMovementState
{
    private HurtData hurtData;
    private float hurtTimer;
    private bool animationEnded;

    public PlayerHurtState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        hurtData = playerSO.playerMovementData.PlayerHurtData;
    }

    public override void OnEnter()
    {
        // 不调用 base.OnEnter()，受伤期间不注册输入事件
        animationEnded = false;
        hurtTimer = 0f;

        // 计算受击方向（世界空间→本地空间）
        Vector3 hitDirWorld = reusableData.lastHitDirection;
        Vector3 localDir = player.transform.InverseTransformDirection(hitDirWorld);

        // 选择对应方向的动画
        ClipTransition clip = hurtData != null ? hurtData.GetClipByDirection(localDir) : null;

        if (clip != null && clip.Clip != null)
        {
            var state = animancer.Play(clip);
            state.Events(this).OnEnd ??= OnHurtAnimationEnd;
        }
        else
        {
            // 没有配置受伤动画，短暂延迟后回 Idle
            RayDebug.Warn("[PlayerHurtState] 缺少受伤动画配置，直接回 Idle");
            animationEnded = true;
        }
    }

    public override void OnUpdate()
    {
        // 受伤硬直期间不响应移动输入
        // 但如果再次被 Hit，会在 PlayerController.OnHit 中重新进入 HurtState
        hurtTimer += Time.deltaTime;

        if (animationEnded)
        {
            playerStateMachine.ChangeState(playerStateMachine.idleState);
        }
    }

    public override void OnExit()
    {
        // 不调用 base.OnExit()（因为也没调用 base.OnEnter()）
        animationEnded = false;
        hurtTimer = 0f;
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
