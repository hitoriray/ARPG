using System;
using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Config.Skill
{
    [CreateAssetMenu(fileName = "Skill Config", menuName = "Config/SkillConfig")]
    public class SkillConfig : ConfigBase
    {
        [LabelText("技能名称")] public string SkillName;
        [LabelText("帧数上限")] public int FrameCount = 100;
        [LabelText("帧率")] public int FrameRate = 30;

        [NonSerialized, OdinSerialize]
        public SkillAnimationData SkillAnimationData = new();

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

    /// <summary>
    /// 技能动画数据
    /// </summary>
    [Serializable]
    public class SkillAnimationData
    {
        /// <summary>
        /// 动画帧事件： 帧数 -> 事件
        /// </summary>
        [NonSerialized, OdinSerialize]
        [DictionaryDrawerSettings(KeyLabel = "帧数", ValueLabel = "动画事件")]
        public Dictionary<int, SkillAnimationEvent> FrameEventDict = new();
    }

    /// <summary>
    /// 帧事件基类
    /// </summary>
    [Serializable]
    public abstract class SkillFrameEventBase
    {
        
    }

    /// <summary>
    /// 动画帧事件
    /// </summary>
    public class SkillAnimationEvent : SkillFrameEventBase
    {
        public AnimationClip AnimationClip;
        public bool ApplyRootMotion;
        public float TransitionTime = 0.25f;
        
#if UNITY_EDITOR
        public int DurationFrame;
#endif
    }
}