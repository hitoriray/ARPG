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
            CustomCharacterData = SaveSystem.LoadObject<CustomCharacterData>();
        }

        #region 玩家数据

        public static CustomCharacterData CustomCharacterData { get; private set; }
        public static void InitCustomCharacterData()
        {
            CustomCharacterData = new CustomCharacterData();
            CustomCharacterData.CustomPartDataDict = new Serialized_Dic<int, CustomCharacterPartData>();
            CustomCharacterData.CustomPartDataDict.Dictionary.Add((int)CharacterPartType.Face, 
                new CustomCharacterPartData { Index = 1, Size = 1, Height = 0, } );
            CustomCharacterData.CustomPartDataDict.Dictionary.Add((int)CharacterPartType.Hair,
                new CustomCharacterPartData { Index = 1, Color1 = Color.white.ConverToSerializationColor(), });
            CustomCharacterData.CustomPartDataDict.Dictionary.Add((int)CharacterPartType.Cloth,
                new CustomCharacterPartData
                {
                    Index = 1, Color1 = Color.white.ConverToSerializationColor(),
                    Color2 = Color.black.ConverToSerializationColor(),
                });
            CustomCharacterData.SkillLearnedDatas = new()
            {
                SkillTotalPoint = 1000,
            };
        }

        public static void SaveCustomCharacterData()
        {
            SaveSystem.SaveObject(CustomCharacterData);
        }

        #endregion
    }
}