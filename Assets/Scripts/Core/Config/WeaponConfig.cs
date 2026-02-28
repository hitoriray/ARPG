using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 武器配置表（用于编辑器和运行时）
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Config/WeaponConfig")]
    public class WeaponConfig : ConfigBase
    {
        [LabelText("所有武器列表")]
        [InfoBox("这里配置所有角色可能使用的武器名称，用于技能编辑器的下拉选择")]
        public List<WeaponEntry> Weapons = new List<WeaponEntry>();

        /// <summary>
        /// 获取所有武器名称（用于下拉列表）
        /// </summary>
        public List<string> GetAllWeaponNames()
        {
            List<string> names = new List<string>();
            foreach (var weapon in Weapons)
            {
                names.Add(weapon.WeaponName);
            }
            return names;
        }

        /// <summary>
        /// 根据武器名称查找
        /// </summary>
        public WeaponEntry GetWeaponByName(string weaponName)
        {
            return Weapons.Find(w => w.WeaponName == weaponName);
        }
    }

    [System.Serializable]
    public class WeaponEntry
    {
        [LabelText("武器名称")]
        [Tooltip("必须和角色模型上的武器骨骼/节点名称一致")]
        public string WeaponName;

        // [LabelText("武器描述")]
        // [TextArea(1, 3)]
        // public string Description;

        // [LabelText("所属角色")]
        // [Tooltip("哪些角色使用这个武器")]
        // public List<string> OwnerCharacters = new List<string>();

        // [LabelText("武器预览图")]
        // [PreviewField(50)]
        // public Sprite Icon;
    }
}
