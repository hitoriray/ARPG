using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossSetEvasionDirection : Action
    {
        public SharedTransform Target;
        public BossEvasionDirMode Direction = BossEvasionDirMode.AwayFromTarget;

        private BossController boss;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (boss == null)
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
            boss.SetEvasionDir(dir.normalized);
            return TaskStatus.Success;
        }
    }
}
