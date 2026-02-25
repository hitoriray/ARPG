using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class ChangeTarget : BossActionBase
    {
        public SharedTransform NewTarget;

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            boss.SetTarget(NewTarget.Value);
            return NewTarget.Value != null ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
