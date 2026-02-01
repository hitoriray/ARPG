using System;
using System.Collections.Generic;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Config
{
    /// <summary>
    /// 角色资源配置表（单例模式）
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterTable", menuName = "Config/CharacterTable")]
    public class CharacterTable : ConfigBase
    {
        [LabelText("角色列表")]
        // [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
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
        [LabelText("角色ID")]
        public int CharacterId;
        [LabelText("角色名称")]
        public string CharacterName;
        [LabelText("角色Icon")]
        public Sprite CharacterIcon;
        [LabelText("角色模型预制体"), AssetSelector(Paths = "Assets/Prefabs/Characters")]
        public AssetReference CharacterModelPrefab;
        [LabelText("角色配置"), AssetSelector(Paths = "Assets/Config")]
        public AssetReference CharacterConfig;
        [LabelText("预估占用内存(MB)"), Range(1, 200)]
        public int MemoryCost = 50;
        [LabelText("是否为玩家可控角色")]
        public bool IsPlayable;
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