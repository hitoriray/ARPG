using BehaviorDesigner.Runtime.Tasks;
using Boss;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class BackstepTransition : BossActionBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            boss.SetEvasionDir(-boss.transform.forward);
            return boss.TryStartEvasion(BossEvasionType.Avoid) ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
