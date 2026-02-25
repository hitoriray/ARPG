using BehaviorDesigner.Runtime.Tasks;

namespace Enemy.Boss.AI.Actions
{
    [TaskCategory("Boss")]
    [TaskDescription("改变BossController的基础物理/动画状态。状态切换仅在任务开始时执行一次。")]
    public class ChangeBossState : Action
    {
        public BossStateType targetState;
        
        [Tooltip("true=切换状态后立刻返回Success（适合瞬发动作）;\nfalse=返回Running直到动画播完（适合攻击/闪避等需等待的动作）")]
        public bool returnSuccessImmediately = true;

        private BossController bossController;

        public override void OnAwake()
        {
            bossController = gameObject.GetComponent<BossController>();
        }

        /// <summary>
        /// 状态切换只在任务【第一次开始】时调用一次，避免每帧重复调用 OnEnter
        /// </summary>
        public override void OnStart()
        {
            if (bossController != null)
            {
                bossController.ChangeState(targetState);
            }
        }

        /// <summary>
        /// OnUpdate 只用来检查是否要结束、让出控制权给行为树重新判断
        /// 不再调用 ChangeState
        /// </summary>
        public override TaskStatus OnUpdate()
        {
            if (bossController == null)
            {
                return TaskStatus.Failure;
            }

            if (returnSuccessImmediately)
            {
                return TaskStatus.Success;
            }

            // 等待型：检查具体状态是否已经执行完毕
            if (targetState == BossStateType.Attack)
            {
                var attackObj = bossController.StateMachine.attackState;
                if (!attackObj.isAttacking) return TaskStatus.Success;
            }
            else if (targetState == BossStateType.Evasion)
            {
                var evadeObj = bossController.StateMachine.evasionState;
                if (!evadeObj.isEvading) return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }
    }
}
