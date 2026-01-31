using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Data
{
    [Serializable]
    public class SkillLearnedDatas
    {
        /// <summary>
        /// Key: 对应技能配置中的索引，Value：技能学习数据
        /// </summary>
        public Serialized_Dic<int, SkillLearnedData> SkillLearnedDataDict = new();

        public int SkillTotalPoint;
    }
    
    [Serializable]
    public class SkillLearnedData
    {
        public int lv;
    }
}