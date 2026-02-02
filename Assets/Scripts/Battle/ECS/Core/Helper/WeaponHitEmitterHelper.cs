using Battle.ECS.Component;
using Skill.Behaviour;
using UnityEngine;

namespace Battle.ECS.Core.Helper
{
    public static class WeaponHitEmitterHelper
    {
        public static bool Emit(SkillBehaviourBase behaviour, IHitTarget target, AttackData attackData)
        {
            if (behaviour == null || target == null)
                return false;

            RayDebug.Log($"[{nameof(WeaponHitEmitterHelper)}]: 由Ecs发射{nameof(WeaponDetectionRequest)}");
            BattleEcsRunner.Instance.Context.World.Create(
                new WeaponDetectionRequest()
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