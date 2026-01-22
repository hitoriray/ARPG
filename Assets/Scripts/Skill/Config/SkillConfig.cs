using JKFrame;
using Skill.Behaviour;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "SkillConfig", menuName = "Config/Skill/SkillConfig")]
    public class SkillConfig : ConfigBase
    {
        public SkillClip[] Clips; // 全部的技能片段
        public SkillBehaviourBase Behaviour; // 技能的运行逻辑
    }
}