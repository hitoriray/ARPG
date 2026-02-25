using UnityEngine;
using Animancer;
using RayPlayer; // 若PlayerSO等通用数据在该命名空间

namespace Enemy.Boss.State
{
    /// <summary>
    /// Boss各类状态的基类，类似Player的具体StateBase
    /// 注入了BossController依赖并提供对通用组件(Animancer等)的便捷访问
    /// </summary>
    public abstract class BossStateBase : IState
    {
        protected BossStateMachine stateMachine;
        protected BossController boss;
        protected AnimancerComponent animancer;
        
        // 如果Boss可以复用PlayerSO的动画数据，可以缓存一份
        protected PlayerSO bossMotionSource;

        public BossStateBase(BossStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
            this.boss = stateMachine.bossController;
            this.animancer = boss.animancer;
            this.bossMotionSource = boss.BossMotionSource;
        }

        public abstract void OnEnter();
        public abstract void OnExit();
        public abstract void OnUpdate();
        public abstract void OnAnimationUpdate();
        public abstract void OnAnimationEnd();

        // 可以在这里统一处理部分Boss特有的公共逻辑
    }
}
