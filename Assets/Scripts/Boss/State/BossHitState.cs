namespace Boss
{
    public class BossHitState : BossStateBase
    {
        public BossHitState(BossController boss) : base(boss) { }

        public override void OnEnter()
        {
            boss.ClearDesiredMove();
        }
    }
}
