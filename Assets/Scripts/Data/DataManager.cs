using UnityEngine;
using Config;
using JKFrame;
using Serialization;

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
            var saveItem = SaveManager.GetSaveItem(0);
            HasArchive = saveItem != null;
        }

        /// <summary>
        /// 创建新存档
        /// </summary>
        public static void CreateArchive()
        {
            if (HasArchive)
            {
                SaveManager.DeleteAllSaveItems();
            }
            SaveManager.CreateSaveItem();
            
            // 初始化角色外观数据
            InitCustomCharacterData();
            SaveCustomCharacterData();
        }

        public static void LoadCurrentArchive()
        {
            CustomCharacterData = SaveManager.LoadObject<CustomCharacterData>();
        }

        #region 玩家数据

        public static CustomCharacterData CustomCharacterData;
        public static void InitCustomCharacterData()
        {
            CustomCharacterData = new CustomCharacterData();
            CustomCharacterData.CustomPartDataDict = new SerializableDictionary<int, CustomCharacterPartData>();
            CustomCharacterData.CustomPartDataDict.Add((int)CharacterPartType.Face, 
                new CustomCharacterPartData { Index = 1, Size = 1, Height = 0, } );
            CustomCharacterData.CustomPartDataDict.Add((int)CharacterPartType.Hair,
                new CustomCharacterPartData { Index = 1, Color1 = Color.white.ConvertToSerializationColor(), });
            CustomCharacterData.CustomPartDataDict.Add((int)CharacterPartType.Cloth,
                new CustomCharacterPartData
                {
                    Index = 1, Color1 = Color.white.ConvertToSerializationColor(),
                    Color2 = Color.black.ConvertToSerializationColor(),
                });
        }

        public static void SaveCustomCharacterData()
        {
            SaveManager.SaveObject(CustomCharacterData);
        }

        #endregion
    }
}