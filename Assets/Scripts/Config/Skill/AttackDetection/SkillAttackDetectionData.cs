using System;
using System.Collections.Generic;
using Sirenix.Serialization;

namespace Config
{
    public class SkillAttackDetectionData
    {
        [NonSerialized, OdinSerialize]
        public List<SkillAttackDetectionEvent> FrameData = new();
    }
}