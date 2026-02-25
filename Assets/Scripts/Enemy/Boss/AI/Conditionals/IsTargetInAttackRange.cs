using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Enemy.Boss.AI.Conditionals
{
    [TaskCategory("Boss")]
    [TaskDescription("判断指定的 Target Transform 是否在 BossAttackRange (攻击范围) 内。")]
    public class IsTargetInAttackRange : Conditional
    {
        public SharedTransform target;
        public SharedFloat attackRange = 3f;

        public override TaskStatus OnUpdate()
        {
            if (target.Value == null)
            {
                return TaskStatus.Failure;
            }

            // 比较当前的距离和设定的攻击范围
            float sqrDistance = (transform.position - target.Value.position).sqrMagnitude;
            if (sqrDistance <= attackRange.Value * attackRange.Value)
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
}
