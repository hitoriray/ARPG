using System.Collections.Generic;
using Attribute;
using Config;
using Skill;

namespace Boss
{
    public abstract class BossSkillBrainBase : SkillBrainBase
    {
        private BossController boss;

        public virtual void Init(BossController boss, List<SkillConfig> skillConfigs)
        {
            base.Init(boss);
            this.boss = boss;

            if (skillConfigs == null)
            {
                skillBehaviours = new List<Skill.Behaviour.SkillBehaviourBase>();
                return;
            }

            skillBehaviours = new List<Skill.Behaviour.SkillBehaviourBase>(skillConfigs.Count);
            for (int i = 0; i < skillConfigs.Count; i++)
            {
                AddSkill(boss, skillConfigs, i, null);
            }
        }

        public override bool CheckCost(SkillCostType costType, float costValue)
        {
            if (boss == null || boss.CharacterAttribute == null)
                return false;

            switch (costType)
            {
                case SkillCostType.HP:
                    return boss.CharacterAttribute.currentHp >= -costValue;
                case SkillCostType.MP:
                    return boss.CharacterAttribute.currentMp >= -costValue;
            }

            return false;
        }

        public override void ApplyCost(SkillCostType costType, float costValue)
        {
            if (boss == null || boss.CharacterAttribute == null)
                return;

            switch (costType)
            {
                case SkillCostType.HP:
                    boss.CharacterAttribute.AddHp(costValue);
                    break;
                case SkillCostType.MP:
                    boss.CharacterAttribute.AddMp(costValue);
                    break;
            }
        }
    }
}
