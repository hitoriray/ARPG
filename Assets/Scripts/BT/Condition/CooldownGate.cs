using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Conditions
{
    [TaskCategory("Common")]
    public class CooldownGate : Conditional
    {
        public SharedFloat NextAllowedTime;

        public override TaskStatus OnUpdate()
        {
            return Time.time >= NextAllowedTime.Value ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
