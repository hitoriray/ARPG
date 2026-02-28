using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 单条掉落规则。
    /// </summary>
    [Serializable]
    public class LootEntry
    {
        [LabelText("物品")]
        [Required]
        public ItemConfig Item;

        [LabelText("数量Min")]
        [MinValue(1)]
        public int MinCount = 1;

        [LabelText("数量Max")]
        [MinValue(1)]
        public int MaxCount = 1;

        [LabelText("掉率%")]
        [Range(0f, 100f)]
        public float DropChance = 100f;
    }

    /// <summary>
    /// 掉落表配置（ScriptableObject）。
    /// 挂在 SpawnGroupConfig 里，描述该类敌人死亡后掉落什么。
    /// 创建路径：Create → Config/Item/LootConfig
    /// </summary>
    [CreateAssetMenu(fileName = "LootConfig", menuName = "Config/Item/LootConfig")]
    public class LootConfig : ScriptableObject
    {
        [LabelText("必掉金币（0 = 不掉）")]
        [MinValue(0)]
        public int GoldMin = 10;

        [LabelText("金币上限")]
        [MinValue(0)]
        public int GoldMax = 30;

        [LabelText("掉落规则列表")]
        [ListDrawerSettings(ShowFoldout = false)]
        public List<LootEntry> Entries = new();

        /// <summary>
        /// 按掉率随机，返回这次死亡实际掉落的物品列表（物品+数量）。
        /// </summary>
        public List<(ItemConfig item, int count)> Roll()
        {
            var result = new List<(ItemConfig, int)>();
            foreach (var entry in Entries)
            {
                if (entry.Item == null) continue;
                if (UnityEngine.Random.Range(0f, 100f) <= entry.DropChance)
                {
                    int count = UnityEngine.Random.Range(entry.MinCount, entry.MaxCount + 1);
                    result.Add((entry.Item, count));
                }
            }
            return result;
        }

        /// <summary>
        /// 随机返回本次掉落的金币数量。
        /// </summary>
        public int RollGold()
        {
            if (GoldMax <= 0) return 0;
            return UnityEngine.Random.Range(GoldMin, GoldMax + 1);
        }
    }
}
