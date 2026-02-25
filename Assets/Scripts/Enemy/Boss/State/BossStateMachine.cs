using System;
using UnityEngine;
using Animancer;

namespace Enemy.Boss.State
{
    /// <summary>
    /// Boss专属状态机，负责缓存并切变Boss的状态
    /// </summary>
    public class BossStateMachine : StateMachineBase
    {
        public BossController bossController;

        // --- 核心状态缓存 ---
        public BossIdleState idleState;
        public BossChaseState chaseState;
        public BossAttackState attackState;
        // 退避、立回高阶动作扩展
        public BossStrafeState strafeState;
        public BossEvasionState evasionState;

        public BossStateMachine(BossController controller)
        {
            this.bossController = controller;
            
            // 实例化各个状态并传入自身
            idleState = new BossIdleState(this);
            chaseState = new BossChaseState(this);
            attackState = new BossAttackState(this);
            strafeState = new BossStrafeState(this);
            evasionState = new BossEvasionState(this);
        }

        public override void ChangeState(IState targetState)
        {
            base.ChangeState(targetState);
            // 可以在此派发事件或进行状态可视化记录
        }
    }
}
