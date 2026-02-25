using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossHasTarget : Conditional
    {
        public SharedTransform Target;

        public override TaskStatus OnUpdate()
        {
            return Target.Value != null ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
