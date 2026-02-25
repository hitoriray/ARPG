using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Enemy.Boss.AI.Actions
{
    [TaskCategory("Boss")]
    [TaskDescription("持续计算 Boss 到 Target 的距离，并存入共享变量。此节点始终返回 Success，适合放在 Parallel 或 Sequence 的第一个子节点。")]
    public class UpdateDistanceToTarget : Action
    {
        [Tooltip("目标玩家的 Transform")]
        public SharedTransform target;

        [Tooltip("输出结果：保存计算出的距离变量")]
        public SharedFloat storeDistance;

        public override TaskStatus OnUpdate()
        {
            if (target.Value == null)
            {
                return TaskStatus.Failure;
            }

            float distance = UnityEngine.Vector3.Distance(transform.position, target.Value.position);
            storeDistance.Value = distance;

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            target = null;
            storeDistance = 0f;
        }
    }
}
