using BehaviorDesigner.Runtime.Tasks;
using Boss;

namespace BT.Conditions
{
    public abstract class BossConditionBase : Conditional
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
