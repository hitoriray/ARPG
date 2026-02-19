using Animancer;
using UnityEngine;

/// <summary>
/// 闪避动作抽象基类，Avoid / Slide / Roll 三种状态共用此逻辑
/// 子类只需提供：动画配置、无敌帧时长、冷却时间、方向阈值
/// </summary>
public abstract class PlayerEvasionState : PlayerMovementState
{
    protected enum EvasionDirection { Forward, Backward, Left, Right }

    private int invincibleTimerId = -1;

    // ── 子类必须实现的接口 ─────────────────────────────────────────────
    protected abstract ClipTransition GetClip(EvasionDirection dir);
    protected abstract float InvincibleDuration { get; }    // 无敌帧时长，0=无无敌帧
    protected abstract float CooldownTime { get; }           // 动作冷却
    protected abstract float ForwardThreshold { get; }       // 向前判定阈值（锁敌时用）
    protected abstract float SideThreshold { get; }          // 侧向判定阈值（锁敌时用）

    protected PlayerEvasionState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    // ── 生命周期 ───────────────────────────────────────────────────────
    public override void OnEnter()
    {
        base.OnEnter();

        // 冷却检查（所有闪避动作共享同一个冷却时间戳）
        if (Time.time - reusableData.lastEvasiveActionTime < CooldownTime)
        {
            ReturnToPreviousState();
            return;
        }

        reusableData.lastEvasiveActionTime = Time.time;

        EvasionDirection dir = DetermineDirection();
        PlayEvasionAnimation(dir);

        // 无敌帧
        if (InvincibleDuration > 0)
        {
            reusableData.isInvincible = true;
            invincibleTimerId = timerServer.AddTimer((int)(InvincibleDuration * 1000), OnInvincibleEnd);
        }
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        // 闪避期间硬直，不做额外逻辑
    }

    public override void OnExit()
    {
        base.OnExit();

        reusableData.isInvincible = false;

        if (invincibleTimerId != -1)
        {
            timerServer.RemoveTimer(invincibleTimerId);
            invincibleTimerId = -1;
        }
    }

    public override void OnAnimationEnd() { }
    public override void OnAnimationUpdate() { }

    // 闪避期间硬直，不响应其他输入
    protected override void AddEventListening() { base.AddEventListening(); }
    protected override void RemoveEventListening() { base.RemoveEventListening(); }

    // ── 内部逻辑 ───────────────────────────────────────────────────────

    /// <summary>
    /// 根据锁敌状态和输入方向判断闪避方向
    /// 锁敌时: 使用实际输入方向（4方向）
    /// 非锁敌时: 有输入=前（角色朝向已跟随输入旋转），无输入=后
    /// </summary>
    private EvasionDirection DetermineDirection()
    {
        Vector2 input = inputServer.Move;
        bool isLockedOn = reusableData.lockValueParameter.CurrentValue >= 0.5f;

        if (isLockedOn)
        {
            if (Mathf.Abs(input.x) > SideThreshold)
                return input.x > 0 ? EvasionDirection.Right : EvasionDirection.Left;
            if (input.y > ForwardThreshold)
                return EvasionDirection.Forward;
            return EvasionDirection.Backward;
        }
        else
        {
            return input != Vector2.zero ? EvasionDirection.Forward : EvasionDirection.Backward;
        }
    }

    private void PlayEvasionAnimation(EvasionDirection dir)
    {
        ClipTransition clip = GetClip(dir);

        if (clip != null && clip.Clip != null)
        {
            animancer.Play(clip).Events(player).OnEnd = OnEvasionComplete;
        }
        else
        {
            RayDebug.Error($"{GetType().Name} 动画未配置：{dir}");
            ReturnToPreviousState();
        }
    }

    private void OnInvincibleEnd()
    {
        reusableData.isInvincible = false;
        invincibleTimerId = -1;
    }

    private void OnEvasionComplete()
    {
        ReturnToPreviousState();
    }

    /// <summary>
    /// 闪避结束后返回前置状态（moveLoop 或 idle，不走 moveStart）
    /// </summary>
    private void ReturnToPreviousState()
    {
        if (inputServer.Move != Vector2.zero)
            playerStateMachine.ChangeState(playerStateMachine.moveLoopState);
        else
            playerStateMachine.ChangeState(playerStateMachine.idleState);
    }
}
