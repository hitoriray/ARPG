using Animancer;

/// <summary>
/// Q 键 = Roll 翻滚（Walk/Run 均可，大幅位移，最长无敌帧，紧急躲避大招）
/// </summary>
public class PlayerRollState : PlayerEvasionState
{
    private PlayerRollData data;

    public PlayerRollState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        data = playerSO.playerMovementData.PlayerRollData;
    }

    protected override ClipTransition GetClip(EvasionDirection dir) => dir switch
    {
        EvasionDirection.Forward  => data.rollForward,
        EvasionDirection.Backward => data.rollBackward,
        EvasionDirection.Left     => data.rollLeft,
        EvasionDirection.Right    => data.rollRight,
        _                         => data.rollBackward
    };

    protected override float InvincibleDuration => data.invincibleDuration;
    protected override float CooldownTime       => data.cooldown;
    protected override float ForwardThreshold   => data.forwardThreshold;
    protected override float SideThreshold      => data.sideThreshold;
}
