using BehaviorDesigner.Runtime.Tasks;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class InterruptCurrent : BossActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            boss.ClearDesiredMove();
            boss.Change2IdleState();
            // TODO: 若需要强制打断技能/霸体，可在BossController补充接口
            return TaskStatus.Success;
        }
    }
}
