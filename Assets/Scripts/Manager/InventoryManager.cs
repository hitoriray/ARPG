using System.Collections.Generic;
using Config;
using Data;
using JKFrame;

namespace Manager
{
    /// <summary>
    /// 背包管理器（静态工具类，依托 DataManager 存档）。
    /// 提供添加、移除、查询物品和消耗品使用等接口。
    /// 物品量变化时触发 OnInventoryChanged 事件供 UI 刷新。
    /// </summary>
    public static class InventoryManager
    {
        /// <summary>背包发生变化时触发（itemId, newCount）。</summary>
        public static event System.Action<int, int> OnInventoryChanged;

        // ── 内部工具 ───────────────────────────────────────────────

        private static Dictionary<int, int> Dict
        {
            get
            {
                if (DataManager.GameData == null) return null;
                if (DataManager.GameData.InventoryItems == null)
                    DataManager.GameData.InventoryItems = new Serialized_Dic<int, int>();
                return DataManager.GameData.InventoryItems.Dictionary;
            }
        }

        // ── 查询 ───────────────────────────────────────────────────

        /// <summary>返回指定物品当前持有数量（没有则返回 0）。</summary>
        public static int GetCount(int itemId)
        {
            var d = Dict;
            if (d == null) return 0;
            d.TryGetValue(itemId, out int count);
            return count;
        }

        /// <summary>是否拥有至少 amount 个指定物品。</summary>
        public static bool Has(int itemId, int amount = 1) => GetCount(itemId) >= amount;

        // ── 添加 ───────────────────────────────────────────────────

        /// <summary>
        /// 向背包添加物品，自动限制叠加上限，自动存档。
        /// 返回实际添加数量（可能因叠加上限而小于 amount）。
        /// </summary>
        public static int AddItem(ItemConfig config, int amount = 1)
        {
            if (config == null || amount <= 0) return 0;
            var d = Dict;
            if (d == null) return 0;

            d.TryGetValue(config.ItemId, out int current);
            int canAdd = config.MaxStackCount - current;
            int realAdd = System.Math.Min(amount, canAdd);

            if (realAdd <= 0)
            {
                JKLog.Warning($"[Inventory] {config.ItemName} 已达叠加上限 {config.MaxStackCount}");
                return 0;
            }

            d[config.ItemId] = current + realAdd;
            DataManager.SaveGameData();
            OnInventoryChanged?.Invoke(config.ItemId, d[config.ItemId]);
            JKLog.Log($"[Inventory] 获得 {config.ItemName} x{realAdd}，当前持有: {d[config.ItemId]}");
            return realAdd;
        }

        // ── 移除 ───────────────────────────────────────────────────

        /// <summary>
        /// 消耗物品，数量不足时返回 false 且不扣除。
        /// </summary>
        public static bool RemoveItem(int itemId, int amount = 1)
        {
            var d = Dict;
            if (d == null) return false;
            if (!d.TryGetValue(itemId, out int current) || current < amount)
            {
                JKLog.Warning($"[Inventory] 物品 {itemId} 数量不足（当前 {current}，需要 {amount}）");
                return false;
            }
            int newCount = current - amount;
            if (newCount <= 0)
                d.Remove(itemId);
            else
                d[itemId] = newCount;

            DataManager.SaveGameData();
            OnInventoryChanged?.Invoke(itemId, newCount);
            return true;
        }

        // ── 消耗品使用 ─────────────────────────────────────────────

        /// <summary>
        /// 使用一个消耗品（扣除数量 + 应用效果到角色属性）。
        /// </summary>
        public static bool UseConsumable(ItemConfig config, Attribute.CharacterAttribute target)
        {
            if (config == null || target == null) return false;
            if (config.ItemType != ItemType.Consumable) return false;
            if (!RemoveItem(config.ItemId, 1)) return false;

            if (config.HpRestore > 0f)  target.AddHp(config.HpRestore);
            if (config.MpRestore > 0f)  target.AddMp(config.MpRestore);

            if (config.ExpGain > 0L)
            {
                // 经验丹：需要玩家角色的 LevelGrowthConfig
                var playerConfig = PlayerService.Instance.GetCharacterConfig();
                if (playerConfig?.LevelGrowthConfig != null)
                {
                    DataManager.AddExperience(
                        DataManager.GameData.SelectedCharacterId,
                        config.ExpGain,
                        playerConfig.LevelGrowthConfig);
                }
            }

            JKLog.Log($"[Inventory] 使用了 {config.ItemName}");
            return true;
        }
    }
}
