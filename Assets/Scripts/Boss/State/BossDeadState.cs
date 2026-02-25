namespace Boss
{
    public class BossDeadState : BossStateBase
    {
        public BossDeadState(BossController boss) : base(boss) { }

        public override void OnEnter()
        {
            boss.ClearDesiredMove();
            boss.disableRootMotion = true;
        }
    }
}
