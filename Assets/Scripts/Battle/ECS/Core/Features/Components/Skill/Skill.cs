using Arch.Core;
using Config;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 技能组件
    /// </summary>
    public struct Skill
    {
        public readonly int ID;
        public readonly SkillConfig Config; //技能配置
        public int Level { get; private set; } //技能等级
        public readonly Entity Caster; //所有者
        public bool Enable { get; private set; } // 是否启用

        public Skill(SkillConfig config, Entity caster)
        {
            ID = config.skillId;
            Config = config;
            Level = 1;
            Caster = caster;
            Enable = false;
        }

        public int LevelUp()
        {
            Level++;
            return Level;
        }

        public bool SetEnable(bool enable)
        {
            if (Enable == enable)
                return false;
            Enable = enable;
            return true;
        }
    }
}