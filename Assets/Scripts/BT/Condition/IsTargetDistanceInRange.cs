using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Conditions
{
    [TaskCategory("Common")]
    public class IsTargetDistanceInRange : Conditional
    {
        public SharedTransform Target;
        public SharedFloat MinDistance; // >=0
        public SharedFloat MaxDistance; // <=0 means ignore

        public override TaskStatus OnUpdate()
        {
            if (Target.Value == null)
                return TaskStatus.Failure;

            float dist = Vector3.Distance(transform.position, Target.Value.position);
            float min = Mathf.Max(0f, MinDistance.Value);
            float max = MaxDistance.Value;
            // RayDebug.Log(dist.ToString());

            if (dist < min)
                return TaskStatus.Failure;
            if (max > 0f && dist > max)
                return TaskStatus.Failure;

            return TaskStatus.Success;
        }
    }
}
