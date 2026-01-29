using Arch.Core;
using Config;

namespace Battle.ECS.Component
{
    /// <summary>
    /// buff组件
    /// </summary>
    public struct Buff
    {
        public readonly int ID;
        public readonly BuffConfig Config;
        public Entity Caster { get; private set; } // 当前生效的施法者实体
        public Entity Target; // 目标实体
        public Entity Vfx; // 特效实体

        // TODO：这些事件需要都有吗？
        public bool HasHurtEvent;
        public bool HasDealDamageEvent;
        public bool HasHurtModifierEvent;
        public bool HasDealDamageModifierEvent;
        public bool HasOnCastEvent;
        public bool HasHealedEvent;
        public bool HasAddShieldEvent;
        public bool HasTargetDeathEvent;

        public Buff(BuffConfig config, Entity caster)
        {
            ID = config.buffId;
            Config = config;
            Caster = caster;
            Target = Entity.Null;
            Vfx = Entity.Null;

            HasHurtEvent = false;
            HasDealDamageEvent = false;
            HasHurtModifierEvent = false;
            HasDealDamageModifierEvent = false;
            HasOnCastEvent = false;
            HasHealedEvent = false;
            HasAddShieldEvent = false;
            HasTargetDeathEvent = false;
        }

        public void ReplaceCaster(Entity caster)
        {
            Caster = caster;
        }
    }
}