using System;
using System.Collections.Generic;
using UnityEngine;
using Config;
using Data;
using JKFrame;

namespace Manager
{
    /// <summary>
    /// 数据管理器
    /// </summary>
    public static class DataManager
    {
        private const int DefaultSkillPoint = 100;
        private const int DefaultSkillLv = 1;
        private const int ShortcutSlotCount = 6;

        private static bool? _hasArchive;

        public static bool HasArchive
        {
            get
            {
                if (_hasArchive == null) LoadArchive();
                return _hasArchive.Value;
            }
            private set => _hasArchive = value;
        }

        public static GameData GameData { get; private set; }

        private static void LoadArchive()
        {
            var saveItem = SaveSystem.GetSaveItem(0);
            if (saveItem != null)
            {
                _hasArchive = true;
                return;
            }

            var allSaves = SaveSystem.GetAllSaveItem();
            _hasArchive = allSaves != null && allSaves.Count > 0;
        }

        /// <summary>
        /// 创建新存档（当前项目单槽位，创建新档会清空旧档）
        /// </summary>
        public static void CreateArchive(int initCharacterId = 1001)
        {
            SaveSystem.DeleteAllSaveItem();
            SaveSystem.CreateSaveItem();
            _hasArchive = true;
            InitGameData(initCharacterId);
            SaveGameData();
        }

        /// <summary>
        /// 加载当前存档
        /// </summary>
        public static bool LoadCurrentArchive()
        {
            GameData = SaveSystem.LoadObject<GameData>();
            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] 读取存档失败，GameData 为空。");
                _hasArchive = false;
                return false;
            }

            bool dirty = EnsureGameDataContainers();
            if (dirty) SaveGameData();
            _hasArchive = true;
            return true;
        }

        public static void InitGameData(int initCharacterId)
        {
            GameData = new GameData
            {
                SelectedCharacterId = initCharacterId,
                UnlockedCharacterIds = new Serialized_List<int>(),
                CharacterTeam = new[] { initCharacterId, -1, -1, -1 },
                CharacterSkillsDict = new Serialized_Dic<int, SkillLearnedDatas>(),
                CharacterShortcutSkillsDict = new Serialized_Dic<int, ShortcutSkillSlotData>(),
            };

            GameData.UnlockedCharacterIds.List.Add(initCharacterId);
            GameData.CharacterSkillsDict.Dictionary[initCharacterId] = CreateDefaultSkillData();
            GameData.CharacterShortcutSkillsDict.Dictionary[initCharacterId] = CreateDefaultShortcutData();
        }

        public static void SaveGameData()
        {
            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] SaveGameData 失败：GameData 为空。");
                return;
            }

            SaveSystem.SaveObject(GameData);
            _hasArchive = true;
        }

        #region 角色管理

        /// <summary>
        /// 在读取角色配置后修复当前角色的技能与快捷栏数据
        /// 1. 移除越界技能索引
        /// 2. 修复非法等级
        /// 3. 修复快捷栏长度/无效技能/重复技能
        /// 4. 新角色默认补齐普攻，必要时补一个主动技能
        /// </summary>
        public static bool EnsureCurrentCharacterDataByConfig(CharacterConfig characterConfig, bool autoSave = true)
        {
            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] GameData 为空！请先初始化或加载存档！");
                return false;
            }

            bool dirty = EnsureGameDataContainers();
            int characterId = GameData.SelectedCharacterId;

            if (!GameData.UnlockedCharacterIds.List.Contains(characterId))
            {
                GameData.UnlockedCharacterIds.List.Add(characterId);
                dirty = true;
            }

            var skillDatas = GetOrCreateCharacterSkillData(characterId, ref dirty);
            var shortcutDatas = GetOrCreateCharacterShortcutData(characterId, ref dirty);

            if (characterConfig != null)
            {
                dirty |= RepairSkillDataByConfig(skillDatas, characterConfig);
                dirty |= RepairShortcutDataByConfig(shortcutDatas, skillDatas, characterConfig);
            }
            else
            {
                dirty |= RepairShortcutSlotData(shortcutDatas);
            }

            if (dirty && autoSave) SaveGameData();
            return true;
        }

        /// <summary>
        /// 获取当前角色的技能数据
        /// </summary>
        public static SkillLearnedDatas GetCurrentCharacterSkills()
        {
            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] GameData 为空！请先初始化存档！");
                return null;
            }

            bool dirty = EnsureGameDataContainers();
            var skillDatas = GetOrCreateCharacterSkillData(GameData.SelectedCharacterId, ref dirty);
            if (dirty) SaveGameData();
            return skillDatas;
        }

        /// <summary>
        /// 获取当前角色的快捷栏数据
        /// </summary>
        public static ShortcutSkillSlotData GetCurrentCharacterShortcutSkills()
        {
            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] GameData 为空！请先初始化存档！");
                return null;
            }

            bool dirty = EnsureGameDataContainers();
            var shortcuts = GetOrCreateCharacterShortcutData(GameData.SelectedCharacterId, ref dirty);
            dirty |= RepairShortcutSlotData(shortcuts);
            if (dirty) SaveGameData();
            return shortcuts;
        }

        /// <summary>
        /// 切换当前角色
        /// </summary>
        public static bool SwitchCharacter(int characterId)
        {
            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] SwitchCharacter 失败：GameData 为空。");
                return false;
            }

            bool dirty = EnsureGameDataContainers();
            if (!GameData.UnlockedCharacterIds.List.Contains(characterId))
            {
                JKLog.Warning($"[{nameof(DataManager)}] 角色ID {characterId} 未解锁！");
                return false;
            }

            GameData.SelectedCharacterId = characterId;
            GetOrCreateCharacterSkillData(characterId, ref dirty);
            GetOrCreateCharacterShortcutData(characterId, ref dirty);
            SaveGameData();
            return true;
        }

        /// <summary>
        /// 解锁新角色
        /// </summary>
        public static void UnlockCharacter(int characterId)
        {
            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] UnlockCharacter 失败：GameData 为空。");
                return;
            }

            bool dirty = EnsureGameDataContainers();

            if (!GameData.UnlockedCharacterIds.List.Contains(characterId))
            {
                GameData.UnlockedCharacterIds.List.Add(characterId);
                dirty = true;
            }

            GetOrCreateCharacterSkillData(characterId, ref dirty);
            GetOrCreateCharacterShortcutData(characterId, ref dirty);

            if (dirty)
            {
                SaveGameData();
                JKLog.Log($"[{nameof(DataManager)}] 解锁角色ID {characterId}");
            }
        }

        /// <summary>
        /// 检查角色是否已解锁
        /// </summary>
        public static bool IsCharacterUnlocked(int characterId)
        {
            return GameData != null
                   && GameData.UnlockedCharacterIds != null
                   && GameData.UnlockedCharacterIds.List != null
                   && GameData.UnlockedCharacterIds.List.Contains(characterId);
        }

        #endregion

        #region 角色成长（等级 / 经验 / 金币）

        /// <summary>
        /// 获取指定角色的成长数据（不存在则自动创建）。
        /// </summary>
        public static CharacterProgressData GetOrCreateProgressData(int characterId)
        {
            if (GameData == null) return null;
            EnsureGameDataContainers();

            if (!GameData.CharacterProgressDict.Dictionary.TryGetValue(characterId, out var data) || data == null)
            {
                data = new CharacterProgressData();
                GameData.CharacterProgressDict.Dictionary[characterId] = data;
            }
            return data;
        }

        /// <summary>
        /// 给指定角色增加经验，自动结算升级（可多级连升），自动存档。
        /// 返回最终到达的等级。
        /// </summary>
        public static int AddExperience(int characterId, long expGain, Config.LevelGrowthConfig growthConfig)
        {
            if (growthConfig == null || expGain <= 0) return GetOrCreateProgressData(characterId)?.Level ?? 1;

            var data = GetOrCreateProgressData(characterId);
            if (data == null) return 1;

            if (data.Level >= growthConfig.MaxLevel)
            {
                JKLog.Log($"[DataManager] 角色 {characterId} 已满级（{growthConfig.MaxLevel}），经验不再增加");
                return data.Level;
            }

            data.Experience += expGain;

            // 结算升级
            bool leveledUp = false;
            while (data.Level < growthConfig.MaxLevel)
            {
                long need = growthConfig.GetExpRequiredForNextLevel(data.Level);
                if (data.Experience < need) break;
                data.Experience -= need;
                data.Level++;
                leveledUp = true;
                JKLog.Log($"[DataManager] 角色 {characterId} 升级！当前等级: {data.Level}");
            }

            if (leveledUp)
                OnLevelUp?.Invoke(characterId, data.Level);

            SaveGameData();
            return data.Level;
        }

        /// <summary>
        /// 直接设置指定角色的等级（调试 / GM 指令），自动存档。
        /// </summary>
        public static void SetLevel(int characterId, int level, Config.LevelGrowthConfig growthConfig)
        {
            var data = GetOrCreateProgressData(characterId);
            if (data == null) return;

            int maxLv = growthConfig != null ? growthConfig.MaxLevel : 100;
            data.Level = Mathf.Clamp(level, 1, maxLv);
            data.Experience = 0;
            SaveGameData();
        }

        /// <summary>
        /// 增加或消耗金币（amount 为负值时消耗）。
        /// 金币不足时返回 false 且不扣除。
        /// </summary>
        public static bool AddGold(long amount)
        {
            if (GameData == null) return false;
            if (amount < 0 && GameData.Gold + amount < 0)
            {
                JKLog.Warning($"[DataManager] 金币不足，当前 {GameData.Gold}，需要 {-amount}");
                return false;
            }
            GameData.Gold += amount;
            SaveGameData();
            return true;
        }

        /// <summary>
        /// 角色升级时触发的事件（characterId, newLevel）。
        /// UI 层订阅此事件来播放升级特效、刷新属性面板等。
        /// </summary>
        public static event System.Action<int, int> OnLevelUp;

        #endregion

        #region Internal Repair

        private static bool EnsureGameDataContainers()
        {
            if (GameData == null) return false;

            bool dirty = false;

            if (GameData.UnlockedCharacterIds == null)
            {
                GameData.UnlockedCharacterIds = new Serialized_List<int>();
                dirty = true;
            }
            if (GameData.UnlockedCharacterIds.List == null)
            {
                GameData.UnlockedCharacterIds.List = new System.Collections.Generic.List<int>();
                dirty = true;
            }

            if (GameData.CharacterSkillsDict == null)
            {
                GameData.CharacterSkillsDict = new Serialized_Dic<int, SkillLearnedDatas>();
                dirty = true;
            }

            if (GameData.CharacterShortcutSkillsDict == null)
            {
                GameData.CharacterShortcutSkillsDict = new Serialized_Dic<int, ShortcutSkillSlotData>();
                dirty = true;
            }

            if (GameData.CharacterTeam == null || GameData.CharacterTeam.Length != 4)
            {
                int[] oldTeam = GameData.CharacterTeam;
                GameData.CharacterTeam = new[] { GameData.SelectedCharacterId, -1, -1, -1 };
                if (oldTeam != null)
                {
                    System.Array.Copy(oldTeam, GameData.CharacterTeam, System.Math.Min(oldTeam.Length, GameData.CharacterTeam.Length));
                }
                if (GameData.CharacterTeam[0] == -1)
                {
                    GameData.CharacterTeam[0] = GameData.SelectedCharacterId;
                }
                dirty = true;
            }

            // 向后兼容：旧存档没有成长字典时自动创建
            if (GameData.CharacterProgressDict == null)
            {
                GameData.CharacterProgressDict = new Serialized_Dic<int, CharacterProgressData>();
                dirty = true;
            }

            // 向后兼容：旧存档没有已清空区域列表时自动创建
            if (GameData.ClearedRegionKeys == null)
            {
                GameData.ClearedRegionKeys = new Serialized_List<string>();
                dirty = true;
            }

            // 向后兼容：旧存档没有背包字典时自动创建
            if (GameData.InventoryItems == null)
            {
                GameData.InventoryItems = new Serialized_Dic<int, int>();
                dirty = true;
            }

            return dirty;
        }

        private static SkillLearnedDatas GetOrCreateCharacterSkillData(int characterId, ref bool dirty)
        {
            if (!GameData.CharacterSkillsDict.Dictionary.TryGetValue(characterId, out var skillDatas) || skillDatas == null)
            {
                skillDatas = CreateDefaultSkillData();
                GameData.CharacterSkillsDict.Dictionary[characterId] = skillDatas;
                dirty = true;
            }

            if (skillDatas.SkillLearnedDataDict == null)
            {
                skillDatas.SkillLearnedDataDict = new Serialized_Dic<int, SkillLearnedData>();
                dirty = true;
            }

            if (skillDatas.SkillTotalPoint < 0)
            {
                skillDatas.SkillTotalPoint = 0;
                dirty = true;
            }

            var dict = skillDatas.SkillLearnedDataDict.Dictionary;
            if (dict.Count == 0)
            {
                dict[0] = new SkillLearnedData { lv = DefaultSkillLv };
                dirty = true;
            }

            var keys = new List<int>(dict.Keys);
            foreach (var key in keys)
            {
                if (dict[key] == null)
                {
                    dict[key] = new SkillLearnedData { lv = DefaultSkillLv };
                    dirty = true;
                }
                else if (dict[key].lv <= 0)
                {
                    dict[key].lv = DefaultSkillLv;
                    dirty = true;
                }
            }

            return skillDatas;
        }

        private static ShortcutSkillSlotData GetOrCreateCharacterShortcutData(int characterId, ref bool dirty)
        {
            if (!GameData.CharacterShortcutSkillsDict.Dictionary.TryGetValue(characterId, out var shortcutData) || shortcutData == null)
            {
                shortcutData = CreateDefaultShortcutData();
                GameData.CharacterShortcutSkillsDict.Dictionary[characterId] = shortcutData;
                dirty = true;
            }

            if (RepairShortcutSlotData(shortcutData))
            {
                dirty = true;
            }

            return shortcutData;
        }

        private static bool RepairSkillDataByConfig(SkillLearnedDatas skillDatas, CharacterConfig config)
        {
            bool dirty = false;
            var dict = skillDatas.SkillLearnedDataDict.Dictionary;
            int skillCount = config.SkillConfigList != null ? config.SkillConfigList.Count : 0;

            if (skillCount <= 0) return false;

            List<int> invalidKeys = new List<int>();
            foreach (var pair in dict)
            {
                if (pair.Key < 0 || pair.Key >= skillCount)
                {
                    invalidKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < invalidKeys.Count; i++)
            {
                dict.Remove(invalidKeys[i]);
                dirty = true;
            }

            if (!dict.ContainsKey(0))
            {
                dict[0] = new SkillLearnedData { lv = DefaultSkillLv };
                dirty = true;
            }

            var keys = new List<int>(dict.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int skillIndex = keys[i];
                var configItem = config.SkillConfigList[skillIndex];
                if (configItem == null)
                {
                    dict.Remove(skillIndex);
                    dirty = true;
                    continue;
                }

                var learnedData = dict[skillIndex];
                if (learnedData == null)
                {
                    dict[skillIndex] = new SkillLearnedData { lv = DefaultSkillLv };
                    dirty = true;
                    continue;
                }

                int maxLv = Mathf.Max(1, configItem.maxLv);
                int fixedLv = Mathf.Clamp(Mathf.Max(learnedData.lv, DefaultSkillLv), 1, maxLv);
                if (fixedLv != learnedData.lv)
                {
                    learnedData.lv = fixedLv;
                    dirty = true;
                }
            }

            // 新角色默认体验：若只有普攻，则自动补一个主动技能
            if (dict.Count <= 1)
            {
                int firstActiveIndex = FindFirstActiveSkillIndex(config);
                if (firstActiveIndex > 0 && !dict.ContainsKey(firstActiveIndex))
                {
                    dict[firstActiveIndex] = new SkillLearnedData { lv = DefaultSkillLv };
                    dirty = true;
                }
            }

            return dirty;
        }

        private static bool RepairShortcutDataByConfig(ShortcutSkillSlotData shortcutData, SkillLearnedDatas skillDatas, CharacterConfig config)
        {
            bool dirty = RepairShortcutSlotData(shortcutData);
            int skillCount = config.SkillConfigList != null ? config.SkillConfigList.Count : 0;
            if (skillCount <= 0) return dirty;

            var learned = skillDatas.SkillLearnedDataDict.Dictionary;
            HashSet<int> used = new HashSet<int>();

            for (int i = 0; i < shortcutData.skillIds.Length; i++)
            {
                int skillIndex = shortcutData.skillIds[i];
                if (skillIndex == -1) continue;

                bool valid = skillIndex >= 0
                             && skillIndex < skillCount
                             && learned.ContainsKey(skillIndex)
                             && config.SkillConfigList[skillIndex] != null
                             && config.SkillConfigList[skillIndex].canRelease;

                if (!valid || !used.Add(skillIndex))
                {
                    shortcutData.skillIds[i] = -1;
                    dirty = true;
                }
            }

            bool allEmpty = true;
            for (int i = 0; i < shortcutData.skillIds.Length; i++)
            {
                if (shortcutData.skillIds[i] != -1)
                {
                    allEmpty = false;
                    break;
                }
            }

            if (allEmpty)
            {
                List<int> learnedSkills = new List<int>(learned.Keys);
                learnedSkills.Sort();
                for (int i = 0; i < learnedSkills.Count; i++)
                {
                    int skillIndex = learnedSkills[i];
                    if (skillIndex <= 0 || skillIndex >= skillCount) continue;
                    var cfg = config.SkillConfigList[skillIndex];
                    if (cfg == null || !cfg.canRelease) continue;

                    int emptySlot = Array.IndexOf(shortcutData.skillIds, -1);
                    if (emptySlot < 0) break;
                    shortcutData.skillIds[emptySlot] = skillIndex;
                    dirty = true;
                }
            }

            return dirty;
        }

        private static bool RepairShortcutSlotData(ShortcutSkillSlotData shortcutData)
        {
            if (shortcutData == null) return false;

            if (shortcutData.skillIds == null || shortcutData.skillIds.Length != ShortcutSlotCount)
            {
                int[] old = shortcutData.skillIds;
                int[] fixedSlots = CreateEmptyShortcutSlots();
                if (old != null)
                {
                    Array.Copy(old, fixedSlots, Mathf.Min(old.Length, fixedSlots.Length));
                }
                shortcutData.skillIds = fixedSlots;
                return true;
            }

            bool dirty = false;
            for (int i = 0; i < shortcutData.skillIds.Length; i++)
            {
                if (shortcutData.skillIds[i] < -1)
                {
                    shortcutData.skillIds[i] = -1;
                    dirty = true;
                }
            }
            return dirty;
        }

        private static int FindFirstActiveSkillIndex(CharacterConfig config)
        {
            if (config?.SkillConfigList == null) return -1;

            for (int i = 1; i < config.SkillConfigList.Count; i++)
            {
                var cfg = config.SkillConfigList[i];
                if (cfg != null && cfg.canRelease)
                {
                    return i;
                }
            }

            return -1;
        }

        private static SkillLearnedDatas CreateDefaultSkillData()
        {
            var data = new SkillLearnedDatas
            {
                SkillTotalPoint = DefaultSkillPoint,
                SkillLearnedDataDict = new Serialized_Dic<int, SkillLearnedData>()
            };
            data.SkillLearnedDataDict.Dictionary[0] = new SkillLearnedData { lv = DefaultSkillLv };
            return data;
        }

        private static ShortcutSkillSlotData CreateDefaultShortcutData()
        {
            return new ShortcutSkillSlotData
            {
                skillIds = CreateEmptyShortcutSlots()
            };
        }

        private static int[] CreateEmptyShortcutSlots()
        {
            int[] slots = new int[ShortcutSlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = -1;
            }
            return slots;
        }

        #endregion
    }
}
