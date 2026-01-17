using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config.Skill
{
    [CreateAssetMenu(fileName = "Skill Config", menuName = "Config/Skill Config")]
    public class SkillConfig : ConfigBase
    {
        [LabelText("技能名称")] public string SkillName;
        [LabelText("帧数上限")] public int FrameCount = 100;
    }
}