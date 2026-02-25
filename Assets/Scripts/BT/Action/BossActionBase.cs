using BehaviorDesigner.Runtime.Tasks;
using Boss;

namespace BT.Actions
{
    public abstract class BossActionBase : BehaviorDesigner.Runtime.Tasks.Action
    {
        protected BossController boss;

        public override void OnStart()
        {
            if (boss == null)
                boss = GetComponent<BossController>();
        }

        protected bool EnsureBoss()
        {
            return boss != null;
        }
    }
}
