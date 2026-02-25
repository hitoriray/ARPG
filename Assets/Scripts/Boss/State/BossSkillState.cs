using UnityEngine;

namespace Boss
{
    public class BossSkillState : BossStateBase
    {
        private readonly BossStateMachine stateMachine;
        private bool isPendingExit;

        public BossSkillState(BossController boss, BossStateMachine stateMachine) : base(boss)
        {
            this.stateMachine = stateMachine;
        }

        public override void OnEnter()
        {
            isPendingExit = false;
            boss.EnterSkillMode(false);
        }

        public override void OnUpdate()
        {
            if (isPendingExit)
                return;

            if (boss.AI.FaceTarget && boss.AI.Target != null)
            {
                Vector3 dir = boss.AI.Target.position - boss.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, Quaternion.LookRotation(dir),
                        Time.deltaTime * boss.RotateSpeed);
                }
            }
        }

        public override void OnExit()
        {
            boss.ExitSkillMode();
        }

        public void NotifySkillEnd()
        {
            if (isPendingExit)
                return;

            isPendingExit = true;
            boss.ExitSkillMode();

            if (boss.AI.HasMove)
                stateMachine.ChangeState(stateMachine.moveState);
            else
                stateMachine.ChangeState(stateMachine.idleState);
        }
    }
}
