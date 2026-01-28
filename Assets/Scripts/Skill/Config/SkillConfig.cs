using System;
using System.Collections.Generic;
using JKFrame;
using Skill.Behaviour;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "SkillConfig", menuName = "Config/Skill/SkillConfig")]
    public class SkillConfig : ConfigBase
    {
        public string skillName;
        public Sprite skillIcon;
        public Sprite[] skillIcons;
        public string skillDescription;
        public int skillPointRequired;
        public int maxLv;
        public bool canRelease;
        public float basicCdTime; // 整段技能结束的cd时间
        public float basicAttackValue; // 攻击力
        
        public float cdTimeMultiplierPerLv; // cd每等级减少多少
        public float attackValueMultiplierPerLv; // 攻击力每等级增加多少
        
        public Dictionary<SkillCostType, float> ReleaseCostDict = new();
        public SkillClip[] Clips; // 全部的技能片段
        public SkillBehaviourBase Behaviour; // 技能的运行逻辑

        public float GetAttackValueByLv(int lv)
        {
            float result = basicAttackValue * ((lv - 1) * attackValueMultiplierPerLv + 1);
            return (float)Math.Round(result, 2);
        }
        
        public float GetCdTimeByLv(int lv)
        {
            float result = basicCdTime * (1 - (lv - 1) * cdTimeMultiplierPerLv);
            return (float)Math.Round(result, 2);
        }
    }
}