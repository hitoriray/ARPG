using Animancer;

namespace Boss
{
    public abstract class BossStateBase : IState
    {
        protected readonly BossController boss;
        protected readonly AnimancerComponent animancer;
        protected readonly PlayerReusableData reusableData;
        protected readonly PlayerSO playerSO;

        protected BossStateBase(BossController boss)
        {
            this.boss = boss;
            animancer = boss.Animancer;
            reusableData = boss.ReusableData;
            playerSO = boss != null ? boss.PlayerSO : null;
        }

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnAnimationUpdate() { }
        public virtual void OnExit() { }
        public virtual void OnAnimationEnd() { }
    }
}
