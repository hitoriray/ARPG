using BehaviorDesigner.Runtime.Tasks;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class HoldGroundOrBlock : BossActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            boss.ClearDesiredMove();
            boss.AI.FaceTarget = true;
            // TODO: 如需举盾/格挡，可在这里触发对应技能或动画
            return TaskStatus.Success;
        }
    }
}
