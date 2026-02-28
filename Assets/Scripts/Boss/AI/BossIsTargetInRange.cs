using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossIsTargetInRange : Conditional
    {
        public SharedTransform Target;
        public SharedFloat Range;

        public override TaskStatus OnUpdate()
        {
            float range = Range.Value;

            if (Target.Value != null)
            {
                float dist = UnityEngine.Vector3.Distance(transform.position, Target.Value.position);
                return dist <= range ? TaskStatus.Success : TaskStatus.Failure;
            }
            
            return TaskStatus.Failure;
        }
    }
}
