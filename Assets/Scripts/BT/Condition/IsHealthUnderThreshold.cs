using Attribute;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Boss;
using RayPlayer;
using UnityEngine;

namespace BT.Conditions
{
    [TaskCategory("Common")]
    public class IsHealthUnderThreshold : Conditional
    {
        public SharedTransform InTarget;
        public SharedFloat InThreshold; // <=1 means percent, >1 means absolute HP
        
        private Transform cachedTarget;
        private CharacterAttribute cachedAttribute;

        public override void OnStart()
        {
            cachedTarget = InTarget.Value;
            cachedAttribute = ResolveAttribute(cachedTarget);
        }

        public override TaskStatus OnUpdate()
        {
            if (cachedTarget == null || cachedAttribute == null)
                return TaskStatus.Failure;

            float hp = cachedAttribute.currentHp;
            float max = cachedAttribute.maxHp.Total;
            float th = InThreshold.Value;

            bool under = th <= 1f ? (max > 0f && hp / max <= th) : hp <= th;
            return under ? TaskStatus.Success : TaskStatus.Failure;
        }

        private static CharacterAttribute ResolveAttribute(Transform target)
        {
            if (target == null)
                return null;

            var attr = target.GetComponentInParent<CharacterAttribute>();
            if (attr != null)
                return attr;

            var boss = target.GetComponentInParent<BossController>();
            if (boss != null && boss.CharacterAttribute != null)
                return boss.CharacterAttribute;

            var player = target.GetComponentInParent<PlayerController>();
            if (player != null && player.CharacterAttribute != null)
                return player.CharacterAttribute;

            return null;
        }
    }
}
