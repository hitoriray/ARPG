using System;
using System.Collections.Generic;
using UnityEngine;
using Config;

namespace Data
{
    /// <summary>
    /// 自定义角色的全部数据
    /// </summary>
    [Serializable]
    public class CustomCharacterData
    {
        // TODO:字典结构和Color都不支持序列化
        public Dictionary<int, CustomCharacterPartData> CustomPartDataDict;
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
        public Color Color1;
        public Color Color2;
    }
}
