using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Manager;

namespace BT.Actions
{
    /// <summary>
    /// 初始化玩家引用节点。
    /// 放在行为树最开头（Root → Sequence 第一个子节点），运行时从 PlayerManager 获取
    /// 玩家 Transform 并写入黑板共享变量 PlayerTransform。
    /// 每次行为树启动时执行一次（OnStart），保证动态生成的 Boss 也能拿到玩家引用。
    /// </summary>
    [TaskCategory("Enemy/Boss")]
    [TaskDescription("初始化：从 PlayerManager 获取玩家 Transform，写入黑板变量 PlayerTransform。")]
    public class InitPlayerRef : BossActionBase
    {
        [BehaviorDesigner.Runtime.Tasks.Tooltip("写入目标变量（对应 CanSeeObject/Patrol 里的 Target / SeenObject）")]
        public SharedTransform PlayerTransform;

        public override TaskStatus OnUpdate()
        {
            if (!EnsureBoss())
                return TaskStatus.Failure;

            var player = PlayerManager.Instance?.player;
            if (player == null)
                return TaskStatus.Failure;

            PlayerTransform.Value = player.transform;
            return TaskStatus.Success;
        }
    }
}
