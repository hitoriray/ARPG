using System.Collections.Generic;
using Config;
using Data;
using Skill;

namespace RayPlayer
{
    public abstract class PlayerSkillBrainBase : SkillBrainBase
    {
        private PlayerController player;
        
        public virtual void Init(PlayerController player, SkillLearnedDatas skillLearnedDatas)
        {
            base.Init(player);
            this.player = player;
            // 基于所学技能去初始化，后续是要通过学习修改
            int skillCount = skillLearnedDatas.SkillLearnedDataDict.Dictionary.Count;

            skillBehaviours = new(skillCount);
            List<SkillConfig> skillConfigs = PlayerManager.Instance.GetAllSkillConfig();
            foreach (var item in skillLearnedDatas.SkillLearnedDataDict.Dictionary)
            {
                AddSkill(player, skillConfigs, item.Key, item.Value);
            }
        }

        public override bool CheckCost(SkillCostType costType, float costValue)
        {
            switch (costType)
            {
                case SkillCostType.HP:
                    return player.CharacterAttribute.currentHp >= -costValue;
                case SkillCostType.MP:
                    return player.CharacterAttribute.currentMp >= -costValue;
            }
            return false;
        }

        /// <summary>
        /// 应用代价
        /// </summary>
        /// <param name="costType"></param>
        /// <param name="costValue">具体代价（这里会有负数的情况，因此全部使用Add处理）</param>
        public override void ApplyCost(SkillCostType costType, float costValue)
        {
            switch (costType)
            {
                case SkillCostType.HP:
                    player.CharacterAttribute.AddHp(costValue);
                    break;
                case SkillCostType.MP:
                    player.CharacterAttribute.AddMp(costValue);
                    break;
            }
        }
    }
}