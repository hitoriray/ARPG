using System.Collections.Generic;
using Attribute;
using Config;
using Data;
using JKFrame;
using Manager;
using Michsky.MUIP;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_GameSceneMainWindow), true, nameof(UI_GameSceneMainWindow), 1)]
    public class UI_GameSceneMainWindow : UI_WindowBase
    {
        public override void Init()
        {
            base.Init();
            SetupScrollContent();
        }

        public override void OnShow()
        {
            base.OnShow();
            RefreshList(null, 0);
            EventSystem.EventTrigger("RequestInteractListUpdate");
            _hasTargetScroll = false;
            
            // 每次显示时重新初始化颜色和禁用进度条自走模式
            InitBarColors();
            StopBarsAutoPlay();

            // 立即刷新血量和魔量条
            var attr = PlayerService.Instance?.GetCharacterController()?.CharacterAttribute;
            if (attr != null)
            {
                OnHpChanged(attr.currentHp, attr.maxHp.Total);
                OnMpChanged(attr.currentMp, attr.maxMp.Total);
            }

            // 从存档读取当前角色等级和经验，初始化经验条
            RefreshExpBarFromSave();
        }
        
        protected override void RegisterEventListener()
        {
            base.RegisterEventListener();
            EventSystem.AddEventListener<List<string>, int>("UpdateInteractList", RefreshList);

            // 订阅 HP/MP 事件
            var attr = PlayerService.Instance?.GetCharacterController()?.CharacterAttribute;
            if (attr != null)
            {
                attr.OnHpChanged += OnHpChanged;
                attr.OnMpChanged += OnMpChanged;
            }

            // 订阅 EXP 事件（获得经验时即时驱动，升级时也驱动）
            DataManager.OnExpGained += OnExpGained;
            DataManager.OnLevelUp   += OnExpLevelChanged;
        }
        
        protected override void UnRegisterEventListener()
        {
            base.UnRegisterEventListener();
            EventSystem.RemoveEventListener<List<string>, int>("UpdateInteractList", RefreshList);

            var attr = PlayerService.Instance?.GetCharacterController()?.CharacterAttribute;
            if (attr != null)
            {
                attr.OnHpChanged -= OnHpChanged;
                attr.OnMpChanged -= OnMpChanged;
            }

            DataManager.OnExpGained -= OnExpGained;
            DataManager.OnLevelUp   -= OnExpLevelChanged;
        }

        private void Start()
        {
            InitBarColors();
        }

        private void Update()
        {
            if (!_hasTargetScroll || interactScrollRect == null || interactListView == null || interactListView.itemParent == null)
                return;

            var content = interactListView.itemParent as RectTransform;
            if (content == null) return;

            float currentY = content.anchoredPosition.y;
            float nextY = Mathf.SmoothDamp(currentY, _targetScrollY, ref _scrollVelocity, scrollSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, nextY);

            if (Mathf.Abs(nextY - _targetScrollY) < 0.1f)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, _targetScrollY);
                _hasTargetScroll = false;
            }
        }

        #region 技能快捷栏
        [SerializeField] private UI_ShortcutSkill_Slot[] shortcutSkillSlots;

        public void Show(ShortcutSkillSlotData shortcutSkillSlotData)
        {
            ShowShortcutSkillSlots(shortcutSkillSlotData);
        }

        public bool TryGetShortcutSkillSlot(int skillIndex, out UI_ShortcutSkill_Slot slot)
        {
            if (TryGetShortcutSkillSlotIndex(skillIndex, out int slotIndex))
            {
                slot = shortcutSkillSlots[slotIndex];
                return true;
            }
            slot = null;
            return false;
        }

        public bool TryGetShortcutSkillSlotIndex(int skillIndex, out int slotIndex)
        {
            for (int i = 0; i < shortcutSkillSlots.Length; i++)
            {
                if (shortcutSkillSlots[i].skillIndex == skillIndex)
                {
                    slotIndex = i;
                    return true;
                }
            }
            slotIndex = -1;
            return false;
        }

        public void ShowShortcutSkillSlots(ShortcutSkillSlotData shortcutSkillSlotData)
        {
            List<SkillConfig> skillConfigs = PlayerService.Instance.GetAllSkillConfig();
            for (int i = 0; i < shortcutSkillSlotData.skillIds.Length; i++)
            {
                SkillConfig skillConfig = null;
                int skillIndex = shortcutSkillSlotData.skillIds[i];
                if (skillIndex != -1)
                {
                    skillConfig = skillConfigs[skillIndex];
                }
                shortcutSkillSlots[i].Init(i);
                shortcutSkillSlots[i].Show(skillIndex, skillConfig);
            }
        }

        public void ChangeShortcutSkill(int slotIndex, int newSkillIndex)
        {
            SkillConfig skillConfig = null;
            if (newSkillIndex != -1)
            {
                skillConfig = PlayerService.Instance.GetAllSkillConfig()[newSkillIndex];
            }
            shortcutSkillSlots[slotIndex].Show(newSkillIndex, skillConfig);

            // 使用新接口：更新当前角色的快捷栏数据
            var currentShortcutData = DataManager.GetCurrentCharacterShortcutSkills();
            currentShortcutData.skillIds[slotIndex] = newSkillIndex;
            DataManager.SaveGameData();
        }
        #endregion
        
        #region buff栏
        [SerializeField] private GameObject buffSlotPrefab;
        [SerializeField] private Transform buffSlotParent;
        private List<UI_Buff_Slot> buffSlotList = new();

        public UI_Buff_Slot AddBuff(BuffConfig buffConfig)
        {
            var buffSlot = ProjectUtility.GetOrInstantiateGameObject(buffSlotPrefab, buffSlotParent).GetComponent<UI_Buff_Slot>();
            buffSlotList.Add(buffSlot);
            buffSlot.Init(buffConfig);
            return buffSlot;
        }

        public void RemoveBuff(UI_Buff_Slot buffSlot)
        {
            buffSlot.Destroy();
            buffSlotList.Remove(buffSlot);
        }
        
        #endregion
        
        #region 获取物品提示栏

        [SerializeField] private NotificationManager pickupNotifyPrefab;
        [SerializeField] private RectTransform pickupNotifyRoot;
        [SerializeField, Min(0.5f)] private float pickupNotifyDuration = 2.5f;
        [SerializeField, Min(1)] private int pickupNotifyMax = 6;

        private readonly List<NotificationManager> _activePickupNotifies = new();
        private readonly Dictionary<int, Sprite> _itemIconCache = new();

        public void ShowPickupNotification(ItemConfig config, int amount)
        {
            if (config == null || amount <= 0) return;

            EnsurePickupNotifyRoot();
            if (pickupNotifyPrefab == null || pickupNotifyRoot == null) return;

            var go = Instantiate(pickupNotifyPrefab.gameObject, pickupNotifyRoot);
            var notif = go.GetComponent<NotificationManager>();
            if (notif == null) return;

            ForceUseCustomContent(notif, true);
            notif.enableTimer = true;
            notif.timer = pickupNotifyDuration;
            notif.closeOnClick = false;
            notif.useStacking = false;
            notif.startBehaviour = NotificationManager.StartBehaviour.None;
            notif.closeBehaviour = NotificationManager.CloseBehaviour.Destroy;

            string pickUpInfo;
            if (amount > 1) pickUpInfo = $"{config.ItemName} x{amount}";
            else pickUpInfo = $"{config.ItemName}";
            notif.title = pickUpInfo;
            notif.description = string.Empty;
            ApplyNotificationIcon(notif, config);
            notif.UpdateUI();

            // 如果 descriptionObj 与 titleObj 是同一个引用，避免把标题隐藏掉
            if (notif.titleObj != null)
                notif.titleObj.text = pickUpInfo;

            notif.onClose.AddListener(() =>
            {
                _activePickupNotifies.Remove(notif);
            });

            _activePickupNotifies.Add(notif);
            TrimPickupNotifies();
            notif.Open();
        }

        private void TrimPickupNotifies()
        {
            if (pickupNotifyMax <= 0) return;
            while (_activePickupNotifies.Count > pickupNotifyMax)
            {
                var oldest = _activePickupNotifies[0];
                _activePickupNotifies.RemoveAt(0);
                if (oldest != null)
                    Destroy(oldest.gameObject);
            }
        }

        private void EnsurePickupNotifyRoot()
        {
            if (pickupNotifyRoot != null) return;

            var root = new GameObject("PickupNotifyRoot", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            pickupNotifyRoot = root.GetComponent<RectTransform>();
            pickupNotifyRoot.anchorMin = new Vector2(0f, 0.5f);
            pickupNotifyRoot.anchorMax = new Vector2(0f, 0.5f);
            pickupNotifyRoot.pivot = new Vector2(0f, 0.5f);
            pickupNotifyRoot.anchoredPosition = new Vector2(40f, 0f);
            pickupNotifyRoot.sizeDelta = new Vector2(420f, 300f);

            var layout = root.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void ApplyNotificationIcon(NotificationManager notif, ItemConfig config)
        {
            if (notif == null || config == null) return;

            if (_itemIconCache.TryGetValue(config.ItemId, out var cached) && cached != null)
            {
                notif.icon = cached;
                if (notif.iconObj != null)
                    notif.iconObj.sprite = cached;
                return;
            }

            if (config.Icon == null || !config.Icon.RuntimeKeyIsValid()) return;

            var handle = config.Icon.LoadAssetAsync<Sprite>();
            handle.Completed += op =>
            {
                if (op.Status != AsyncOperationStatus.Succeeded) return;
                var sprite = op.Result;
                if (sprite == null) return;
                _itemIconCache[config.ItemId] = sprite;
                if (notif != null)
                {
                    notif.icon = sprite;
                    if (notif.iconObj != null)
                    {
                        notif.iconObj.sprite = sprite;
                    }
                    else if (notif.descriptionObj != notif.titleObj)
                    {
                        // 仅在 title/desc 分离时刷新，避免覆盖标题
                        notif.UpdateUI();
                    }
                }
            };
        }

        private static readonly System.Reflection.FieldInfo UseCustomContentField =
            typeof(NotificationManager).GetField("useCustomContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static void ForceUseCustomContent(NotificationManager notif, bool value)
        {
            if (notif == null || UseCustomContentField == null) return;
            UseCustomContentField.SetValue(notif, value);
        }

        #endregion
        
        #region 交互列表

        [Header("交互列表配置")]
        [SerializeField] private ListView interactListView;
        [SerializeField] private ScrollRect interactScrollRect;
        [SerializeField, Range(0.01f, 0.5f)] private float scrollSmoothTime = 0.08f;

        private readonly List<ButtonManager> _activeItems = new();
        private static readonly List<string> EmptyNames = new();
        private float _scrollVelocity;
        private float _targetScrollY;
        private bool _hasTargetScroll;
        
        private void RefreshList(List<string> dropNames, int selectedIndex)
        {
            if (dropNames == null) dropNames = EmptyNames;

            // 1. 回收旧的 Item
            if (interactListView != null && interactListView.itemParent != null)
            {
                for (int i = interactListView.itemParent.childCount - 1; i >= 0; i--)
                {
                    var child = interactListView.itemParent.GetChild(i);
                    child.gameObject.GameObjectPushPool();
                }
            }
            _activeItems.Clear();

            // 2. 生成新的 Item
            for (int i = 0; i < dropNames.Count; i++)
            {
                var dropName = dropNames[i];
                if (string.IsNullOrEmpty(dropName)) continue;

                var go = ProjectUtility.GetOrInstantiateGameObject(interactListView.itemPreset, interactListView.itemParent);
                go.SetActive(true);
                // 配合 JKFrame 等原生 UI 组件保证布局刷新
                go.transform.SetAsLastSibling();

                var btn = go.GetComponent<ButtonManager>();
                var item = go.GetComponent<ListViewItem>();

                if (btn != null)
                {
                    // 设置 ButtonManager 名字兜底
                    btn.SetText(i == selectedIndex ? $"E  {dropName}" : $"   {dropName}");
                    
                    // 强制手动播放高亮效果
                    if (i == selectedIndex)
                    {
                        btn.StartCoroutine("SetHighlight");
                    }
                    else
                    {
                        btn.StartCoroutine("SetNormal");
                    }
                    
                    _activeItems.Add(btn);
                }

                if (item != null)
                {
                    // 设置 ListViewItem 名字（Modern UI Pack 列表专用逻辑）
                    item.rowCount = ListView.RowCount.One;
                    item.row0Ref = new ListView.ListRow
                    {
                        rowText = i == selectedIndex ? $"E  {dropName}" : $"●  {dropName}",
                        rowType = ListView.RowType.Text
                    };
                    item.PassReferences();
                }
            }

            // 3. 强制刷新布局，避免滚动区域不更新
            if (interactListView != null && interactListView.itemParent is RectTransform rt)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                // EnsureSelectedVisible(selectedIndex);
            }
        }
        
        // private void EnsureSelectedVisible(int selectedIndex)
        // {
        //     if (interactScrollRect == null || interactListView == null || interactListView.itemParent == null)
        //         return;
        //
        //     var content = interactListView.itemParent as RectTransform;
        //     if (content == null) return;
        //     if (selectedIndex < 0 || selectedIndex >= content.childCount) return;
        //
        //     var viewport = interactScrollRect.viewport != null
        //         ? interactScrollRect.viewport
        //         : interactScrollRect.GetComponent<RectTransform>();
        //     if (viewport == null) return;
        //
        //     var item = content.GetChild(selectedIndex) as RectTransform;
        //     if (item == null) return;
        //
        //     var layout = content.GetComponent<VerticalLayoutGroup>();
        //     float spacing = layout != null ? layout.spacing : 0f;
        //     float paddingTop = layout != null ? layout.padding.top : 0f;
        //     float paddingBottom = layout != null ? layout.padding.bottom : 0f;
        //
        //     float itemHeight = item.rect.height;
        //     float itemTop = paddingTop + selectedIndex * (itemHeight + spacing);
        //     float itemBottom = itemTop + itemHeight;
        //
        //     float viewportHeight = viewport.rect.height;
        //     float contentHeight = content.rect.height;
        //     float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
        //
        //     float scrollY = Mathf.Clamp(content.anchoredPosition.y, 0f, maxScroll);
        //
        //     if (itemTop < scrollY)
        //         scrollY = itemTop;
        //     else if (itemBottom > scrollY + viewportHeight)
        //         scrollY = itemBottom - viewportHeight;
        //
        //     scrollY = Mathf.Clamp(scrollY, 0f, maxScroll);
        //     _targetScrollY = scrollY;
        //     _hasTargetScroll = true;
        // }
        
        private void SetupScrollContent()
        {
            if (interactListView == null || interactListView.itemParent == null || interactScrollRect == null) return;

            var content = interactListView.itemParent as RectTransform;
            if (content == null) return;

            // 让 Content 按顶部对齐并根据子项高度自动扩展，保证 ScrollRect 可滚动
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 确保 ScrollRect 绑定 Content
            if (interactScrollRect.content == null)
                interactScrollRect.content = content;
        }

        #endregion
        
        #region HP / MP / EXP 进度条
        
        [Header("HP/MP/EXP 进度条")]
        [SerializeField] private ProgressBar hpBar;
        [SerializeField] private ProgressBar mpBar;
        [SerializeField] private ProgressBar expBar;

        /// <summary>
        /// 通过代码修改进度条颜色，避免在 Inspector 设置后被 MUIP UpdateUI 重置。
        /// 只需在 Start / Init 时调用一次，效果就不会再被重置了。
        /// </summary>
        private void InitBarColors()
        {
            if (hpBar?.loadingBar != null) hpBar.loadingBar.color = new Color(0.87f, 0.22f, 0.22f); // 红色
            if (mpBar?.loadingBar != null) mpBar.loadingBar.color = new Color(0.20f, 0.52f, 0.95f); // 蓝色
            if (expBar?.loadingBar != null) expBar.loadingBar.color = new Color(0.98f, 0.79f, 0.10f); // 金黄色
        }

        private void OnHpChanged(float current, float max)
        {
            if (hpBar == null) return;
            hpBar.maxValue = max;
            hpBar.SetValue(current);
        }

        private void OnMpChanged(float current, float max)
        {
            if (mpBar == null) return;
            mpBar.maxValue = max;
            mpBar.SetValue(current);
        }


        /// <summary>
        /// 关闭所有进度条的自动滚动模式（isOn=false / speed=0），
        /// 防止 MUIP ProgressBar 在 Update 里自己一直往上加数值。
        /// </summary>
        private void StopBarsAutoPlay()
        {
            StopBar(hpBar);
            StopBar(mpBar);
            StopBar(expBar);
        }

        private static void StopBar(ProgressBar bar)
        {
            if (bar == null) return;
            bar.isOn = false;
            bar.speed = 0;
        }

        /// <summary>
        /// 获得经验时即时驱动 EXP 进度条 (characterId, currentExp, expToNextLevel)。
        /// </summary>
        private void OnExpGained(int characterId, long currentExp, long expToNextLevel)
        {
            if (expBar == null || DataManager.GameData == null) return;
            if (characterId != DataManager.GameData.SelectedCharacterId) return;

            float max = expToNextLevel > 0 ? expToNextLevel : 100;
            expBar.maxValue = max;
            expBar.SetValue(currentExp % max);
        }

        /// <summary>
        /// 升级时重置经验进度条（currentExp 已重置为升级后剩余量）。
        /// </summary>
        private void OnExpLevelChanged(int characterId, int newLevel)
        {
            if (expBar == null || DataManager.GameData == null) return;
            if (characterId != DataManager.GameData.SelectedCharacterId) return;
            if (DataManager.GameData.CharacterProgressDict == null) return;

            DataManager.GameData.CharacterProgressDict.Dictionary.TryGetValue(characterId, out var progress);
            if (progress == null) return;

            expBar.maxValue = 100;
            expBar.SetValue(progress.Experience);
        }

        /// <summary>
        /// 从存档读取当前角色的等级和当前经验，并使用 LevelGrowthConfig 计算该级所需经验作为上限，
        /// 将经验条初始化到正确的当前进度。
        /// </summary>
        private void RefreshExpBarFromSave()
        {
            if (expBar == null || DataManager.GameData == null) return;

            int charId = DataManager.GameData.SelectedCharacterId;
            if (DataManager.GameData.CharacterProgressDict == null) return;

            DataManager.GameData.CharacterProgressDict.Dictionary.TryGetValue(charId, out var progress);
            if (progress == null) return;

            // 通过 PlayerService 拿角色配置里的 LevelGrowthConfig 计算该级经验上限
            var growthConfig = PlayerService.Instance?.GetCharacterConfig()?.LevelGrowthConfig;
            long expToNext = growthConfig != null
                ? growthConfig.GetExpRequiredForNextLevel(progress.Level)
                : 100;
            if (expToNext <= 0) expToNext = 100; // 满级兜底

            expBar.maxValue = expToNext;
            expBar.SetValue(progress.Experience);
        }

        #endregion
    }
}
