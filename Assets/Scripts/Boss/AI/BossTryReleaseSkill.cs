using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossTryReleaseSkill : Action
    {
        public SharedInt SkillIndex;

        private BossController boss;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null || boss.IsDead)
                return TaskStatus.Failure;

            return boss.TryStartSkill(SkillIndex.Value) ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
