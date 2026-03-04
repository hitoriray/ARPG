using System;
using System.Collections.Generic;
using UnityEngine;

namespace Config
{
    /// <summary>
    /// 酒馆 NPC 角色卡（SillyTavern 风格精简字段）
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterCard", menuName = "Config/AI/Character Card")]
    public class TavernCharacterCard : ScriptableObject
    {
        [Header("Basic")]
        [SerializeField] private string npcId = "tavern_default";
        [SerializeField] private string characterName = "酒馆NPC";
        [SerializeField] private Sprite icon;
        [TextArea(2, 6)]
        [SerializeField] private string description = "";
        [TextArea(2, 6)]
        [SerializeField] private string personality = "";
        [TextArea(2, 6)]
        [SerializeField] private string scenario = "";

        [Header("Dialog")]
        [TextArea(2, 8)]
        [SerializeField] private string firstMessage = "";
        [TextArea(2, 10)]
        [SerializeField] private string exampleDialogue = "";

        [Header("Lorebook")]
        [SerializeField] private List<LoreEntry> loreEntries = new();

        public string NpcId => string.IsNullOrWhiteSpace(npcId) ? "tavern_default" : npcId.Trim();
        public string CharacterName => string.IsNullOrWhiteSpace(characterName) ? "酒馆NPC" : characterName.Trim();
        public Sprite Icon => icon;
        public string Description => description;
        public string Personality => personality;
        public string Scenario => scenario;
        public string FirstMessage => firstMessage;
        public string ExampleDialogue => exampleDialogue;
        public List<LoreEntry> LoreEntries => loreEntries;
    }

    [Serializable]
    public class LoreEntry
    {
        [SerializeField] private string key = "";
        [TextArea(1, 6)]
        [SerializeField] private string content = "";

        public string Key => key;
        public string Content => content;
    }
}
