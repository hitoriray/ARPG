using Animancer;

/// <summary>
/// Walk 状态 + Shift = Avoid 闪身（小幅4方向回避，短无敌帧）
/// </summary>
public class PlayerAvoidState : PlayerEvasionState
{
    private PlayerAvoidData data;

    public PlayerAvoidState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        data = playerSO.playerMovementData.PlayerAvoidData;
    }

    protected override ClipTransition GetClip(EvasionDirection dir) => dir switch
    {
        EvasionDirection.Forward  => data.avoidForward,
        EvasionDirection.Backward => data.avoidBackward,
        EvasionDirection.Left     => data.avoidLeft,
        EvasionDirection.Right    => data.avoidRight,
        _                         => data.avoidBackward
    };

    protected override float InvincibleDuration => data.invincibleDuration;
    protected override float CooldownTime       => data.cooldown;
    protected override float ForwardThreshold   => data.forwardThreshold;
    protected override float SideThreshold      => data.sideThreshold;
}
