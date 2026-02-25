using Animancer;

namespace Boss
{
    public class BossAvoidState : BossEvasionState
    {
        private PlayerAvoidData data;

        public BossAvoidState(BossController boss) : base(boss)
        {
            data = boss.PlayerSO.playerMovementData.PlayerAvoidData;
        }

        protected override ClipTransition GetClip(EvasionDirection dir) => dir switch
        {
            EvasionDirection.Forward => data.avoidForward,
            EvasionDirection.Backward => data.avoidBackward,
            EvasionDirection.Left => data.avoidLeft,
            EvasionDirection.Right => data.avoidRight,
            _ => data.avoidBackward
        };

        protected override float InvincibleDuration => data.invincibleDuration;
        protected override float CooldownTime => data.cooldown;
        protected override float ForwardThreshold => data.forwardThreshold;
        protected override float SideThreshold => data.sideThreshold;
    }
}
