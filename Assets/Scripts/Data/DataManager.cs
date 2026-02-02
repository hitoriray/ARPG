using UnityEngine;
using Config;
using JKFrame;

namespace Data
{
    /// <summary>
    /// 数据管理器
    /// </summary>
    public static class DataManager
    {
        // static DataManager() { } // 移除静态构造函数，避免初始化顺序问题

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

        private static void LoadArchive()
        {
            var saveItem = SaveSystem.GetSaveItem(0);
            _hasArchive = saveItem != null;
        }

        /// <summary>
        /// 创建新存档
        /// </summary>
        public static void CreateArchive(int initCharacterId = 1001)
        {
            if (HasArchive)
            {
                SaveSystem.DeleteAllSaveItem();
            }
            SaveSystem.CreateSaveItem();
            _hasArchive = true; // 手动更新状态
            
            // 初始化角色外观数据
            // InitCustomCharacterData();
            // 使用新版初始化方式
            InitGameData(initCharacterId);
            SaveGameData();
        }

        public static void LoadCurrentArchive()
        {
            GameData = SaveSystem.LoadObject<GameData>();
        }

        #region 玩家数据

        public static GameData GameData { get; private set; }

        public static void InitGameData(int initCharacterId)
        {
            GameData = new GameData
            {
                SelectedCharacterId = initCharacterId,
                UnlockedCharacterIds = new Serialized_List<int>(),
                CharacterTeam = new int[4] { initCharacterId, -1, -1, -1 },
                CharacterSkillsDict = new Serialized_Dic<int, SkillLearnedDatas>(),
                CharacterShortcutSkillsDict = new Serialized_Dic<int, ShortcutSkillSlotData>(),
            };
            // 初始化解锁角色列表
            GameData.UnlockedCharacterIds.List.Add(initCharacterId);
            // 初始化技能学习数据
            var skillDatas = new SkillLearnedDatas()
            {
                SkillTotalPoint = 100,
                SkillLearnedDataDict = new Serialized_Dic<int, SkillLearnedData>()
            };
            skillDatas.SkillLearnedDataDict.Dictionary.Add(0, new SkillLearnedData { lv = 1 });
            GameData.CharacterSkillsDict.Dictionary.Add(initCharacterId, skillDatas);
            // 初始化技能快捷栏
            var shortcutData = new ShortcutSkillSlotData()
            {
                skillIds = new int[6] { -1, -1, -1, -1, -1, -1 }
            };
            GameData.CharacterShortcutSkillsDict.Dictionary.Add(initCharacterId, shortcutData);
        }
        
        // 旧版初始化（旧版捏脸系统）
        /*
        public static void InitCustomCharacterData()
        {
            GameData = new GameData();
            GameData.CustomPartDataDict = new Serialized_Dic<int, CustomCharacterPartData>();
            GameData.CustomPartDataDict.Dictionary.Add((int)CharacterPartType.Face, 
                new CustomCharacterPartData { Index = 1, Size = 1, Height = 0, } );
            GameData.CustomPartDataDict.Dictionary.Add((int)CharacterPartType.Hair,
                new CustomCharacterPartData { Index = 1, Color1 = Color.white.ConverToSerializationColor(), });
            GameData.CustomPartDataDict.Dictionary.Add((int)CharacterPartType.Cloth,
                new CustomCharacterPartData
                {
                    Index = 1, Color1 = Color.white.ConverToSerializationColor(),
                    Color2 = Color.black.ConverToSerializationColor(),
                });
            GameData.SkillLearnedDatas = new()
            {
                SkillTotalPoint = 1000,
            };
            GameData.SkillLearnedDatas.SkillLearnedDataDict.Dictionary.Add(0, new SkillLearnedData(){lv=1});
            GameData.ShortcutSkillSlotData = new();
            GameData.ShortcutSkillSlotData.skillIds = new int[6] { -1, -1, -1, -1, -1, -1 };
        }
        */
        
        public static void SaveGameData()
        {
            SaveSystem.SaveObject(GameData);
        }

        #endregion
        
        #region 角色管理
        /// <summary>
        /// 获取当前角色的技能数据
        /// </summary>
        public static SkillLearnedDatas GetCurrentCharacterSkills()
        {
            if (GameData?.CharacterSkillsDict?.Dictionary != null &&
                GameData.CharacterSkillsDict.Dictionary.TryGetValue(GameData.SelectedCharacterId, out var skillDatas))
            {
                return skillDatas;
            }

            if (GameData == null)
            {
                JKLog.Error($"[{nameof(DataManager)}] GameData 为空！请先初始化存档！");
                return null;
            }

            // 创建新的默认数据
            JKLog.Warning($"[{nameof(DataManager)}] 角色ID {GameData.SelectedCharacterId} 没有技能数据，创建默认数据...");
            var newSkillDatas = new SkillLearnedDatas
            {
                SkillTotalPoint = 100,
                SkillLearnedDataDict = new Serialized_Dic<int, SkillLearnedData>()
            };
            newSkillDatas.SkillLearnedDataDict.Dictionary.Add(0, new SkillLearnedData { lv = 1 });
            GameData.CharacterSkillsDict.Dictionary.Add(GameData.SelectedCharacterId, newSkillDatas);
            return newSkillDatas;
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
            
            if (GameData.CharacterShortcutSkillsDict.Dictionary.TryGetValue(GameData.SelectedCharacterId, out var shortcuts))
            {
                return shortcuts;
            }


            // 创建新的默认数据
            JKLog.Warning($"[{nameof(DataManager)}] 角色ID {GameData.SelectedCharacterId} 没有快捷栏数据，创建默认数据...");
            var newShortcuts = new ShortcutSkillSlotData
            {
                skillIds = new int[6] { -1, -1, -1, -1, -1, -1 }
            };
            GameData.CharacterShortcutSkillsDict.Dictionary.Add(GameData.SelectedCharacterId, newShortcuts);
            return newShortcuts;
        }
        
        /// <summary>
        /// 切换当前角色
        /// </summary>
        public static bool SwitchCharacter(int characterId)
        {
            if (!GameData.UnlockedCharacterIds.List.Contains(characterId))
            {
                JKLog.Warning($"[{nameof(DataManager)}] 角色ID {characterId} 未解锁！");
                return false;
            }

            GameData.SelectedCharacterId = characterId;
            SaveGameData();
            return true;
        }
        
        /// <summary>
        /// 解锁新角色
        /// </summary>
        public static void UnlockCharacter(int characterId)
        {
            if (!GameData.UnlockedCharacterIds.List.Contains(characterId))
            {
                GameData.UnlockedCharacterIds.List.Add(characterId);

                // 初始化该角色的技能和快捷栏数据
                if (!GameData.CharacterSkillsDict.Dictionary.ContainsKey(characterId))
                {
                    var skillData = new SkillLearnedDatas
                    {
                        SkillTotalPoint = 1000,
                        SkillLearnedDataDict = new Serialized_Dic<int, SkillLearnedData>()
                    };
                    GameData.CharacterSkillsDict.Dictionary.Add(characterId, skillData);
                }

                if (!GameData.CharacterShortcutSkillsDict.Dictionary.ContainsKey(characterId))
                {
                    var shortcutData = new ShortcutSkillSlotData
                    {
                        skillIds = new int[6] { -1, -1, -1, -1, -1, -1 }
                    };
                    GameData.CharacterShortcutSkillsDict.Dictionary.Add(characterId, shortcutData);
                }

                SaveGameData();
                JKLog.Log($"[{nameof(DataManager)}] 解锁角色ID {characterId}");
            }
        }
        
        /// <summary>
        /// 检查角色是否已解锁
        /// </summary>
        public static bool IsCharacterUnlocked(int characterId)
        {
            return GameData.UnlockedCharacterIds.List.Contains(characterId);
        }
        
        #endregion
    }
}