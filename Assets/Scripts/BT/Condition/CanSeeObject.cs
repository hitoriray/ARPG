using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Conditions
{
    [TaskCategory("Common")]
    public class CanSeeObject : Conditional
    {
        public SharedTransform Target;
        public SharedFloat ViewDistance;
        public SharedFloat ViewAngle;
        public SharedFloat EyeHeight;
        public SharedLayerMask ObstacleMask;
        public SharedBool RequireLineOfSight;

        public override TaskStatus OnUpdate()
        {
            if (Target.Value == null)
                return TaskStatus.Failure;

            Vector3 origin = transform.position + Vector3.up * EyeHeight.Value;
            Vector3 toTarget = Target.Value.position - origin;
            float dist = toTarget.magnitude;

            if (ViewDistance.Value > 0f && dist > ViewDistance.Value)
                return TaskStatus.Failure;

            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            if (ViewAngle.Value > 0f && angle > ViewAngle.Value * 0.5f)
                return TaskStatus.Failure;

            if (RequireLineOfSight.Value)
            {
                if (Physics.Raycast(origin, toTarget.normalized, out var hit, dist, ObstacleMask.Value))
                {
                    if (hit.transform != Target.Value)
                        return TaskStatus.Failure;
                }
            }

            return TaskStatus.Success;
        }
    }
}
