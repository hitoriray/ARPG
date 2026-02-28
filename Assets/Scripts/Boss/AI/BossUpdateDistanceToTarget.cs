using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossUpdateDistanceToTarget : Action
    {
        public SharedTransform Target;
        public SharedFloat Distance;

        public override TaskStatus OnUpdate()
        {
            if (Target.Value == null)
                return TaskStatus.Failure;

            Distance.Value = Vector3.Distance(transform.position, Target.Value.position);
            return TaskStatus.Success;
        }
    }
}
