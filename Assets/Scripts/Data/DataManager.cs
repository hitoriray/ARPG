using UnityEngine;
using Config;
using Serialization;

namespace Data
{
    /// <summary>
    /// 数据管理器
    /// </summary>
    public static class DataManager
    {
        public static CustomCharacterData CustomCharacterData;
        
        public static void InitCustomCharacterData()
        {
            CustomCharacterData = new CustomCharacterData();
            CustomCharacterData = new CustomCharacterData
            {
                CustomPartDataDict = new(3)
                {
                    { (int)CharacterPartType.Face, new CustomCharacterPartData { Index = 1, Size = 1, Height = 0, } },
                    { (int)CharacterPartType.Hair, new CustomCharacterPartData { Index = 1, Color1 = Color.white.ConvertToSerializationColor(), } },
                    { (int)CharacterPartType.Cloth, new CustomCharacterPartData { Index = 1, Color1 = Color.white.ConvertToSerializationColor(), Color2 = Color.black.ConvertToSerializationColor(), } }
                }
            };
        }
        
        
    }
}