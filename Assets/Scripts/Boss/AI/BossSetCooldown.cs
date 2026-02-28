using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossSetCooldown : Action
    {
        public SharedFloat NextAllowedTime;
        public SharedFloat Cooldown;

        public override TaskStatus OnUpdate()
        {
            float cd = Cooldown.Value;
            if (cd <= 0f)
                cd = 1.0f;
            NextAllowedTime.Value = Time.time + cd;
            return TaskStatus.Success;
        }
    }
}
