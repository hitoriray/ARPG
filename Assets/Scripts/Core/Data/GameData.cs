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
        // 角色的等级/经验/金币等成长数据（Key = CharacterId）
        public Serialized_Dic<int, CharacterProgressData> CharacterProgressDict;
        // 玩家持有的金币（全局共享）
        public long Gold = 0;
        // 已被玩家清空（永久消灭）的 SpawnRegion 标识列表
        public Serialized_List<string> ClearedRegionKeys;
        // 背包：物品ID → 持有数量
        public Serialized_Dic<int, int> InventoryItems;
        // 无限期掉落物：场景名称 → 掉落物列表
        public Serialized_Dic<string, Serialized_List<PersistentDropData>> PersistentDrops;
        // AI 对话历史记录
        public Serialized_Dic<string, Serialized_List<AIChatRecord>> AIChatHistoryByNpc;
        public Serialized_List<AIChatRecord> AIChatHistory;
    }

    /// <summary>
    /// AI 对话历史的单条存档记录
    /// </summary>
    [Serializable]
    public class AIChatRecord
    {
        public string role;
        public string content;

        public AIChatRecord() { }
        public AIChatRecord(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    /// <summary>
    /// 单个角色的成长存档数据
    /// </summary>
    [Serializable]
    public class CharacterProgressData
    {
        public int  Level      = 1;   // 当前等级
        public long Experience = 0;   // 当前累计经验值
        /// <summary>当前 HP（-1 = 满血，进游戏后会用 maxHp.Total 填充）</summary>
        public float CurrentHp = -1f;
        /// <summary>当前 MP（-1 = 满蓝）</summary>
        public float CurrentMp = -1f;
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

    /// <summary>
    /// 持久化掉落物数据（无限时长）
    /// </summary>
    [Serializable]
    public class PersistentDropData
    {
        public string Guid;
        public int    ItemId;
        public int    Count;
        public Serialized_Vector3 Position;
    }

    [Serializable]
    public class Serialized_List<T>
    {
        public System.Collections.Generic.List<T> List = new();
    }
}
