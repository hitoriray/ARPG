namespace Boss
{
    public class BossStateMachine : StateMachineBase
    {
        public readonly BossController boss;

        public BossIdleState idleState;
        public BossMoveState moveState;
        public BossSkillState skillState;
        public BossAvoidState avoidState;
        public BossSlideState slideState;
        public BossRollState rollState;
        public BossHitState hitState;
        public BossDeadState deadState;

        public BossStateMachine(BossController boss)
        {
            this.boss = boss;
            idleState = new BossIdleState(boss);
            moveState = new BossMoveState(boss);
            skillState = new BossSkillState(boss, this);
            avoidState = new BossAvoidState(boss);
            slideState = new BossSlideState(boss);
            rollState = new BossRollState(boss);
            hitState = new BossHitState(boss);
            deadState = new BossDeadState(boss);
        }

        public override void ChangeState(IState targetState)
        {
            base.ChangeState(targetState);
            if (boss.ReusableData != null)
                boss.ReusableData.currentState.Value = targetState?.GetType().Name;
        }

        public void TickAI()
        {
            if (currentState == skillState || currentState == avoidState || currentState == slideState ||
                currentState == rollState || currentState == hitState || currentState == deadState)
                return;

            if (boss.AI.HasMove)
            {
                if (currentState != moveState)
                    ChangeState(moveState);
            }
            else
            {
                if (currentState != idleState)
                    ChangeState(idleState);
            }
        }
    }
}
