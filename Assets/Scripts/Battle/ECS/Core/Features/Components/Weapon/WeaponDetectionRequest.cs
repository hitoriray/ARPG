using Skill.Behaviour;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 武器命中请求（由WeaponController触发）
    /// </summary>
    public struct WeaponDetectionRequest
    {
        public SkillBehaviourBase Behaviour;
        public IHitTarget Target;
        public AttackData AttackData;
    }
}