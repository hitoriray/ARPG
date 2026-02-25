using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace BT.Actions
{
    public abstract class BossSkillActionBase : BossActionBase
    {
        public SharedInt SkillIndex;
        public SharedBool WaitForEnd;

        private bool started;

        public override void OnStart()
        {
            base.OnStart();
            started = false;
        }

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            if (!started)
            {
                if (!boss.TryStartSkill(SkillIndex.Value))
                    return TaskStatus.Failure;
                started = true;
                if (!WaitForEnd.Value)
                    return TaskStatus.Success;
            }

            return boss.IsInSkill ? TaskStatus.Running : TaskStatus.Success;
        }

        public override void OnEnd()
        {
            started = false;
        }
    }
}
