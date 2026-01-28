using System;
using JKFrame;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "SkillClip", menuName = "Config/Skill/SkillClip")]
    public class SkillClip : ConfigBase
    {
        [LabelText("技能名称")] public string SkillName;
        [LabelText("帧数上限")] public int FrameCount = 100;
        [LabelText("帧率")] public int FrameRate = 30;

        [LabelText("动画数据")]
        [NonSerialized, OdinSerialize] public SkillAnimationData SkillAnimationData = new();
        [LabelText("音效数据")]
        [NonSerialized, OdinSerialize] public SkillAudioData SkillAudioData = new();
        [LabelText("特效数据")]
        [NonSerialized, OdinSerialize] public SkillEffectData SkillEffectData = new();
        [LabelText("攻击检测数据")]
        [NonSerialized, OdinSerialize] public SkillAttackDetectionData SkillAttackDetectionData = new();
        [LabelText("事件数据")]
        [NonSerialized, OdinSerialize] public SkillCustomEventData SkillCustomEventData = new();

#if UNITY_EDITOR
        private static Action skillClipValidateAction;

        public static void SetSkillClipValidateAction(Action action)
        {
            skillClipValidateAction = action;
        }
        private void OnValidate()
        {
            skillClipValidateAction?.Invoke();
        }
#endif
    }
}