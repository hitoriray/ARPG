using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using Data;
using JKFrame;
using RayPlayer;
using UI;
using UnityEngine;

namespace Manager
{
    public class PlayerManager : SingletonMono<PlayerManager>, IPlayerManager
    {
        protected override void Awake()
        {
            base.Awake();
            PlayerService.Instance = this;
        }

        [SerializeField] public PlayerController player;
        [SerializeField] private GameObject cineMachine;
        [SerializeField] private CharacterModelManager characterModelManager;

        public CharacterConfig CharacterConfig { get; private set; }
        private InputService inputService;
        private bool characterControl = true;

        /// <summary>
        /// 统一管理角色输入开关（InputSystem）
        /// </summary>
        public bool CharacterControl
        {
            get => characterControl;
            set => SetCharacterControl(value);
        }

        /// <summary>
        /// 初始化玩家（使用角色ID）
        /// </summary>
        public async UniTask InitAsync()
        {
            var modelManager = GetCharacterModelManager();
            if (modelManager == null)
            {
                await UniTask.Yield();
                modelManager = GetCharacterModelManager();
            }

            if (modelManager == null)
            {
                RayDebug.Error("CharacterModelManager 未初始化，无法加载角色配置与模型。请检查场景中是否存在 CharacterModelManager。");
                return;
            }

            int characterId = DataManager.GameData.SelectedCharacterId;
            // 1.从资源管理器加载角色配置
            CharacterConfig = await modelManager.LoadCharacterConfigAsync(characterId);
            if (CharacterConfig == null)
            {
                RayDebug.Error($"无法加载角色配置，ID: {characterId}");
                return;
            }
            
            // 2. 动态替换外观模型
            var newModel = await modelManager.ReplaceCharacterModelAsync(
                player.gameObject,
                characterId,
                "PlayerModel"  // 这里的名字要和你场景中的挂载点名称一致
            );
            
            if (newModel == null)
            {
                RayDebug.Error($"无法加载角色外观，ID: {characterId}");
                return;
            }

            // 角色配置到位后，修复/迁移当前角色的技能与快捷栏数据
            DataManager.EnsureCurrentCharacterDataByConfig(CharacterConfig);
            var shortcutSkillDatas = DataManager.GetCurrentCharacterShortcutSkills();

            player.BindModel(newModel);
            player.Init(CharacterConfig, DataManager.GameData);
            inputService = InputService.Instance;
            SetCharacterControl(true);
            UISystem.Show<UI_GameSceneMainWindow>().Show(shortcutSkillDatas);
        }

        public List<SkillConfig> GetAllSkillConfig()
        {
            return CharacterConfig.SkillConfigList;
        }

        public void AddSkill(int skillIndex, SkillLearnedData skillLearnedData)
        {
            player.SkillBrain.AddSkill(player, GetAllSkillConfig(), skillIndex, skillLearnedData);
        }

        public void SetCharacterControl(bool canControl)
        {
            characterControl = canControl;
            inputService ??= InputService.Instance;

            if (inputService != null && inputService.inputMap != null)
            {
                if (canControl)
                    inputService.inputMap.Player.Enable();
                else
                    inputService.inputMap.Player.Disable();
            }

            Cursor.lockState = canControl ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !canControl;

            if (cineMachine != null)
                cineMachine.SetActive(canControl);
        }

        private CharacterModelManager GetCharacterModelManager()
        {
            if (characterModelManager != null)
                return characterModelManager;

            characterModelManager = CharacterModelManager.Instance;
            if (characterModelManager == null)
                characterModelManager = FindAnyObjectByType<CharacterModelManager>();

            return characterModelManager;
        }

        public ICharacter GetCharacterController() => player;
        public CharacterConfig GetCharacterConfig() => CharacterConfig;
    }
}
