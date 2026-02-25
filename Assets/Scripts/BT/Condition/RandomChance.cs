using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace BT.Conditions
{
    [TaskCategory("Common")]
    public class RandomChance : Conditional
    {
        [Range(0f, 1f)] public SharedFloat Probability;

        public override TaskStatus OnUpdate()
        {
            float p = Mathf.Clamp01(Probability.Value);
            return Random.value <= p ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
