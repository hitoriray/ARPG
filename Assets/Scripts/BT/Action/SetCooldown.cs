using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace BT.Actions
{
    [TaskCategory("Common")]
    public class SetCooldown : BehaviorDesigner.Runtime.Tasks.Action
    {
        public SharedFloat NextAllowedTime;
        public SharedFloat Cooldown;

        public override TaskStatus OnUpdate()
        {
            float cd = Cooldown.Value;
            if (cd <= 0f)
                cd = 1.0f;
            NextAllowedTime.Value = UnityEngine.Time.time + cd;
            return TaskStatus.Success;
        }
    }
}
