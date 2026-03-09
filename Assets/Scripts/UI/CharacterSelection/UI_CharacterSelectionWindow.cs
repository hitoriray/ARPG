using System.Collections.Generic;
using Animancer;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using JKFrame;
using Config;
using Manager;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace UI
{
    [UIWindowData(typeof(UI_CharacterSelectionWindow), false, nameof(UI_CharacterSelectionWindow), 1)]
    public class UI_CharacterSelectionWindow : UI_WindowBase
    {
        #region UI组件引用
        [Header("角色选择器")]
        [SerializeField] private HorizontalSelector characterSelector;

        [Header("属性显示")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI mpText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterDescriptionText;

        [Header("按钮")]
        [SerializeField] private ButtonManager confirmButton;
        [SerializeField] private ButtonManager backButton;

        [Header("3D预览")]
        [SerializeField] private Transform modelSpawnPoint;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private UnityEngine.UI.RawImage characterDisplayRawImage;
        [SerializeField] private float modelRotationSpeed = 30f;
        [SerializeField] private float modelScale = 1.5f;
        [SerializeField] private Vector3 modelPositionOffset = Vector3.zero;
        [SerializeField] private string previewLayerName = "CharacterPreview";
        [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(1024, 1024);
        #endregion

        #region 私有字段
        private CharacterTable _characterTable;
        private List<CharacterEntry> _selectableCharacters;
        private GameObject _currentPreviewModel;
        private int _selectedCharacterId;
        private bool _isDragging;
        private RenderTexture _renderTexture;
        private int _previewLayer = -1;
        private int _currentPreviewIdleIndex;
        private Animancer.ManualMixerState _previewStandMixer;
        // 预览模型上的布料组件，拖拽时暂停模拟以避免掉帧
        private Behaviour[] _previewClothComponents;
        #endregion

        #region 生命周期
        public override void Init()
        {
            base.Init();

            CreateRenderTexture();
            ConfigurePreviewStage();

            _characterTable = ResSystem.LoadAsset<CharacterTable>("CharacterTable");
            if (_characterTable == null)
            {
                RayDebug.Error("无法加载 CharacterTable。");
                return;
            }

            _selectableCharacters = _characterTable.Characters.FindAll(c => c.IsPlayable);
            if (_selectableCharacters.Count == 0)
            {
                RayDebug.Error("没有可选角色。");
                return;
            }

            InitializeCharacterSelector();
            RegisterButtons();
        }

        public override void OnShow()
        {
            base.OnShow();
            if (previewCamera != null)
            {
                previewCamera.enabled = true;
            }
        }

        public override void OnClose()
        {
            base.OnClose();

            if (_currentPreviewModel != null)
            {
                Destroy(_currentPreviewModel);
                _currentPreviewModel = null;
            }

            if (previewCamera != null)
            {
                previewCamera.enabled = false;
            }

            DestroyRenderTexture();
            UnregisterButtonEvents();
        }
        #endregion

        #region UI初始化
        private void InitializeCharacterSelector()
        {
            if (characterSelector == null)
            {
                return;
            }

            characterSelector.items.Clear();

            foreach (var character in _selectableCharacters)
            {
                var item = new HorizontalSelector.Item
                {
                    itemTitle = character.CharacterName,
                    itemIcon = null
                };
                characterSelector.items.Add(item);
                LoadAndSetIconAsync(character, item).Forget();
            }

            characterSelector.SetupSelector();
            characterSelector.onValueChanged.AddListener(OnCharacterSelectionChanged);

            if (_selectableCharacters.Count > 0)
            {
                LoadCharacterPreview(_selectableCharacters[0].CharacterId).Forget();
            }
        }

        private async UniTaskVoid LoadAndSetIconAsync(CharacterEntry character, HorizontalSelector.Item item)
        {
            if (character == null || item == null)
                return;

            if (character.CharacterIcon == null || !character.CharacterIcon.RuntimeKeyIsValid())
                return;

            try
            {
                Sprite sprite;
                if (character.CharacterIcon.IsValid())
                {
                    // handle 已存在（第二次打开窗口），直接复用结果，避免重复加载警告
                    sprite = character.CharacterIcon.OperationHandle.Result as Sprite;
                }
                else
                {
                    sprite = await character.CharacterIcon.LoadAssetAsync<Sprite>().ToUniTask();
                }

                if (sprite == null)
                    return;

                item.itemIcon = sprite;
                if (characterSelector != null)
                    characterSelector.UpdateUI();
            }
            catch (System.Exception e)
            {
                JKLog.Warning($"[UI_CharacterSelectionWindow] Load icon failed: {character.CharacterName}, {e.Message}");
            }
        }

        private void RegisterButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.Interactable(true);
                confirmButton.useRipple = true;
                confirmButton.enableButtonSounds = false;
                confirmButton.useClickSound = false;
                confirmButton.useHoverSound = false;
                confirmButton.useCustomContent = false;
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            if (backButton != null)
            {
                backButton.Interactable(true);
                backButton.useRipple = true;
                backButton.enableButtonSounds = false;
                backButton.useClickSound = false;
                backButton.useHoverSound = false;
                backButton.useCustomContent = false;
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void UnregisterButtonEvents()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);

            if (characterSelector != null)
                characterSelector.onValueChanged.RemoveListener(OnCharacterSelectionChanged);
        }
        #endregion

        #region 加载角色模型
        /// <summary>
        /// 加载角色模型
        /// </summary>
        private async UniTaskVoid LoadCharacterPreview(int characterId)
        {
            RayDebug.Log($"{nameof(LoadCharacterPreview)} 加载角色模型: CharacterID={characterId}");

            _selectedCharacterId = characterId;

            // 销毁旧模型，重置预览状态
            if (_currentPreviewModel != null)
            {
                Destroy(_currentPreviewModel);
                _currentPreviewModel = null;
            }
            _previewStandMixer = null;
            _currentPreviewIdleIndex = -1;
            _previewClothComponents = null;
            
            // 异步加载新模型
            var modelPrefab = await CharacterModelManager.Instance.LoadCharacterModelPrefabAsync(characterId);
            if (modelPrefab == null)
            {
                return;
            }

            if (modelSpawnPoint == null)
            {
                RayDebug.Error("[UI_CharacterSelectionWindow] modelSpawnPoint is null, cannot spawn preview model.");
                return;
            }

            _currentPreviewModel = Instantiate(modelPrefab, modelSpawnPoint);
            _currentPreviewModel.transform.localPosition = modelPositionOffset;
            _currentPreviewModel.transform.localRotation = Quaternion.identity;
            _currentPreviewModel.transform.localScale = Vector3.one * modelScale;

            int previewLayer = _previewLayer >= 0 ? _previewLayer : LayerMask.NameToLayer(previewLayerName);
            SetLayerRecursively(_currentPreviewModel, previewLayer);

            // 预览模型不需要物理和阴影，禁用后可大幅降低旋转时的 PhysX 开销和 ShadowMap 失效
            DisablePreviewModelRuntimeFeatures(_currentPreviewModel);

            // 缓存布料组件（通过命名空间匹配 MagicaCloth，兼容 v1/v2）
            // 拖拽旋转时暂停模拟，松手后恢复，避免大量粒子重算导致掉帧
            var allBehaviours = _currentPreviewModel.GetComponentsInChildren<Behaviour>(true);
            _previewClothComponents = System.Array.FindAll(
                allBehaviours,
                b => b != null && (b.GetType().FullName?.Contains("MagicaCloth") == true));
            
            var config = await CharacterModelManager.Instance.LoadCharacterConfigAsync(characterId);
            if (config != null)
            {
                bool previewPlayed = false;
                var animator = _currentPreviewModel.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    animator = _currentPreviewModel.AddComponent<Animator>();
                }

                if (animator != null)
                {
                    // 无条件覆盖 Avatar（不要只在 null 时才赋值，避免 prefab 内旧 Avatar 干扰）
                    if (config.Avatar != null)
                        animator.avatar = config.Avatar;
                    else if (config.GenericLocomotionConfig?.avatar != null)
                        animator.avatar = config.GenericLocomotionConfig.avatar;

                    animator.applyRootMotion = false;
                    // 注意：不能在 AnimancerComponent 存在时调用 animator.Rebind()
                    // Rebind() 会切断 Animancer 的 AnimationPlayableOutput 与 Animator 的连接
                }

                // 优先在整个预制体层级内查找已有的 AnimancerComponent，避免重复添加
                var animancer = _currentPreviewModel.GetComponentInChildren<AnimancerComponent>(true);

                if (config.PlayerSO != null && animator != null)
                {
                    if (animancer == null)
                        animancer = animator.gameObject.AddComponent<AnimancerComponent>();

                    if (animancer.Animator == null)
                        animancer.Animator = animator;

                    var idleData = config.PlayerSO.playerMovementData?.PlayerIdleData;
                    if (idleData?.idle != null)
                    {
                        try
                        {
                            var state = animancer.Play(idleData.idle);

                            // ── 第一层：根混合器 ──────────────────────────────────────
                            // 对应游戏内 lockValueParameter = 0（非锁敌）
                            // 若根节点是 MixerState<float>（LinearMixerState），将参数拨到 0
                            if (state is Animancer.MixerState<float> rootMixer)
                                rootMixer.Parameter = 0f;

                            // 强制熄灭锁敌分支（Child 1），只保留非锁敌分支（Child 0）
                            var lockBranch = state.GetChild(1);
                            if (lockBranch != null) { lockBranch.SetWeight(0f); lockBranch.Stop(); }

                            var nonLockBranch = state.GetChild(0);
                            if (nonLockBranch != null)
                            {
                                nonLockBranch.SetWeight(1f);

                                // ── 第二层：站立/蹲伏混合器 ───────────────────────────
                                // 对应游戏内 standValueParameter = 1（站立）
                                if (nonLockBranch is Animancer.MixerState<float> standMixer)
                                    standMixer.Parameter = 1f;

                                // 强制熄灭蹲伏分支（Child 0），只保留站立分支（Child 1）
                                var crouchBranch = nonLockBranch.GetChild(0);
                                if (crouchBranch != null) { crouchBranch.SetWeight(0f); crouchBranch.Stop(); }
                            }

                            // ── 第三层：站立Idle ManualMixerState ────────────────────
                            // 树结构：root.Child(0).Child(1) = standIdleMixerState
                            _previewStandMixer = nonLockBranch?.GetChild(1) as Animancer.ManualMixerState;
                            if (_previewStandMixer != null && _previewStandMixer.ChildCount > 0)
                            {
                                _currentPreviewIdleIndex = -1;
                                PlayNextPreviewIdle();
                                previewPlayed = true;
                            }
                        }
                        catch (System.Exception e)
                        {
                            RayDebug.Warn($"角色预览 Animancer 播放失败: {e.Message}");
                        }
                    }
                }

                // 回退：GenericLocomotion AnimatorController
                if (!previewPlayed && config.GenericLocomotionConfig?.animatorController != null && animator != null)
                {
                    if (config.GenericLocomotionConfig.avatar != null)
                        animator.avatar = config.GenericLocomotionConfig.avatar;
                    animator.runtimeAnimatorController = config.GenericLocomotionConfig.animatorController;
                    animator.applyRootMotion = false;
                    animator.Play(0, 0, 0f);
                    previewPlayed = true;
                }

                // 最后兜底：只要有 Animator 就尝试播放默认状态
                if (!previewPlayed && animator != null)
                {
                    animator.applyRootMotion = false;
                    animator.Play(0, 0, 0f);
                    previewPlayed = true;
                }

                if (!previewPlayed)
                {
                    RayDebug.Warn($"当前无法播放预览动画: CharacterID={characterId}");
                }

                // 更新属性显示
                UpdateAttributeDisplay(config);
            }
            
            // 加入少量延迟，确保Animancer绑定并计算好Idle姿态，防止初次打开看到T-pose
            await UniTask.Delay(200);
            EventSystem.EventTrigger(GameManager.GameSceneReadyEvent);
        }

        /// <summary>
        /// 循环播放预览 Stand Idle 动画（首次调用播放 index 0，结束后自动切下一个）
        /// 与游戏内 PlayerReusableLogic.PlayNextState 逻辑完全对应
        /// </summary>
        private void PlayNextPreviewIdle()
        {
            if (_previewStandMixer == null || _previewStandMixer.ChildCount == 0) return;

            _currentPreviewIdleIndex = (_currentPreviewIdleIndex + 1) % _previewStandMixer.ChildCount;

            for (int i = 0; i < _previewStandMixer.ChildCount; i++)
            {
                var child = _previewStandMixer.GetChild(i);
                if (i == _currentPreviewIdleIndex)
                {
                    child.SetWeight(1f);
                    child.Play();
                    child.Events(this).OnEnd = PlayNextPreviewIdle;
                }
                else
                {
                    child.SetWeight(0f);
                    child.Stop();
                }
            }
        }

        private void ConfigurePreviewStage()
        {
            _previewLayer = LayerMask.NameToLayer(previewLayerName);
            if (_previewLayer < 0)
            {
                RayDebug.Warn($"[UI_CharacterSelectionWindow] Preview layer not found: {previewLayerName}");
                return;
            }

            int previewMask = 1 << _previewLayer;
            if (previewCamera != null)
            {
                previewCamera.cullingMask = previewMask;
            }
            else
            {
                RayDebug.Warn("[UI_CharacterSelectionWindow] previewCamera is null.");
            }

            if (modelSpawnPoint != null)
            {
                var lights = modelSpawnPoint.root.GetComponentsInChildren<Light>(true);
                foreach (var light in lights)
                {
                    light.cullingMask |= previewMask;
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null || layer < 0)
                return;

            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void UpdateAttributeDisplay(CharacterConfig config)
        {
            hpText.text = $"{config.hpBaseValue:F0}";
            mpText.text = $"{config.mpBaseValue:F0}";
            attackText.text = $"{config.attackBaseValue:F0}";

            // 更新角色名称和描述
            var entry = _characterTable.GetCharacterById(_selectedCharacterId);
            if (entry != null)
            {
                characterNameText.text = entry.CharacterName;
                characterDescriptionText.text = entry.CharacterName;
            }
        }
        #endregion

        #region 切换模型
        private void OnCharacterSelectionChanged(int index)
        {
            if (index < 0 || index >= _selectableCharacters.Count)
                return;

            var selectedCharacter = _selectableCharacters[index];
            LoadCharacterPreview(selectedCharacter.CharacterId).Forget();
        }

        private void OnConfirmButtonClicked()
        {
            EnterGame().Forget();
        }

        /// <summary>
        /// 进入游戏
        /// </summary>
        private async UniTaskVoid EnterGame()
        {
            // 立即禁用按钮，防止重复点击
            if (confirmButton != null) confirmButton.Interactable(false);
            if (backButton != null) backButton.Interactable(false);

            await UniTask.Yield();

            // 创建新存档并初始化选中的角色
            DataManager.CreateArchive(_selectedCharacterId);

            UISystem.Close<UI_CharacterSelectionWindow>();
            GameManager.Instance.EnterGameSceneWithLoading();
        }

        private void OnBackButtonClicked()
        {
            BackToMenu().Forget();
        }

        /// <summary>
        /// 返回菜单界面
        /// </summary>
        private async UniTaskVoid BackToMenu()
        {
            // 立即禁用按钮，防止重复点击
            if (confirmButton != null) confirmButton.Interactable(false);
            if (backButton != null) backButton.Interactable(false);

            // 等待一帧
            await UniTask.Yield();

            UISystem.Close<UI_CharacterSelectionWindow>();
            JKFrame.SceneSystem.LoadSceneAsync("Menu");
        }
        #endregion
        #region Update
        private void Update()
        {
            if (_currentPreviewModel != null && characterDisplayRawImage != null)
            {
                var mouse = Mouse.current;
                if (mouse == null)
                    return;

                Vector2 mousePosition = mouse.position.ReadValue();

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    // 检查鼠标是否在 RawImage 区域内
                    if (RectTransformUtility.RectangleContainsScreenPoint(
                            characterDisplayRawImage.rectTransform,
                            mousePosition,
                            null))
                    {
                        _isDragging = true;
                        // SetPreviewClothEnabled(false); // 拖拽时暂停布料模拟
                    }
                }
                else if (mouse.leftButton.wasReleasedThisFrame)
                {
                    _isDragging = false;
                    // SetPreviewClothEnabled(true); // 松手后恢复
                }

                if (_isDragging)
                {
                    // 直接读取 Input System 的帧内聚合 delta，单位：像素/帧
                    // 鼠标位移本身已是 per-frame 值，不乘 Time.deltaTime
                    // modelRotationSpeed 语义变为：度/像素（Inspector 建议设 0.2~0.5）
                    float deltaX = mouse.delta.ReadValue().x;
                    _currentPreviewModel.transform.Rotate(Vector3.up, -deltaX * modelRotationSpeed, Space.World);
                }
            }
        }
        #endregion

        #region 布料模拟控制
        private void SetPreviewClothEnabled(bool enabled)
        {
            if (_previewClothComponents == null) return;
            foreach (var cloth in _previewClothComponents)
            {
                if (cloth != null) cloth.enabled = enabled;
            }
        }

        /// <summary>
        /// 禁用预览模型上与游戏运行相关但对展示无用的组件，消除旋转时的物理/阴影开销
        /// </summary>
        private void DisablePreviewModelRuntimeFeatures(GameObject model)
        {
            // 禁用 CharacterController：旋转时 PhysX CapsuleSweep 是主要 CPU 杀手
            var cc = model.GetComponentInChildren<CharacterController>(true);
            if (cc != null) cc.enabled = false;

            // Rigidbody 设为 Kinematic，避免物理引擎驱动位移
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true))
                rb.isKinematic = true;

            // 关闭所有 Renderer 的阴影投射与接收
            // 旋转时骨骼大幅位移会让 ShadowMap dirty，强制每帧重渲染阴影 Pass
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
        #endregion

        #region RenderTexture预览
        private void CreateRenderTexture()
        {
            if (_renderTexture != null)
                return;

            if (previewCamera == null || characterDisplayRawImage == null)
            {
                RayDebug.Error("[UI_CharacterSelectionWindow] previewCamera or characterDisplayRawImage is not assigned.");
                return;
            }

            _renderTexture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 24)
            {
                antiAliasing = 4
            };
            _renderTexture.filterMode = FilterMode.Bilinear;
            _renderTexture.Create();

            if (previewCamera != null)
                previewCamera.targetTexture = _renderTexture;
            if (characterDisplayRawImage != null)
                characterDisplayRawImage.texture = _renderTexture;
        }

        private void DestroyRenderTexture()
        {
            if (_renderTexture != null)
            {
                if (previewCamera != null)
                    previewCamera.targetTexture = null;
                if (characterDisplayRawImage != null)
                    characterDisplayRawImage.texture = null;

                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
        #endregion
    }
}

