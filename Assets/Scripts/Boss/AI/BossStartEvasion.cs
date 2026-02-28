using BehaviorDesigner.Runtime.Tasks;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossStartEvasion : Action
    {
        public BossEvasionType EvasionType = BossEvasionType.Avoid;

        private BossController boss;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null)
                return TaskStatus.Failure;

            return boss.TryStartEvasion(EvasionType) ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
