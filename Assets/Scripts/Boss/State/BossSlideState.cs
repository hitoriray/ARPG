using Animancer;

namespace Boss
{
    public class BossSlideState : BossEvasionState
    {
        private PlayerSlideData data;

        public BossSlideState(BossController boss) : base(boss)
        {
            data = boss.PlayerSO.playerMovementData.PlayerSlideData;
        }

        protected override ClipTransition GetClip(EvasionDirection dir) => dir switch
        {
            EvasionDirection.Forward => data.slideForward,
            EvasionDirection.Backward => data.slideBackward,
            EvasionDirection.Left => data.slideLeft,
            EvasionDirection.Right => data.slideRight,
            _ => data.slideBackward
        };

        protected override float InvincibleDuration => data.invincibleDuration;
        protected override float CooldownTime => data.cooldown;
        protected override float ForwardThreshold => data.forwardThreshold;
        protected override float SideThreshold => data.sideThreshold;
        protected override bool PreferSideStep => true;
    }
}
