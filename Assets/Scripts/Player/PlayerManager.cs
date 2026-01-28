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

        public void Init(GameData gameData)
        {
            // 根据不同的职业获取不同的角色配置
            // CharacterConfig characterConfig = ResSystem.LoadAsset<CharacterConfig>(gameData.ProfessionType.ToString() + "Config");
            characterConfig = ResSystem.LoadAsset<CharacterConfig>("AnbiConfig");
            player.Init(characterConfig, gameData);
            InputManager.Instance.Init(true);
            UISystem.Show<UI_GameSceneMainWindow>().Show(gameData.ShortcutSkillSlotData);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) && UISystem.GetWindow<UI_SkillLearnWindow>() == null)
            {
                UISystem.Show<UI_SkillLearnWindow>().Init(DataManager.GameData.SkillLearnedDatas);
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                UI_SkillWindow window = UISystem.GetWindow<UI_SkillWindow>();
                if (window == null || window.gameObject.activeInHierarchy == false)
                {
                    UISystem.Show<UI_SkillWindow>().Show(DataManager.GameData.SkillLearnedDatas);
                }
                else
                {
                    UISystem.Close<UI_SkillWindow>();
                }
            }
        }

        public List<SkillConfig> GetAllSkillConfig()
        {
            return characterConfig.SkillConfigList;
        }

        public void AddSkill(int skillIndex, SkillLearnedData skillLearnedData)
        {
            player.SkillBrain.AddSkill(player, GetAllSkillConfig(), skillIndex, skillLearnedData);
        }
    }
}