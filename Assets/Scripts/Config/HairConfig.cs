using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 头发配置
    /// </summary>
    [CreateAssetMenu(fileName = "HairConfig_", menuName = "Config/HairConfig")]
    public class HairConfig : CharacterPartConfigBase
    {
        /// <summary>
        /// 颜色索引, -1意味着无效
        /// </summary>
        [LabelText("颜色Index")] public int ColorIndex;
        [LabelText("半头网格")] public Mesh Mesh2;
    }
}