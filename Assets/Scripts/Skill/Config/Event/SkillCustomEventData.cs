using System;
using System.Collections.Generic;
using Sirenix.Serialization;

namespace Config
{
    public class SkillCustomEventData
    {
        [NonSerialized, OdinSerialize]
        public Dictionary<int, SkillCustomEvent> FrameData = new();
    }
}