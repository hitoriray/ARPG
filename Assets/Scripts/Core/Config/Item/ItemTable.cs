using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 物品资源配置总表，用于根据 ItemId 获取具体的 ItemConfig
    /// </summary>
    [CreateAssetMenu(fileName = "ItemTable", menuName = "Config/Item/ItemTable")]
    public class ItemTable : ConfigBase
    {
        [LabelText("所有物品配置列表")]
        [Searchable]
        [ListDrawerSettings(NumberOfItemsPerPage = 20, IsReadOnly = false, ShowFoldout = false)]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        public List<ItemConfig> Items = new();

        public ItemConfig GetItemById(int itemId)
        {
            return Items.Find(x => x != null && x.ItemId == itemId);
        }
    }
}
