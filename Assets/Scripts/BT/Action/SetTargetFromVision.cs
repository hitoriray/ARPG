using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BT.Actions;

namespace BT.Action
{
    [TaskCategory("Enemy/Boss")]
    public class SetTargetFromVision : BossActionBase
    {
        public SharedTransform SeenTarget;
        public SharedTransform Target;

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss() || SeenTarget.Value == null)
                return TaskStatus.Failure;

            Target.Value = SeenTarget.Value;
            boss.SetTarget(SeenTarget.Value);
            return TaskStatus.Success;
        }
    }
}
