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
using RayAnimation;

namespace UI
{
    /// <summary>
    /// 角色选择UI窗口
    /// 负责展示可选角色列表、预览3D模型、显示角色属性
    /// </summary>
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
        [SerializeField] private Transform modelSpawnPoint; // 3D模型生成点
        [SerializeField] private Camera previewCamera;      // 专用预览相机
        [SerializeField] private UnityEngine.UI.RawImage characterDisplayRawImage; // 显示RenderTexture的UI
        [SerializeField] private float modelRotationSpeed = 30f; // 模型旋转速度
        [SerializeField] private float modelScale = 1.5f; // 模型缩放倍数（调整显示大小）
        [SerializeField] private Vector3 modelPositionOffset = Vector3.zero; // 模型位置偏移（如果需要上下左右移动）
        [SerializeField] private string previewLayerName = "CharacterPreview"; // 预览Layer名称
        [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(1024, 1024); // RenderTexture分辨率
        #endregion

        #region 私有字段
        private CharacterTable _characterTable;
        private List<CharacterEntry> _selectableCharacters;
        private GameObject _currentPreviewModel;
        private int _selectedCharacterId;
        private bool _isDragging;
        private Vector2 _lastMousePosition;
        private RenderTexture _renderTexture;
        #endregion

        #region 生命周期
        public override void Init()
        {
            base.Init();
            
            // 创建RenderTexture
            CreateRenderTexture();

            // 加载角色配置表
            _characterTable = ResSystem.LoadAsset<CharacterTable>("CharacterTable");
            if (_characterTable == null)
            {
                RayDebug.Error($"无法加载CharacterTable！");
                return;
            }

            // 筛选可选角色
            _selectableCharacters = _characterTable.Characters.FindAll(c => c.IsPlayable);
            if (_selectableCharacters.Count == 0)
            {
                RayDebug.Error($"没有可选角色！");
                return;
            }

            // 初始化UI
            InitializeCharacterSelector();
            RegisterButtons();
        }

        public override void OnShow()
        {
            base.OnShow();

            // 启用预览相机
            if (previewCamera != null)
            {
                previewCamera.enabled = true;
            }
        }

        public override void OnClose()
        {
            base.OnClose();

            // 清理3D模型
            if (_currentPreviewModel != null)
            {
                Destroy(_currentPreviewModel);
                _currentPreviewModel = null;
            }

            // 关闭预览相机
            if (previewCamera != null)
            {
                previewCamera.enabled = false;
            }

            // 释放RenderTexture
            DestroyRenderTexture();

            // 注销按钮事件
            UnregisterButtonEvents();

            // 释放窗口资源
            ResSystem.UnloadInstance(gameObject);
        }
        #endregion

        #region UI初始化
        /// <summary>
        /// 初始化角色选择器
        /// </summary>
        private void InitializeCharacterSelector()
        {
            if (characterSelector == null)
            {
                return;
            }

            // 清空现有项
            characterSelector.items.Clear();

            // 添加可选角色到选择器
            foreach (var character in _selectableCharacters)
            {
                var item = new HorizontalSelector.Item
                {
                    itemTitle = character.CharacterName,
                    itemIcon = null // 先设为空，稍后异步加载
                };
                
                characterSelector.items.Add(item);
                
                // 异步加载 Sprite
                LoadAndSetIconAsync(character, item).Forget();
            }

            // 重新初始化选择器
            characterSelector.SetupSelector();

            // 监听选择变化
            characterSelector.onValueChanged.AddListener(OnCharacterSelectionChanged);

            // 手动加载第一个角色（因为设置index=0不会触发onValueChanged）
            if (_selectableCharacters.Count > 0)
            {
                LoadCharacterPreview(_selectableCharacters[0].CharacterId).Forget();
            }
        }

        private async UniTaskVoid LoadAndSetIconAsync(CharacterEntry character, HorizontalSelector.Item item)
        {
            if (character.CharacterIcon != null && character.CharacterIcon.RuntimeKeyIsValid())
            {
                var sprite = await character.CharacterIcon.LoadAssetAsync<Sprite>().ToUniTask();
                item.itemIcon = sprite;
                // 注意：由于 HorizontalSelector 在刷新图片时有额外逻辑，所以可能需要在此处调用 UpdateUI();
                characterSelector.UpdateUI();
            }
        }

        private void RegisterButtons()
        {
            confirmButton.Interactable(true);
            confirmButton.useRipple = true;
            confirmButton.enableButtonSounds = false;
            confirmButton.useClickSound = false;
            confirmButton.useHoverSound = false;
            confirmButton.useCustomContent = false;
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            
       
            backButton.Interactable(true);
            backButton.useRipple = true;
            backButton.enableButtonSounds = false;
            backButton.useClickSound = false;
            backButton.useHoverSound = false;
            backButton.useCustomContent = false;
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        /// <summary>
        /// 注销按钮事件
        /// </summary>
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

        #region 角色预览逻辑
        /// <summary>
        /// 加载并显示角色3D预览
        /// </summary>
        private async UniTaskVoid LoadCharacterPreview(int characterId)
        {
            RayDebug.Log($"{nameof(LoadCharacterPreview)} 被调用，CharacterID={characterId}");

            _selectedCharacterId = characterId;

            // 销毁旧模型
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

            // 实例化模型到预览舞台
            _currentPreviewModel = Instantiate(modelPrefab, modelSpawnPoint);
            _currentPreviewModel.transform.localPosition = modelPositionOffset; // 应用位置偏移
            _currentPreviewModel.transform.localRotation = Quaternion.identity;
            _currentPreviewModel.transform.localScale = Vector3.one * modelScale; // 应用缩放

            // 递归设置模型及所有子对象的Layer为CharacterPreview
            int previewLayer = LayerMask.NameToLayer(previewLayerName);
            SetLayerRecursively(_currentPreviewModel, previewLayer);
            
            // 加载角色配置并播放Idle动画
            var config = await CharacterModelManager.Instance.LoadCharacterConfigAsync(characterId);
            if (config != null)
            {
                var animancer = _currentPreviewModel.GetComponent<AnimancerComponent>();
                if (animancer == null)
                    animancer = _currentPreviewModel.AddComponent<AnimancerComponent>();
                if (animancer.Animator == null)
                    animancer.Animator = _currentPreviewModel.GetComponent<Animator>();
                if (config.PlayerSO != null)
                {
                    await animancer.Play(config.PlayerSO.playerMovementData.PlayerIdleData.idle);
                }
                // 更新属性显示
                UpdateAttributeDisplay(config);
            }
        }

        /// <summary>
        /// 递归设置GameObject及其所有子对象的Layer
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 更新属性显示
        /// </summary>
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
                characterDescriptionText.text = $"类型: {entry.CharacterType}\n内存占用: {entry.MemoryCost}MB";
            }
        }
        #endregion

        #region 事件回调
        /// <summary>
        /// 当角色选择发生变化
        /// </summary>
        private void OnCharacterSelectionChanged(int index)
        {
            if (index < 0 || index >= _selectableCharacters.Count)
                return;

            var selectedCharacter = _selectableCharacters[index];
            LoadCharacterPreview(selectedCharacter.CharacterId).Forget();
        }

        /// <summary>
        /// 确认按钮点击
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            EnterGame().Forget();
        }

        /// <summary>
        /// 延迟关闭窗口并进入游戏（等待当前帧结束）
        /// </summary>
        private async UniTaskVoid EnterGame()
        {
            // 立即禁用按钮，防止重复点击
            if (confirmButton != null) confirmButton.Interactable(false);
            if (backButton != null) backButton.Interactable(false);

            // 等待一帧，让ButtonManager.OnPointerClick完全执行完毕
            await UniTask.Yield();

            // 创建新存档并初始化选中的角色
            DataManager.CreateArchive(_selectedCharacterId);

            UISystem.Close<UI_CharacterSelectionWindow>();
            SceneSystem.LoadScene("Game");
        }

        /// <summary>
        /// 返回按钮点击
        /// </summary>
        private void OnBackButtonClicked()
        {
            BackToMenu().Forget();
        }

        /// <summary>
        /// 延迟关闭窗口并返回主菜单
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
                if (Input.GetMouseButtonDown(0))
                {
                    // 检查鼠标是否在RawImage区域内
                    if (RectTransformUtility.RectangleContainsScreenPoint(
                            characterDisplayRawImage.rectTransform,
                            Input.mousePosition,
                            null))
                    {
                        _isDragging = true;
                        _lastMousePosition = Input.mousePosition;
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _isDragging = false;
                }

                if (_isDragging)
                {
                    Vector2 currentMousePosition = Input.mousePosition;
                    Vector2 delta = currentMousePosition - _lastMousePosition;
                    // 水平拖拽旋转模型（左右拖拽）
                    float rotationAmount = delta.x * modelRotationSpeed * Time.deltaTime;
                    _currentPreviewModel.transform.Rotate(Vector3.up, -rotationAmount, Space.World);

                    _lastMousePosition = currentMousePosition;
                }
            }
        }
        #endregion

        #region RenderTexture管理
        /// <summary>
        /// 创建并配置RenderTexture
        /// </summary>
        private void CreateRenderTexture()
        {
            if (_renderTexture != null)
                return;

            _renderTexture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 24)
            {
                antiAliasing = 4, // 4x MSAA抗锯齿
                filterMode = FilterMode.Bilinear
            };
            _renderTexture.Create();

            // 绑定到PreviewCamera和RawImage
            previewCamera.targetTexture = _renderTexture;
            characterDisplayRawImage.texture = _renderTexture;
        }

        /// <summary>
        /// 销毁RenderTexture
        /// </summary>
        private void DestroyRenderTexture()
        {
            if (_renderTexture != null)
            {
                // 解绑
                previewCamera.targetTexture = null; 
                characterDisplayRawImage.texture = null;
                // 释放资源
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
        #endregion
    }
}
