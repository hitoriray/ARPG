using Animancer;

/// <summary>
/// Run 状态 + Shift = Slide 滑步（大位移4方向穿插，无无敌帧，进攻性）
/// </summary>
public class PlayerSlideState : PlayerEvasionState
{
    private PlayerSlideData data;

    public PlayerSlideState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        data = playerSO.playerMovementData.PlayerSlideData;
    }

    protected override ClipTransition GetClip(EvasionDirection dir) => dir switch
    {
        EvasionDirection.Forward  => data.slideForward,
        EvasionDirection.Backward => data.slideBackward,
        EvasionDirection.Left     => data.slideLeft,
        EvasionDirection.Right    => data.slideRight,
        _                         => data.slideBackward
    };

    protected override float InvincibleDuration => data.invincibleDuration;
    protected override float CooldownTime       => data.cooldown;
    protected override float ForwardThreshold   => data.forwardThreshold;
    protected override float SideThreshold      => data.sideThreshold;
}
