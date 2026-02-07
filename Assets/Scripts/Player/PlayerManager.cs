using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using Data;
using JKFrame;
using Manager;
using UI;
using UnityEngine;

namespace RayPlayer
{
    public class PlayerManager : SingletonMono<PlayerManager>
    {
        [SerializeField] public PlayerController player;
        public CharacterConfig characterConfig { get;private set; }

        /// <summary>
        /// 初始化玩家（使用角色ID）
        /// </summary>
        public async UniTask InitAsync()
        {
            int characterId = DataManager.GameData.SelectedCharacterId;
            // 1.从资源管理器加载角色配置
            characterConfig = await CharacterModelManager.Instance.LoadCharacterConfigAsync(characterId);
            if (characterConfig == null)
            {
                RayDebug.Error($"无法加载角色配置，ID: {characterId}");
                return;
            }
            
            // 2. 动态替换外观模型
            var newModel = await CharacterModelManager.Instance.ReplaceCharacterModelAsync(
                player.gameObject,
                characterId,
                "PlayerModel"  // 这里的名字要和你场景中的挂载点名称一致
            );
            
            if (newModel == null)
            {
                RayDebug.Error($"无法加载角色外观，ID: {characterId}");
                return;
            }

            var shortcutSkillDatas = DataManager.GetCurrentCharacterShortcutSkills();

            player.BindModel(newModel);
            player.Init(characterConfig, DataManager.GameData);
            InputManager.Instance.Init(true);
            UISystem.Show<UI_GameSceneMainWindow>().Show(shortcutSkillDatas);
        }

        // public void Init(GameData gameData)
        // {
        //     // 根据不同的职业获取不同的角色配置
        //     // CharacterConfig characterConfig = ResSystem.LoadAsset<CharacterConfig>(gameData.ProfessionType.ToString() + "Config");
        //     characterConfig = ResSystem.LoadAsset<CharacterConfig>("AnbiConfig");
        //     player.Init(characterConfig, gameData);
        //     InputManager.Instance.Init(true);
        //     UISystem.Show<UI_GameSceneMainWindow>().Show(gameData.ShortcutSkillSlotData);
        // }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) && UISystem.GetWindow<UI_SkillLearnWindow>() == null)
            {
                UISystem.Show<UI_SkillLearnWindow>().Init(DataManager.GetCurrentCharacterSkills());
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                UI_SkillWindow window = UISystem.GetWindow<UI_SkillWindow>();
                if (window == null || window.gameObject.activeInHierarchy == false)
                {
                    UISystem.Show<UI_SkillWindow>().Show(DataManager.GetCurrentCharacterSkills());
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