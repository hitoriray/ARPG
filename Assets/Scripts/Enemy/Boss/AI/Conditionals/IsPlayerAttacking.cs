using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using RayPlayer; 

namespace Enemy.Boss.AI.Conditionals
{
    [TaskCategory("Boss")]
    [TaskDescription("检测玩家是否处于攻击/技能释放等硬直状态中，用于实现Boss的读指令(Input Reading)。")]
    public class IsPlayerAttacking : Conditional
    {
        public SharedTransform targetPlayer;
        
        // 缓存的玩家控制器，避免每帧GetComponent
        private PlayerController cachedPlayerController;

        public override void OnStart()
        {
            if (targetPlayer.Value != null && cachedPlayerController == null)
            {
                cachedPlayerController = targetPlayer.Value.GetComponent<PlayerController>();
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (cachedPlayerController == null)
            {
                return TaskStatus.Failure;
            }

            // 根据游戏实际架构判定玩家是否在发招硬直中。
            // 假设 PlayerController 或者其 StateMachine 提供暴露当前状态的方法：
            // 此处用简化逻辑示例，真实项目可判定：cachedPlayerController.StateMachine.currentState is PlayerSkillState
            
            // 假设提供了一个公开的只读属性或者查询方法来辨认玩家是不是正在放技能
            // 为了安全起见，如果在项目中没有现成标识可用，这里通过访问玩家身上的 Animancer 或者 StateMachine 来判断
            // 比如：
            if (cachedPlayerController.MovementStateMachine != null && 
                cachedPlayerController.MovementStateMachine.currentState != cachedPlayerController.MovementStateMachine.idleState &&
                cachedPlayerController.MovementStateMachine.currentState != cachedPlayerController.MovementStateMachine.moveLoopState)
            {
                // 可以认为玩家正在执行非简单的跑动与待机，极可能有破绽 (比如 Attack, Jump, Dodge)
                // 真实应用中，建议给 PlayerController 加一个 `IsActionLocked` 或者 `IsAttacking` 的属性布尔。
                 return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }

        public override void OnReset()
        {
            targetPlayer = null;
            cachedPlayerController = null;
        }
    }
}
