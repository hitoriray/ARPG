using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Config
{
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
}