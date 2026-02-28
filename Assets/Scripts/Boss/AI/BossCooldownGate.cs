using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossCooldownGate : Conditional
    {
        public SharedFloat NextAllowedTime;

        public override TaskStatus OnUpdate()
        {
            return Time.time >= NextAllowedTime.Value ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
