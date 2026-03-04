using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using UnityEngine;

namespace Manager
{
    public enum InventorySortMode
    {
        ByItemIdAsc,
        ByTypeThenId,
        ByCountDescThenId
    }

    /// <summary>
    /// 给 UI 使用的背包条目快照数据。
    /// </summary>
    public readonly struct InventoryItemViewData
    {
        public readonly int ItemId;
        public readonly int Count;
        public readonly ItemConfig Config;

        public bool HasConfig => Config != null;
        public string DisplayName => Config != null ? Config.ItemName : $"Unknown Item ({ItemId})";

        public InventoryItemViewData(int itemId, int count, ItemConfig config)
        {
            ItemId = itemId;
            Count = count;
            Config = config;
        }
    }

    /// <summary>
    /// 背包管理器（静态工具类，依托 DataManager 存档）。
    /// 提供添加、移除、查询、快照和初始化修复接口。
    /// </summary>
    public static class InventoryManager
    {
        public const string InventoryChangedEvent = "InventoryChanged";
        public const string InventoryItemChangedEvent = "InventoryItemChanged";

        /// <summary>背包发生变化时触发（itemId, newCount）。</summary>
        public static event System.Action<int, int> OnInventoryChanged;
        /// <summary>背包列表变化时触发（增删改后统一通知一次）。</summary>
        public static event System.Action OnInventoryListChanged;

        private static ItemTable _itemTable;

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

        private static ItemTable GetItemTable()
        {
            if (_itemTable == null)
            {
                _itemTable = ResSystem.LoadAsset<ItemTable>("ItemTable");
            }
            return _itemTable;
        }

        private static void NotifyChanged(int itemId, int newCount)
        {
            OnInventoryChanged?.Invoke(itemId, newCount);
            OnInventoryListChanged?.Invoke();
            EventSystem.EventTrigger(InventoryItemChangedEvent, itemId, newCount);
            EventSystem.EventTrigger(InventoryChangedEvent);
        }

        private static int CompareByTypeThenId(InventoryItemViewData a, InventoryItemViewData b)
        {
            if (a.HasConfig && b.HasConfig)
            {
                int typeCmp = a.Config.ItemType.CompareTo(b.Config.ItemType);
                if (typeCmp != 0) return typeCmp;
            }
            else if (a.HasConfig != b.HasConfig)
            {
                // 有配置的排在前面，未知配置排后面
                return a.HasConfig ? -1 : 1;
            }

            return a.ItemId.CompareTo(b.ItemId);
        }

        private static int CompareByCountDescThenId(InventoryItemViewData a, InventoryItemViewData b)
        {
            int countCmp = b.Count.CompareTo(a.Count);
            if (countCmp != 0) return countCmp;
            return a.ItemId.CompareTo(b.ItemId);
        }

        // ── 初始化 / 修复 ─────────────────────────────────────────

        /// <summary>
        /// 运行时初始化入口：加载 ItemTable，修复背包异常数量，并推送一次全量刷新事件给 UI。
        /// 建议在进入游戏场景后调用一次。
        /// </summary>
        public static void InitializeForRuntime()
        {
            var d = Dict;
            if (d == null) return;

            var itemTable = GetItemTable();
            bool dirty = false;

            // 修复存档异常：去除 <=0 的脏数据；按配置上限进行钳制
            var keys = new List<int>(d.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int itemId = keys[i];
                int count = d[itemId];

                if (count <= 0)
                {
                    d.Remove(itemId);
                    dirty = true;
                    continue;
                }

                var cfg = itemTable != null ? itemTable.GetItemById(itemId) : null;
                if (cfg == null) continue;

                int clamped = Mathf.Clamp(count, 0, Mathf.Max(1, cfg.MaxStackCount));
                if (clamped <= 0)
                {
                    d.Remove(itemId);
                    dirty = true;
                }
                else if (clamped != count)
                {
                    d[itemId] = clamped;
                    dirty = true;
                }
            }

            if (dirty)
            {
                DataManager.SaveGameData();
            }

            // 进入场景后主动推一帧，保证 UI 初次显示是正确数据
            OnInventoryListChanged?.Invoke();
            EventSystem.EventTrigger(InventoryChangedEvent);
        }

        /// <summary>
        /// 强制全量刷新通知（不改数据，只通知 UI）。
        /// </summary>
        public static void PublishFullRefresh()
        {
            OnInventoryListChanged?.Invoke();
            EventSystem.EventTrigger(InventoryChangedEvent);
        }

        /// <summary>
        /// 手动触发存档（用于批量 Add/Remove 时合并写盘）。
        /// </summary>
        public static void FlushSave()
        {
            DataManager.SaveGameData();
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

        /// <summary>背包中的不同物品种类数（按 itemId 去重）。</summary>
        public static int GetDistinctItemCount()
        {
            var d = Dict;
            return d?.Count ?? 0;
        }

        /// <summary>通过 itemId 获取配置（ItemTable 未加载或不存在时返回 false）。</summary>
        public static bool TryGetItemConfig(int itemId, out ItemConfig config)
        {
            config = null;
            var table = GetItemTable();
            if (table == null) return false;

            config = table.GetItemById(itemId);
            return config != null;
        }

        /// <summary>
        /// 填充背包快照（供 UI 直接渲染）。
        /// </summary>
        public static void FillSnapshot(
            List<InventoryItemViewData> result,
            ItemType? filterType = null,
            InventorySortMode sortMode = InventorySortMode.ByTypeThenId)
        {
            if (result == null) return;
            result.Clear();

            var d = Dict;
            if (d == null || d.Count == 0) return;

            var table = GetItemTable();
            foreach (var pair in d)
            {
                int itemId = pair.Key;
                int count = pair.Value;
                if (count <= 0) continue;

                var cfg = table != null ? table.GetItemById(itemId) : null;
                if (filterType.HasValue)
                {
                    if (cfg == null || cfg.ItemType != filterType.Value) continue;
                }

                result.Add(new InventoryItemViewData(itemId, count, cfg));
            }

            switch (sortMode)
            {
                case InventorySortMode.ByItemIdAsc:
                    result.Sort((a, b) => a.ItemId.CompareTo(b.ItemId));
                    break;
                case InventorySortMode.ByCountDescThenId:
                    result.Sort(CompareByCountDescThenId);
                    break;
                case InventorySortMode.ByTypeThenId:
                default:
                    result.Sort(CompareByTypeThenId);
                    break;
            }
        }

        /// <summary>
        /// 获取背包快照（返回新列表）。
        /// </summary>
        public static List<InventoryItemViewData> GetSnapshot(
            ItemType? filterType = null,
            InventorySortMode sortMode = InventorySortMode.ByTypeThenId)
        {
            var list = new List<InventoryItemViewData>();
            FillSnapshot(list, filterType, sortMode);
            return list;
        }

        // ── 添加 ───────────────────────────────────────────────────

        /// <summary>
        /// 向背包添加物品，自动限制叠加上限。
        /// 返回实际添加数量（可能因叠加上限而小于 amount）。
        /// </summary>
        public static int AddItem(ItemConfig config, int amount = 1, bool autoSave = true)
        {
            if (config == null || amount <= 0) return 0;
            var d = Dict;
            if (d == null) return 0;

            d.TryGetValue(config.ItemId, out int current);
            int canAdd = Mathf.Max(0, config.MaxStackCount - current);
            int realAdd = Mathf.Min(amount, canAdd);

            if (realAdd <= 0)
            {
                JKLog.Warning($"[Inventory] {config.ItemName} 已达叠加上限 {config.MaxStackCount}");
                return 0;
            }

            int newCount = current + realAdd;
            d[config.ItemId] = newCount;
            if (autoSave)
            {
                DataManager.SaveGameData();
            }

            NotifyChanged(config.ItemId, newCount);
            JKLog.Log($"[Inventory] 获得 {config.ItemName} x{realAdd}，当前持有: {newCount}");
            return realAdd;
        }

        // ── 移除 ───────────────────────────────────────────────────

        /// <summary>
        /// 消耗物品，数量不足时返回 false 且不扣除。
        /// </summary>
        public static bool RemoveItem(int itemId, int amount = 1, bool autoSave = true)
        {
            if (amount <= 0) return false;

            var d = Dict;
            if (d == null) return false;
            if (!d.TryGetValue(itemId, out int current) || current < amount)
            {
                JKLog.Warning($"[Inventory] 物品 {itemId} 数量不足（当前 {current}，需要 {amount}）");
                return false;
            }

            int newCount = current - amount;
            if (newCount <= 0)
            {
                d.Remove(itemId);
                newCount = 0;
            }
            else
            {
                d[itemId] = newCount;
            }

            if (autoSave)
            {
                DataManager.SaveGameData();
            }

            NotifyChanged(itemId, newCount);
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

            if (config.HpRestore > 0f)
            {
                // ECS 是血量权威数据源，SyncHealth 每帧将 Health.Current → CharacterAttribute.SetHp
                // 所以必须更新 ECS；如果 ECS 不在场则 fallback 到直接修改 CharacterAttribute
                var ecsRunner = Battle.ECS.BattleEcsRunner.Instance;
                if (ecsRunner != null)
                    ecsRunner.HealPlayer(config.HpRestore);
                else
                    target.AddHp(config.HpRestore);
            }

            if (config.MpRestore > 0f) target.AddMp(config.MpRestore);

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
