using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossSetTarget : Action
    {
        public SharedTransform Target;

        private BossController boss;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null)
                return TaskStatus.Failure;

            boss.SetTarget(Target.Value);
            return Target.Value != null ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
