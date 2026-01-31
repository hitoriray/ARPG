using System;

namespace Data
{
    /// <summary>
    /// 游戏的动态数据
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int SelectedCharacterId = 1001;
        // 玩家已解锁的角色列表
        public Serialized_List<int> UnlockedCharacterIds;
        // 玩家队伍的角色id（-1表示无）
        public int[] CharacterTeam = new int[4] { 1001, -1, -1, -1 };
        // 角色的技能学习数据
        public Serialized_Dic<int, SkillLearnedDatas> CharacterSkillsDict;
        // 角色的技能快捷栏数据
        public Serialized_Dic<int, ShortcutSkillSlotData> CharacterShortcutSkillsDict;
        
        // public ProfessionType ProfessionType;
        // public Serialized_Dic<int, CustomCharacterPartData> CustomPartDataDict;
    }

    /// <summary>
    /// 自定义角色部位的数据
    /// </summary>
    // [Serializable]
    // public class CustomCharacterPartData
    // {
    //     public int Index;
    //     public float Size;
    //     public float Height;
    //     public Serialized_Color Color1;
    //     public Serialized_Color Color2;
    // }

    /// <summary>
    /// 技能快捷栏的数据
    /// </summary>
    [Serializable]
    public class ShortcutSkillSlotData
    {
        public int[] skillIds; // -1代表空格子，其他代表技能索引
    }

    [Serializable]
    public class Serialized_List<T>
    {
        public System.Collections.Generic.List<T> List = new();
    }
}
