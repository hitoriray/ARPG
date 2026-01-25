using System.Collections.Generic;
using JKFrame;
using Skill.Behaviour;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "SkillConfig", menuName = "Config/Skill/SkillConfig")]
    public class SkillConfig : ConfigBase
    {
        public float cdTime; // 整段技能结束的cd时间
        public Dictionary<SkillCostType, float> ReleaseCostDict = new();
        public SkillClip[] Clips; // 全部的技能片段
        public SkillBehaviourBase Behaviour; // 技能的运行逻辑
    }
}