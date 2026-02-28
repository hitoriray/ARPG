using System;
using System.Collections.Generic;
using Sirenix.Serialization;

namespace Config
{
    public class SkillAudioData
    {
        /// <summary>
        /// 音效事件
        /// </summary>
        [NonSerialized, OdinSerialize]
        public List<SkillAudioEvent> FrameData = new();
    }
}