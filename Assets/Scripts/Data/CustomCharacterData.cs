using System;
using Serialization;

namespace Data
{
    /// <summary>
    /// 自定义角色的全部数据
    /// </summary>
    [Serializable]
    public class CustomCharacterData
    {
        public SerializableDictionary<int, CustomCharacterPartData> CustomPartDataDict;
    }

    /// <summary>
    /// 自定义角色部位的数据
    /// </summary>
    [Serializable]
    public class CustomCharacterPartData
    {
        public int Index;
        public float Size;
        public float Height;
        public SerializationColor Color1;
        public SerializationColor Color2;
    }
}
