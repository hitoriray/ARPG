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
        static DataManager()
        {
            LoadArchive();
        }

        public static bool HasArchive { get; private set; }

        private static void LoadArchive()
        {
            var saveItem = SaveSystem.GetSaveItem(0);
            HasArchive = saveItem != null;
        }

        /// <summary>
        /// 创建新存档
        /// </summary>
        public static void CreateArchive()
        {
            if (HasArchive)
            {
                SaveSystem.DeleteAllSaveItem();
            }
            SaveSystem.CreateSaveItem();
            
            // 初始化角色外观数据
            InitCustomCharacterData();
            SaveCustomCharacterData();
        }

        public static void LoadCurrentArchive()
        {
            GameData = SaveSystem.LoadObject<GameData>();
        }

        #region 玩家数据

        public static GameData GameData { get; private set; }
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
            GameData.SkillLearnedDatas.SkillLearnedDataDict.Dictionary.Add(1, new SkillLearnedData(){lv=2});
            GameData.SkillLearnedDatas.SkillLearnedDataDict.Dictionary.Add(2, new SkillLearnedData(){lv=3});
        }

        public static void SaveCustomCharacterData()
        {
            SaveSystem.SaveObject(GameData);
        }

        #endregion
    }
}