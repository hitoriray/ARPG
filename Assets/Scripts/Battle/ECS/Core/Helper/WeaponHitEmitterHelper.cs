using Battle.ECS.Component;
using Skill.Behaviour;

namespace Battle.ECS.Core.Helper
{
    public static class WeaponHitEmitterHelper
    {
        public static bool Emit(SkillBehaviourBase behaviour, IHitTarget target, AttackData attackData)
        {
            if (behaviour == null || target == null)
                return false;

            BattleEcsRunner.Instance.Context.World.Create(
                new WeaponHitRequest()
                {
                    Behaviour = behaviour,
                    Target = target,
                    AttackData = attackData
                }
            );
            return true;
        }
    }
}