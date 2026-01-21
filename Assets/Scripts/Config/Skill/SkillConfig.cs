using System;
using JKFrame;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "Skill Config", menuName = "Config/SkillConfig")]
    public class SkillConfig : ConfigBase
    {
        [LabelText("技能名称")] public string SkillName;
        [LabelText("帧数上限")] public int FrameCount = 100;
        [LabelText("帧率")] public int FrameRate = 30;

        [NonSerialized, OdinSerialize]
        public SkillAnimationData SkillAnimationData = new();
        [NonSerialized, OdinSerialize]
        public SkillAudioData SkillAudioData = new();
        [NonSerialized, OdinSerialize]
        public SkillEffectData SkillEffectData = new();

#if UNITY_EDITOR
        private static Action skillConfigValidateAction;

        public static void SetSkillConfigValidateAction(Action action)
        {
            skillConfigValidateAction = action;
        }
        private void OnValidate()
        {
            skillConfigValidateAction?.Invoke();
        }
#endif
    }
}