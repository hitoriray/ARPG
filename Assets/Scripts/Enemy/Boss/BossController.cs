using System;
using UnityEngine;
using Animancer;
using RayPlayer;
using Enemy.Boss.State;
using BehaviorDesigner.Runtime;

namespace Enemy.Boss
{
    /// <summary>
    /// Boss物理与状态调度控制器
    /// 继承 CharacterControllerBase 获取与玩家一致的底层位移、重力支持
    /// 提供 Behavior Designer 调用的各类指令接口
    /// </summary>
    [RequireComponent(typeof(AnimancerComponent))]
    public class BossController : CharacterControllerBase
    {
        [Header("Boss 动作配置")]
        [SerializeField] private PlayerSO bossMotionSource;
        public PlayerSO BossMotionSource => bossMotionSource;

        [Header("目标追踪配置")]
        [SerializeField] private Transform target;
        public Transform Target { get => target; set => target = value; }

        public BossStateMachine StateMachine { get; private set; }

        // BD 行为树组件引用（挂在同一个 GameObject 上）
        private BehaviorTree behaviorTree;

        // Animancer SmoothedFloatParameter，与玩家共用同一套BlendTree参数机制
        // (SmoothedFloatParameter会自动将值平滑写入Animancer的参数系统，驱动Mixer/BlendTree)
        public SmoothedFloatParameter SpeedParameter { get; private set; }
        public SmoothedFloatParameter StandParameter { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            // 初始化 Boss专属状态机
            StateMachine = new BossStateMachine(this);

            // 缓存行为树
            behaviorTree = GetComponent<BehaviorTree>();

            // 初始化 Animancer 参数（和玩家 PlayerReusableData 中的初始化方式完全一致）
            if (bossMotionSource != null && bossMotionSource.playerParameterData != null)
            {
                if (bossMotionSource.playerParameterData.speedValueParameter != null)
                {
                    SpeedParameter = new SmoothedFloatParameter(
                        animancer,
                        bossMotionSource.playerParameterData.speedValueParameter,
                        0.15f // smoothTime 越小越快达到目标值
                    );
                }
                if (bossMotionSource.playerParameterData.standValueParameter != null)
                {
                    StandParameter = new SmoothedFloatParameter(
                        animancer,
                        bossMotionSource.playerParameterData.standValueParameter,
                        0.15f
                    );
                    StandParameter.TargetValue = 1f; // 1 = 站姿
                }
            }
        }

        private void Start()
        {
            // 给定初始默认状态
            StateMachine.ChangeState(StateMachine.idleState);
        }

        protected override void Update()
        {
            base.Update();

            // ---- 每帧刷新行为树共享变量 DistanceToTarget ----
            if (behaviorTree != null && target != null)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                behaviorTree.SetVariableValue("DistanceToTarget", dist);
            }

            // 在此驱动状态机的逻辑更新
            StateMachine?.OnUpdate();
        }

        protected override void OnAnimatorMove()
        {
            base.OnAnimatorMove();
            // 在此驱动状态机的动画位移处理
            StateMachine?.OnAnimationUpdate();
        }

        /// <summary>
        /// 提供给动画事件(如普攻结束) 或 状态内部调用的收尾方法
        /// </summary>
        public void AnimationEnd()
        {
            StateMachine?.OnAnimationEnd();
        }

        /// <summary>
        /// 提供给 Behavior Designer 的命令接口，使 Boss 改变当前物理/动画状态
        /// </summary>
        public void ChangeState(BossStateType targetStateType)
        {
            switch(targetStateType)
            {
                case BossStateType.Idle:
                    StateMachine.ChangeState(StateMachine.idleState);
                    break;
                case BossStateType.Chase:
                    StateMachine.ChangeState(StateMachine.chaseState);
                    break;
                case BossStateType.Attack:
                    StateMachine.ChangeState(StateMachine.attackState);
                    break;
                case BossStateType.Strafe:
                    StateMachine.ChangeState(StateMachine.strafeState);
                    break;
                case BossStateType.Evasion:
                    StateMachine.ChangeState(StateMachine.evasionState);
                    break;
            }
        }
    }

    /// <summary>
    /// 用于 Behavior Designer 配置节点下拉选项的枚举
    /// </summary>
    public enum BossStateType
    {
        Idle,
        Chase,
        Attack,
        Strafe,
        Evasion,
        Hit,
        Death
    }
}

