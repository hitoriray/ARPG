using System;
using System.Collections.Generic;
using Sirenix.Serialization;

namespace Config
{
    public class SkillEffectData
    {
        /// <summary>
        /// 特效事件
        /// </summary>
        [NonSerialized, OdinSerialize]
        public List<SkillEffectEvent> FrameData = new();
    }
}