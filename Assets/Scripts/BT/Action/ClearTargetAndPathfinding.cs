using BehaviorDesigner.Runtime.Tasks;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class ClearTargetAndPathfinding : BossActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            boss.SetTarget(null);
            boss.ClearDesiredMove();
            boss.AI.FaceTarget = false;
            // TODO: 如果后续接入NavMesh/寻路系统，在这里清空路径
            return TaskStatus.Success;
        }
    }
}
