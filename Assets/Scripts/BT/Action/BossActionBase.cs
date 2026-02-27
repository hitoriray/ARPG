using BehaviorDesigner.Runtime.Tasks;
using Boss;

namespace BT.Actions
{
    public abstract class BossActionBase : BehaviorDesigner.Runtime.Tasks.Action
    {
        protected BossController boss;

        /// <summary>
        /// A* 寻路中间层（可选）。若 Boss 未挂 BossAStarMover 则为 null，各节点应降级为直线逻辑。
        /// </summary>
        protected BossAStarMover astarMover;

        public override void OnStart()
        {
            if (boss == null)
                boss = GetComponent<BossController>();

            if (astarMover == null)
                astarMover = GetComponent<BossAStarMover>();
        }

        protected bool EnsureBoss()
        {
            return boss != null;
        }
    }
}
