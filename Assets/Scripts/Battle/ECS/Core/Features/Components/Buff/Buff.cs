using Arch.Core;
using Config;
using FixMath;

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
        
        public Buff(BuffConfig config, Entity caster)
        {
            ID = config.buffId;
            Config = config;
            Caster = caster;
            Target = Entity.Null;
            Vfx = Entity.Null;
        }
    }
}