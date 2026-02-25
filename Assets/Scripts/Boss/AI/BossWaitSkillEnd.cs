using BehaviorDesigner.Runtime.Tasks;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossWaitSkillEnd : Action
    {
        private BossController boss;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null)
                return TaskStatus.Failure;

            return boss.IsInSkill ? TaskStatus.Running : TaskStatus.Success;
        }
    }
}
