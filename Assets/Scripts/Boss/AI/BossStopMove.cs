using BehaviorDesigner.Runtime.Tasks;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossStopMove : Action
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

            boss.ClearDesiredMove();
            return TaskStatus.Success;
        }
    }
}
