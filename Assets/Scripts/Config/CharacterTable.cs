using System;
using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Config
{
    /// <summary>
    /// 角色资源配置表
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterTable", menuName = "Config/CharacterTable")]
    public class CharacterTable : ConfigBase
    {
        [LabelText("角色列表")]
        [Searchable]
        [ListDrawerSettings(NumberOfItemsPerPage = 20, IsReadOnly = false, ShowFoldout = false)]
        public List<CharacterEntry> Characters = new();

        public CharacterEntry GetCharacterById(int characterId)
        {
            return Characters.Find(x => x.CharacterId == characterId);
        }

        public CharacterEntry GetCharacterByName(string characterName)
        {
            return Characters.Find(x => x.CharacterName == characterName);
        }
    }

    [Serializable]
    public class CharacterEntry
    {
        [FoldoutGroup("$CharacterName")]
        [LabelText("角色ID"), DisplayAsString]
        public int CharacterId;
        
        [FoldoutGroup("$CharacterName")]
        [LabelText("角色名称")]
        public string CharacterName;
        
        [FoldoutGroup("$CharacterName")]
        [HorizontalGroup("$CharacterName/Split", Width = 0.3f)] // 在折叠组内水平分栏
        [VerticalGroup("$CharacterName/Split/Left")]
        [PreviewField(Height = 60)]
        [LabelText("角色Icon")]
        public AssetReferenceSprite CharacterIcon;
        
        [VerticalGroup("$CharacterName/Split/Right")]
        [LabelText("角色模型预制体")]
        public AssetReferenceGameObject CharacterModelPrefab;
        
        [VerticalGroup("$CharacterName/Split/Right")]
        [LabelText("角色配置")]
        public AssetReference CharacterConfig;
        
        [FoldoutGroup("$CharacterName/详细属性")] // 在主折叠组里再嵌套一个子折叠组
        [LabelText("预估占用内存(MB)"), Range(1, 200)]
        public int MemoryCost = 50;
        
        [FoldoutGroup("$CharacterName/详细属性")]
        [LabelText("是否为玩家可控角色")]
        public bool IsPlayable;
        
        [FoldoutGroup("$CharacterName/详细属性")]
        [LabelText("角色类型")]
        public CharacterType CharacterType = CharacterType.Player;
    }

    public enum CharacterType
    {
        Player,
        Boss,
        Elite,
        Minion,
    }
}