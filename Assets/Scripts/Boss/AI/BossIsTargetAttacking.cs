using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using RayPlayer;

namespace Boss.AI
{
    [TaskCategory("Boss")]
    public class BossIsTargetAttacking : Conditional
    {
        public SharedTransform Target;
        public SharedFloat ReadWindow; // normalized time window [0..1], default 0.35
        public SharedFloat DisableDodgeRange; // <=0 means ignore

        private BossController boss;

        public override void OnStart()
        {
            boss = GetComponent<BossController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (Target.Value == null)
                return TaskStatus.Failure;

            if (boss == null)
                return TaskStatus.Failure;

            float disableRange = DisableDodgeRange.Value;
            if (disableRange > 0f)
            {
                float dist = UnityEngine.Vector3.Distance(transform.position, Target.Value.position);
                if (dist <= disableRange)
                    return TaskStatus.Failure;
            }

            // Boss 正在出手/闪避/硬直时，不再触发“读指令闪避”
            var sm = boss.MovementStateMachine;
            if (sm != null)
            {
                if (sm.currentState == sm.skillState ||
                    sm.currentState == sm.avoidState ||
                    sm.currentState == sm.slideState ||
                    sm.currentState == sm.rollState ||
                    sm.currentState == sm.hitState ||
                    sm.currentState == sm.deadState)
                {
                    return TaskStatus.Failure;
                }
            }

            var player = Target.Value.GetComponentInParent<PlayerController>();
            if (player == null || player.MovementStateMachine == null)
                return TaskStatus.Failure;

            bool isSkill = player.MovementStateMachine.currentState == player.MovementStateMachine.skillState;
            if (!isSkill)
            {
                // 兜底：技能层权重 > 0 也视为攻击中
                if (player.SkillLayer != null && player.SkillLayer.Weight > 0.05f)
                    isSkill = true;
            }

            if (!isSkill)
                return TaskStatus.Failure;

            float window = ReadWindow.Value;
            if (window <= 0f)
                window = 0.35f;

            var state = player.SkillLayer != null ? player.SkillLayer.CurrentState : null;
            if (state != null)
            {
                float t = state.NormalizedTime;
                return t <= window ? TaskStatus.Success : TaskStatus.Failure;
            }

            return TaskStatus.Failure;
        }
    }
}
