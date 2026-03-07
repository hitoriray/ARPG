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
    public class PlayerManager : MonoSingleton<PlayerManager>, IPlayerManager
    {
        private static bool _hasLastKnownPlayerWorldPosition;
        private static Vector3 _lastKnownPlayerWorldPosition;

        protected override void Awake()
        {
            base.Awake();
            // Avoid stale service binding when a duplicate PlayerManager component is destroyed by MonoSingleton.
            if (instance == this)
            {
                PlayerService.Instance = this;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // If service still points to this instance (or destroyed duplicate), recover to current singleton instance.
            if (ReferenceEquals(PlayerService.Instance, this))
            {
                PlayerService.Instance = instance;
            }
        }

        public static bool TryGetLatestPlayerWorldPosition(out Vector3 worldPosition)
        {
            // Use cached singleton field directly to avoid auto-creating placeholder during teardown.
            var mgr = instance;
            if (mgr != null && mgr.TryGetCurrentPlayerWorldPosition(out worldPosition))
            {
                _lastKnownPlayerWorldPosition = worldPosition;
                _hasLastKnownPlayerWorldPosition = true;
                return true;
            }

            if (_hasLastKnownPlayerWorldPosition)
            {
                worldPosition = _lastKnownPlayerWorldPosition;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
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
        public bool IsRuntimeInitialized => player != null && CharacterConfig != null && _loadedCharacterId > 0;
        private InputService inputService;
        private Behaviour[] _cameraInputBehaviours;
        private bool characterControl = true;
        private int _loadedCharacterId = -1;

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
            if (DataManager.GameData == null)
            {
                RayDebug.Error("[PlayerManager] GameData is null, cannot initialize player.");
                return;
            }

            if (player == null)
            {
                RayDebug.Error("[PlayerManager] player reference is null. Ensure PlayerManager in scene has PlayerController assigned.");
                return;
            }

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
            player.Init(CharacterConfig);
            player.RefreshSceneBindings();
            inputService = InputService.Instance;
            SetCharacterControl(true);
            ShowOrRefreshMainWindow(shortcutSkillDatas);
            _loadedCharacterId = characterId;

            TryRestorePlayerPositionForCurrentScene();

            // ===== 存档调试输出 =====
            PrintGameDataDebug();
        }

        /// <summary>
        /// 跨场景进入时确保玩家可用：仅在必要时完整重建，其余情况只刷新场景引用。
        /// </summary>
        public async UniTask EnsureInitializedAsync()
        {
            int selectedCharacterId = DataManager.GameData != null ? DataManager.GameData.SelectedCharacterId : -1;
            bool needFullInit = !IsRuntimeInitialized || selectedCharacterId != _loadedCharacterId;

            if (needFullInit)
            {
                await InitAsync();
                return;
            }

            player.RefreshSceneBindings();
            inputService = InputService.Instance;
            SetCharacterControl(characterControl);
            TryRestorePlayerPositionForCurrentScene();

            var shortcutSkillDatas = DataManager.GetCurrentCharacterShortcutSkills();
            ShowOrRefreshMainWindow(shortcutSkillDatas);
        }

        private void TryRestorePlayerPositionForCurrentScene()
        {
            if (player == null) return;

            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var savedPos = DataManager.GetPlayerLastPosition(currentScene);
            if (!savedPos.HasValue)
            {
                var gd = DataManager.GameData;
                Vector3 rawPos = gd != null ? (Vector3)gd.PlayerLastPosition : Vector3.zero;
                string rawScene = gd != null ? gd.PlayerLastSceneName : "<GameData null>";
                RayDebug.Warn($"[PlayerManager] 未应用存档位置：currentScene={currentScene}, savedScene={rawScene}, savedPos={rawPos}");
                return;
            }

            var controller = player.controller;
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
                controller.enabled = false;

            // PlayerController is the actual moving object; move manager by delta to keep parent-child relation consistent.
            Vector3 currentPlayerPos = player.transform.position;
            Vector3 delta = savedPos.Value - currentPlayerPos;
            if (player.transform.parent == transform)
                transform.position += delta;
            else
                player.transform.position = savedPos.Value;

            player.ChangeVerticalSpeed(0f);
            player.ClearHorizontalVelocity();

            if (controllerWasEnabled)
                controller.enabled = true;

            CacheLatestPlayerWorldPosition();
            RayDebug.Log($"[PlayerManager] 从存档位置生成: target={savedPos.Value}, managerPos={transform.position}, playerPos={player.transform.position}");
        }

        private void ShowOrRefreshMainWindow(ShortcutSkillSlotData shortcutSkillDatas)
        {
            if (shortcutSkillDatas == null)
            {
                shortcutSkillDatas = DataManager.GetCurrentCharacterShortcutSkills();
            }

            if (shortcutSkillDatas == null) return;

            var mainWindow = UISystem.GetWindow<UI_GameSceneMainWindow>();
            if (mainWindow != null && mainWindow.UIEnable)
            {
                mainWindow.ShowShortcutSkillSlots(shortcutSkillDatas);
                return;
            }

            UISystem.Show<UI_GameSceneMainWindow>()?.Show(shortcutSkillDatas);
        }

        private void PrintGameDataDebug()
        {
            var sb = new System.Text.StringBuilder();
            var gd = DataManager.GameData;
            if (gd == null)
            {
                RayDebug.Info("[GameData] 存档为空");
                return;
            }

            sb.AppendLine("========== 当前存档数据 ==========");
            sb.AppendLine($"  当前角色ID: {gd.SelectedCharacterId}");
            sb.AppendLine($"  金币: {gd.Gold}");

            // --- 已解锁角色 ---
            sb.AppendLine("  [已解锁角色]");
            if (gd.UnlockedCharacterIds?.List != null)
                foreach (var id in gd.UnlockedCharacterIds.List)
                    sb.AppendLine($"    角色ID: {id}");

            // --- 队伍 ---
            sb.Append("  [队伍] ");
            if (gd.CharacterTeam != null)
                sb.AppendLine(string.Join(", ", gd.CharacterTeam));
            else
                sb.AppendLine("null");

            // --- 角色技能 ---
            sb.AppendLine("  [角色技能列表]");
            if (gd.CharacterSkillsDict?.Dictionary != null)
            {
                foreach (var kv in gd.CharacterSkillsDict.Dictionary)
                {
                    sb.AppendLine($"    角色ID={kv.Key}  技能点={kv.Value?.SkillTotalPoint}");
                    if (kv.Value?.SkillLearnedDataDict?.Dictionary != null)
                        foreach (var sk in kv.Value.SkillLearnedDataDict.Dictionary)
                            sb.AppendLine($"      技能Index={sk.Key}  Lv={sk.Value?.lv}");
                }
            }

            // --- 快捷栏 ---
            sb.AppendLine("  [快捷栏数据]");
            if (gd.CharacterShortcutSkillsDict?.Dictionary != null)
            {
                foreach (var kv in gd.CharacterShortcutSkillsDict.Dictionary)
                {
                    string slots = kv.Value?.skillIds != null
                        ? string.Join(", ", kv.Value.skillIds)
                        : "null";
                    sb.AppendLine($"    角色ID={kv.Key}  Slots=[{slots}]");
                }
            }

            // --- 角色成长 ---
            sb.AppendLine("  [角色成长]");
            if (gd.CharacterProgressDict?.Dictionary != null)
            {
                foreach (var kv in gd.CharacterProgressDict.Dictionary)
                {
                    var p = kv.Value;
                    sb.AppendLine($"    角色ID={kv.Key}  Lv={p?.Level}  Exp={p?.Experience}  HP={p?.CurrentHp}  MP={p?.CurrentMp}");
                }
            }

            // --- 背包 ---
            sb.AppendLine("  [背包物品]");
            if (gd.InventoryItems?.Dictionary != null)
                foreach (var kv in gd.InventoryItems.Dictionary)
                    sb.AppendLine($"    ItemId={kv.Key}  数量={kv.Value}");

            // --- 玩家位置存档 ---
            var lastPos = (Vector3)gd.PlayerLastPosition;
            sb.AppendLine("  [玩家位置存档]");
            sb.AppendLine($"    LastScene={gd.PlayerLastSceneName ?? "null"}");
            sb.AppendLine($"    LastPosition={lastPos}");

            sb.AppendLine("==================================");
            RayDebug.Info(sb.ToString());
        }

        public List<SkillConfig> GetAllSkillConfig()
        {
            return CharacterConfig.SkillConfigList;
        }

        public void AddSkill(int skillIndex, SkillLearnedData skillLearnedData)
        {
            player.SkillBrain.AddSkill(player, GetAllSkillConfig(), skillIndex, skillLearnedData);

            // 新技能自动填入快捷栏（若有空位且是主动技能）
            bool added = DataManager.TryAddSkillToShortcut(skillIndex, CharacterConfig);
            if (added)
            {
                DataManager.SaveGameData();
                // 立即刷新主战斗界面快捷栏 UI
                var mainWindow = UISystem.GetWindow<UI_GameSceneMainWindow>();
                if (mainWindow != null && mainWindow.UIEnable)
                {
                    var shortcutData = DataManager.GetCurrentCharacterShortcutSkills();
                    if (shortcutData != null)
                        mainWindow.ShowShortcutSkillSlots(shortcutData);
                }
            }
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
            CacheLatestPlayerWorldPosition();
        }

        private bool TryGetCurrentPlayerWorldPosition(out Vector3 worldPosition)
        {
            if (player != null)
            {
                worldPosition = player.transform.position;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        private void CacheLatestPlayerWorldPosition()
        {
            if (TryGetCurrentPlayerWorldPosition(out var worldPos))
            {
                _lastKnownPlayerWorldPosition = worldPos;
                _hasLastKnownPlayerWorldPosition = true;
            }
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
            {
                // Inspector 可能绑定到子节点(cm)，这里统一提升到相机根节点，避免漏掉输入组件
                var currentRoot = cineMachine.GetComponentInParent<CameraController>();
                if (currentRoot != null)
                {
                    if (cineMachine != currentRoot.gameObject)
                    {
                        cineMachine = currentRoot.gameObject;
                        _cameraInputBehaviours = null;
                    }
                    return;
                }
            }

            var cameraController = FindAnyObjectByType<CameraController>(FindObjectsInactive.Include);
            if (cameraController != null)
            {
                if (cineMachine != cameraController.gameObject)
                {
                    cineMachine = cameraController.gameObject;
                    _cameraInputBehaviours = null;
                }
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
