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

        /// <summary>
        /// Unity MonoBehaviour 消息：Game 窗口获得焦点时触发（包括 Editor 内切换到 Game 窗口），重新强制执行鼠标状态
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyCursorState();
        }

        [SerializeField] public PlayerController player;
        [SerializeField] private GameObject cineMachine;
        [SerializeField] private CharacterModelManager characterModelManager;

        public CharacterConfig CharacterConfig { get; private set; }
        private InputService inputService;
        private Behaviour[] _cameraInputBehaviours;
        private bool characterControl = true;

        // UI 覆盖计数：任意 UI 打开 +1，关闭 -1；> 0 时强制显示鼠标
        private int _uiOverrideCount = 0;
        // Alt 键是否正在按下
        private bool _altPeeking = false;

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
            RayDebug.Log($"[PlayerManager] Loaded CharacterConfig={CharacterConfig.name}, GenericLocomotionConfig={CharacterConfig.GenericLocomotionConfig?.name}, PlayerSO={CharacterConfig.PlayerSO?.name}");
            
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

            // PlayerSkillInput 直接订阅 InputActionReference，不受 ActionMap 开关影响，需单独切换
            if (player != null && player.SkillInput != null)
                player.SkillInput.enabled = canControl;

            ApplyCursorState();

            EnsureCameraRigReference();
            SetCameraInputEnabled(canControl);
            if (cineMachine != null)
                cineMachine.SetActive(canControl);
        }

        /// <summary>
        /// UI 打开时调用，强制显示鼠标。
        /// </summary>
        public void PushUICursor()
        {
            _uiOverrideCount++;
            ApplyCursorState();
        }

        /// <summary>
        /// UI 关闭时调用，恢复鼠标状态。
        /// </summary>
        public void PopUICursor()
        {
            _uiOverrideCount = Mathf.Max(0, _uiOverrideCount - 1);
            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            bool show = _uiOverrideCount > 0 || _altPeeking || !characterControl;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = show;
        }

        private void Update()
        {
            if (inputService == null) return;

            // 仅在战斗控制中且没有 UI 覆盖时，检测 Alt 键
            if (!characterControl || _uiOverrideCount > 0) return;

            bool altDown = inputService.LeftAlt;
            if (altDown != _altPeeking)
            {
                _altPeeking = altDown;
            }
        }

        /// <summary>
        /// 在 LateUpdate 中强制写入鼠标状态，确保在 Cinemachine 等插件 Update 之后执行，防止被覆盖。
        /// </summary>
        private void LateUpdate()
        {
            ApplyCursorState();
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

        private void EnsureCameraRigReference()
        {
            if (cineMachine != null)
                return;

            var cameraController = FindAnyObjectByType<CameraController>(FindObjectsInactive.Include);
            if (cameraController != null)
            {
                cineMachine = cameraController.gameObject;
            }
        }

        private void SetCameraInputEnabled(bool enabled)
        {
            if (cineMachine == null)
                return;

            if (_cameraInputBehaviours == null || _cameraInputBehaviours.Length == 0)
            {
                var allBehaviours = cineMachine.GetComponentsInChildren<Behaviour>(true);
                var temp = new List<Behaviour>();
                for (int i = 0; i < allBehaviours.Length; i++)
                {
                    var behaviour = allBehaviours[i];
                    if (behaviour == null) continue;

                    string typeName = behaviour.GetType().Name;
                    if (typeName.Contains("CinemachineInputProvider") || typeName.Contains("InputAxisController"))
                    {
                        temp.Add(behaviour);
                    }
                }
                _cameraInputBehaviours = temp.ToArray();
            }

            if (_cameraInputBehaviours == null)
                return;

            for (int i = 0; i < _cameraInputBehaviours.Length; i++)
            {
                var behaviour = _cameraInputBehaviours[i];
                if (behaviour == null) continue;
                behaviour.enabled = enabled;
            }
        }

        public ICharacter GetCharacterController() => player;
        public CharacterConfig GetCharacterConfig() => CharacterConfig;
    }
}
