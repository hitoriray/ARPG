using System;
using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using UI;
using UnityEngine;

namespace Player
{
    public class PlayerManager : SingletonMono<PlayerManager>
    {
        [SerializeField] public PlayerController player;
        public CharacterConfig characterConfig { get;private set; }

        public void Init(CustomCharacterData characterData)
        {
            // 根据不同的职业获取不同的角色配置
            // CharacterConfig characterConfig = ResSystem.LoadAsset<CharacterConfig>(characterData.ProfessionType.ToString() + "Config");
            characterConfig = ResSystem.LoadAsset<CharacterConfig>("AnbiConfig");
            player.Init(characterConfig, characterData);
            Cursor.lockState = CursorLockMode.Locked; // 锁定鼠标
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) && UISystem.GetWindow<UI_SkillLearnWindow>() == null)
            {
                UISystem.Show<UI_SkillLearnWindow>().Init(DataManager.CustomCharacterData.SkillLearnedDatas);
            }
        }

        public List<SkillConfig> GetAllSkillConfig()
        {
            return characterConfig.SkillConfigList;
        }
    }
}