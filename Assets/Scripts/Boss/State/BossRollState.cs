using Animancer;

namespace Boss
{
    public class BossRollState : BossEvasionState
    {
        private PlayerRollData data;

        public BossRollState(BossController boss) : base(boss)
        {
            data = boss.PlayerSO.playerMovementData.PlayerRollData;
        }

        protected override ClipTransition GetClip(EvasionDirection dir) => dir switch
        {
            EvasionDirection.Forward => data.rollForward,
            EvasionDirection.Backward => data.rollBackward,
            EvasionDirection.Left => data.rollLeft,
            EvasionDirection.Right => data.rollRight,
            _ => data.rollBackward
        };

        protected override float InvincibleDuration => data.invincibleDuration;
        protected override float CooldownTime => data.cooldown;
        protected override float ForwardThreshold => data.forwardThreshold;
        protected override float SideThreshold => data.sideThreshold;
    }
}
