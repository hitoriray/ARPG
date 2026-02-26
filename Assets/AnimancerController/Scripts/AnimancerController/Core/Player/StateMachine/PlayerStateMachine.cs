using RayPlayer;

/// <summary>
/// 有限状态机，缓存状态，驱动更新状态类
/// </summary>
public class PlayerStateMachine : StateMachineBase
{
    //缓存状态
    public PlayerController player;
    public PlayerIdleState idleState;
    public PlayerMoveStartState moveStartState;
    public PlayerMoveLoopState moveLoopState;
    public PlayerMoveEndState moveEndState;
    public PlayerJumpState jumpState;
    public PlayerClimbState climbState;
    public PlayerLedgeClimbState ledgeClimbState;
    public PlayerMoveToWallState moveWallState;
    public PlayerFallLoopState fallLoopState;
    public PlayerPlatformerUpState platformerUpState;
    public PlayerLandState landState;

    // 闪避三层状态
    public PlayerAvoidState avoidState;   // Walk + Shift
    public PlayerSlideState slideState;   // Run  + Shift
    public PlayerRollState  rollState;    // Q 键（无论 Walk/Run）

    // 战斗状态
    public PlayerSkillState skillState;   // 全身技能（Layer1）

    // 受伤状态
    public PlayerHurtState hurtState;

    public PlayerStateMachine(PlayerController player)
    {
        this.player = player;
        idleState = new PlayerIdleState(this);
        moveStartState = new PlayerMoveStartState(this);
        moveLoopState = new PlayerMoveLoopState(this);
        moveEndState = new PlayerMoveEndState(this);
        jumpState = new PlayerJumpState(this);
        climbState = new PlayerClimbState(this);
        ledgeClimbState = new PlayerLedgeClimbState(this);
        moveWallState = new PlayerMoveToWallState(this);
        fallLoopState = new PlayerFallLoopState(this);
        platformerUpState = new PlayerPlatformerUpState(this);
        landState = new PlayerLandState(this);

        avoidState = new PlayerAvoidState(this);
        slideState = new PlayerSlideState(this);
        rollState  = new PlayerRollState(this);
        skillState = new PlayerSkillState(this);
        hurtState  = new PlayerHurtState(this);
    }

    public override void ChangeState(IState targetState)
    {
        base.ChangeState(targetState);
        player.ReusableData.currentState.Value = targetState.GetType().Name;
    }
}