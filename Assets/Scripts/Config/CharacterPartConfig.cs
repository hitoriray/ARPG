using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 角色的部位类型
    /// </summary>
    public enum CharacterPartType
    {
        [LabelText("帽子")] Hat,
        [LabelText("头发")] Hair,
        [LabelText("脸部")] Face,
        [LabelText("上衣")] Cloth,
        [LabelText("肩部")] ShoulderPad,
        [LabelText("腰带")] Belt,
        [LabelText("手套")] Glove,
        [LabelText("鞋子")] Shoe,
    }

    /// <summary>
    /// 部位配置
    /// </summary>
    public abstract class CharacterPartConfigBase : ConfigBase
    {
        [LabelText("名称")] public string Name;
        [LabelText("支持的职业")] public List<ProfessionType> ProfessionTypes;
        [LabelText("部位")] public CharacterPartType CharacterPartType;
        [LabelText("主网格")] public Mesh Mesh1;
    }
}