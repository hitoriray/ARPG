using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Boss;
using UnityEngine;

namespace BT.Actions
{
    [TaskCategory("Enemy/Boss")]
    public class BossEvasion : BossActionBase
    {
        public SharedTransform Target;
        public BossEvasionDirMode Direction = BossEvasionDirMode.AwayFromTarget;
        public BossEvasionType EvasionType = BossEvasionType.Avoid;

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            Vector3 dir = Vector3.zero;
            switch (Direction)
            {
                case BossEvasionDirMode.Forward:
                    dir = boss.transform.forward;
                    break;
                case BossEvasionDirMode.Backward:
                    dir = -boss.transform.forward;
                    break;
                case BossEvasionDirMode.Left:
                    dir = -boss.transform.right;
                    break;
                case BossEvasionDirMode.Right:
                    dir = boss.transform.right;
                    break;
                case BossEvasionDirMode.TowardTarget:
                    if (Target.Value == null) return TaskStatus.Failure;
                    dir = Target.Value.position - boss.transform.position;
                    break;
                case BossEvasionDirMode.AwayFromTarget:
                    if (Target.Value == null) return TaskStatus.Failure;
                    dir = boss.transform.position - Target.Value.position;
                    break;
                case BossEvasionDirMode.RandomSide:
                    dir = Random.value < 0.5f ? -boss.transform.right : boss.transform.right;
                    break;
            }

            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f)
                return TaskStatus.Failure;

            boss.SetEvasionDir(dir.normalized);
            return boss.TryStartEvasion(EvasionType) ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
