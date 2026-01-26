using System;
using Config;

namespace Data
{
    /// <summary>
    /// 游戏的动态数据
    /// </summary>
    [Serializable]
    public class GameData
    {
        public ProfessionType ProfessionType;
        public Serialized_Dic<int, CustomCharacterPartData> CustomPartDataDict;
        public SkillLearnedDatas SkillLearnedDatas;
    }

    /// <summary>
    /// 自定义角色部位的数据
    /// </summary>
    [Serializable]
    public class CustomCharacterPartData
    {
        public int Index;
        public float Size;
        public float Height;
        public Serialized_Color Color1;
        public Serialized_Color Color2;
    }
}
