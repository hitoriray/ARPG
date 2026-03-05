using System.Collections.Generic;
using Animancer;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using JKFrame;
using Config;
using Data;
using Manager;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace UI
{
    /// <summary>
    /// 瑙掕壊閫夋嫨UI绐楀彛
    /// 璐熻矗灞曠ず鍙€夎鑹插垪琛ㄣ€侀瑙?D妯″瀷銆佹樉绀鸿鑹插睘鎬?    /// </summary>
    [UIWindowData(typeof(UI_CharacterSelectionWindow), false, "UI_CharacterSelectionWindow", 2)]
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
        private Vector2 _lastMousePosition;
        private RenderTexture _renderTexture;
        private int _previewLayer = -1;
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
            ResSystem.UnloadInstance(gameObject);
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
                var sprite = await character.CharacterIcon.LoadAssetAsync<Sprite>().ToUniTask();
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

        #region 瑙掕壊棰勮閫昏緫
        /// <summary>
        /// 鍔犺浇骞舵樉绀鸿鑹?D棰勮
        /// </summary>
        private async UniTaskVoid LoadCharacterPreview(int characterId)
        {
            RayDebug.Log($"{nameof(LoadCharacterPreview)} 琚皟鐢紝CharacterID={characterId}");

            _selectedCharacterId = characterId;

            // 閿€姣佹棫妯″瀷
            if (_currentPreviewModel != null)
            {
                Destroy(_currentPreviewModel);
                _currentPreviewModel = null;
            }
            
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

            // 瀹炰緥鍖栨ā鍨嬪埌棰勮鑸炲彴
            _currentPreviewModel = Instantiate(modelPrefab, modelSpawnPoint);
            _currentPreviewModel.transform.localPosition = modelPositionOffset; // 搴旂敤浣嶇疆鍋忕Щ
            _currentPreviewModel.transform.localRotation = Quaternion.identity;
            _currentPreviewModel.transform.localScale = Vector3.one * modelScale; // 搴旂敤缂╂斁

            // 閫掑綊璁剧疆妯″瀷鍙婃墍鏈夊瓙瀵硅薄鐨凩ayer涓篊haracterPreview
            int previewLayer = _previewLayer >= 0 ? _previewLayer : LayerMask.NameToLayer(previewLayerName);
            SetLayerRecursively(_currentPreviewModel, previewLayer);
            
            // 鍔犺浇瑙掕壊閰嶇疆骞舵挱鏀綢dle鍔ㄧ敾
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
                    if (animator.avatar == null && config.Avatar != null)
                    {
                        animator.avatar = config.Avatar;
                    }

                    if (animator.avatar == null && config.GenericLocomotionConfig?.avatar != null)
                    {
                        animator.avatar = config.GenericLocomotionConfig.avatar;
                    }
                }

                var animancer = animator != null ? animator.GetComponent<AnimancerComponent>() : null;

                // 浼樺厛锛氭湁 PlayerSO 涓旀ā鍨嬪甫 Animator 鏃讹紝浣跨敤 Animancer 鎾斁 idle
                if (config.PlayerSO != null && animator != null)
                {
                    if (animancer == null)
                        animancer = animator.gameObject.AddComponent<AnimancerComponent>();

                    if (animancer.Animator == null)
                        animancer.Animator = animator;

                    var idleTransition = config.PlayerSO.playerMovementData?.PlayerIdleData?.idle;
                    if (idleTransition != null && animancer.Animator != null)
                    {
                        try
                        {
                            _ = animancer.Play(idleTransition);
                            previewPlayed = true;
                        }
                        catch (System.Exception e)
                        {
                            RayDebug.Warn($"瑙掕壊棰勮 Animancer 鎾斁澶辫触锛屽洖閫€鍒?Animator: {e.Message}");
                        }
                    }
                }

                // 鍥為€€锛氫娇鐢?GenericLocomotion 鐨?AnimatorController
                if (!previewPlayed && config.GenericLocomotionConfig != null && animator != null)
                {
                    if (config.GenericLocomotionConfig.avatar != null)
                        animator.avatar = config.GenericLocomotionConfig.avatar;
                    if (config.GenericLocomotionConfig.animatorController != null)
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
                    RayDebug.Warn($"瑙掕壊棰勮妯″瀷缂哄皯 Animator锛屾棤娉曟挱鏀惧姩鐢汇€侰haracterID={characterId}");
                }

                // 更新属性显示
                UpdateAttributeDisplay(config);
            }
        }

        /// <summary>
        /// 閫掑綊璁剧疆GameObject鍙婂叾鎵€鏈夊瓙瀵硅薄鐨凩ayer
        /// </summary>
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

        /// <summary>
        /// 鏇存柊灞炴€ф樉绀?        /// </summary>
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
                characterDescriptionText.text = $"绫诲瀷: {entry.CharacterType}\n鍐呭瓨鍗犵敤: {entry.MemoryCost}MB";
            }
        }
        #endregion

        #region 浜嬩欢鍥炶皟
        /// <summary>
        /// 褰撹鑹查€夋嫨鍙戠敓鍙樺寲
        /// </summary>
        private void OnCharacterSelectionChanged(int index)
        {
            if (index < 0 || index >= _selectableCharacters.Count)
                return;

            var selectedCharacter = _selectableCharacters[index];
            LoadCharacterPreview(selectedCharacter.CharacterId).Forget();
        }

        /// <summary>
        /// 纭鎸夐挳鐐瑰嚮
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            EnterGame().Forget();
        }

        /// <summary>
        /// 寤惰繜鍏抽棴绐楀彛骞惰繘鍏ユ父鎴忥紙绛夊緟褰撳墠甯х粨鏉燂級
        /// </summary>
        private async UniTaskVoid EnterGame()
        {
            // 立即禁用按钮，防止重复点击
            if (confirmButton != null) confirmButton.Interactable(false);
            if (backButton != null) backButton.Interactable(false);

            // 绛夊緟涓€甯э紝璁〣uttonManager.OnPointerClick瀹屽叏鎵ц瀹屾瘯
            await UniTask.Yield();

            // 创建新存档并初始化选中的角色
            DataManager.CreateArchive(_selectedCharacterId);

            UISystem.Close<UI_CharacterSelectionWindow>();
            GameManager.Instance.EnterGameSceneWithLoading();
        }

        /// <summary>
        /// 杩斿洖鎸夐挳鐐瑰嚮
        /// </summary>
        private void OnBackButtonClicked()
        {
            BackToMenu().Forget();
        }

        /// <summary>
        /// 寤惰繜鍏抽棴绐楀彛骞惰繑鍥炰富鑿滃崟
        /// </summary>
        private async UniTaskVoid BackToMenu()
        {
            // 立即禁用按钮，防止重复点击
            if (confirmButton != null) confirmButton.Interactable(false);
            if (backButton != null) backButton.Interactable(false);

            // 等待一帧
            await UniTask.Yield();

            UISystem.Close<UI_CharacterSelectionWindow>();
            UISystem.Show<UI_MenuSceneMenuWindow>();
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
                        _lastMousePosition = mousePosition;
                    }
                }
                else if (mouse.leftButton.wasReleasedThisFrame)
                {
                    _isDragging = false;
                }

                if (_isDragging)
                {
                    Vector2 currentMousePosition = mousePosition;
                    Vector2 delta = currentMousePosition - _lastMousePosition;
                    // 姘村钩鎷栨嫿鏃嬭浆妯″瀷锛堝乏鍙虫嫋鎷斤級
                    float rotationAmount = delta.x * modelRotationSpeed * Time.deltaTime;
                    _currentPreviewModel.transform.Rotate(Vector3.up, -rotationAmount, Space.World);

                    _lastMousePosition = currentMousePosition;
                }
            }
        }
        #endregion

        #region RenderTexture绠＄悊
        /// <summary>
        /// 鍒涘缓骞堕厤缃甊enderTexture
        /// </summary>
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

            // 缁戝畾鍒癙reviewCamera鍜孯awImage
            if (previewCamera != null)
                previewCamera.targetTexture = _renderTexture;
            if (characterDisplayRawImage != null)
                characterDisplayRawImage.texture = _renderTexture;
        }

        /// <summary>
        /// 閿€姣丷enderTexture
        /// </summary>
        private void DestroyRenderTexture()
        {
            if (_renderTexture != null)
            {
                // 瑙ｇ粦
                if (previewCamera != null)
                    previewCamera.targetTexture = null;
                if (characterDisplayRawImage != null)
                    characterDisplayRawImage.texture = null;
                // 閲婃斁璧勬簮
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
        #endregion
    }
}

