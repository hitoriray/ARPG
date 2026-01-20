using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Config
{
    public class SkillAudioData
    {
        /// <summary>
        /// 动画帧事件： 帧数 -> 事件
        /// </summary>
        [NonSerialized, OdinSerialize]
        public List<SkillAudioEvent> FrameData = new();
    }
}