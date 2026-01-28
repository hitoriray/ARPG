using System;
using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using Skill.Behaviour;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "SkillConfig", menuName = "Config/Skill/SkillConfig")]
    public class SkillConfig : ConfigBase
    {
        [LabelText("技能名称")] public string skillName;
        [LabelText("技能图标")] public Sprite skillIcon;
        [LabelText("多段技能图标")]
        [Tooltip("这里的第一个图标需要和技能图标一致！")]
        public Sprite[] skillIcons;
        [LabelText("技能描述")] public string skillDescription;
        [LabelText("技能加点所需点数")] public int skillPointRequired;
        [LabelText("最高等级")] public int maxLv;
        [LabelText("主动技能")] public bool canRelease;
        [LabelText("整个技能的CD")] public float basicCdTime; // 整段技能结束的cd时间
        [LabelText("攻击力")] public float basicAttackValue; // 攻击力
        
        [LabelText("每级CD减少倍率")] public float cdTimeMultiplierPerLv; // cd每等级减少多少
        [LabelText("每级攻击力增加倍率")] public float attackValueMultiplierPerLv; // 攻击力每等级增加多少
        
        [LabelText("释放代价")] public Dictionary<SkillCostType, float> ReleaseCostDict = new();
        [LabelText("技能动画片段")] public SkillClip[] Clips; // 全部的技能片段
        [LabelText("技能运行逻辑")] public SkillBehaviourBase Behaviour; // 技能的运行逻辑

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