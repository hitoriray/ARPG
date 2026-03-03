using System;
using System.Collections.Generic;
using Config;
using JKFrame;
using Manager;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UI
{
    [UIWindowData(typeof(UI_InventoryWindow), true, nameof(UI_InventoryWindow), 1)]
    public class UI_InventoryWindow : UI_WindowBase
    {
        private const string InventoryDropRequestEvent = "InventoryDropRequest";

        [Serializable]
        private struct TabFilterRule
        {
            public int windowIndex;
            public bool enableFilter;
            public ItemType filterType;
        }

        [Serializable]
        private sealed class SubWindowBinding
        {
            public int windowIndex;
            [Header("背包列表（普通 Scroll + GridLayoutGroup）")]
            public RectTransform inventoryContent;
            public GameObject inventoryItemPrefab;
            public TextMeshProUGUI itemNameText;
            public TextMeshProUGUI itemDescText;
            public ButtonManager btnUse;
            public ButtonManager btnDrop;


            [NonSerialized] public UnityAction useAction;
            [NonSerialized] public UnityAction dropAction;

            public RectTransform ResolveListContent()
            {
                if (inventoryContent != null) return inventoryContent;
                return null;
            }

            public GameObject ResolveItemPrefab()
            {
                if (inventoryItemPrefab != null) return inventoryItemPrefab;
                return null;
            }
        }

        private sealed class SubWindowContext
        {
            public int WindowIndex;
            public string WindowName;
            public ItemType? FilterType;

            public RectTransform InventoryContent;
            public GameObject InventoryItemPrefab;
            public TextMeshProUGUI ItemNameText;
            public TextMeshProUGUI ItemDescText;
            public ButtonManager BtnUse;
            public ButtonManager BtnDrop;

            public readonly List<InventoryItemViewData> Snapshot = new();
            public readonly List<ButtonManager> ActiveButtons = new();
            public readonly Dictionary<ButtonManager, int> ButtonItemMap = new();

            public int SelectedItemId = -1;
        }

        [Header("主容器")]
        [SerializeField] private WindowManager windowManager;

        [Header("子窗口引用（请拖拽绑定，避免重名查找误判）")]
        [SerializeField] private List<SubWindowBinding> subWindowBindings = new();

        [Header("页签过滤（可选，不配则按页签名自动识别）")]
        [SerializeField] private List<TabFilterRule> tabFilterRules = new();

        [Header("丢弃到地面")]
        [SerializeField, Min(0.5f)] private float dropForwardDistance = 3.2f;
        [SerializeField] private float dropHeight = 0.35f;
        [SerializeField, Min(0f)] private float dropLockDelay = 0.15f;

        [Header("文案")]
        [SerializeField] private string emptyNameText = "未选择";
        [SerializeField] private string emptyDescText = "请选择一个背包物品";

        private readonly List<SubWindowContext> _contexts = new();
        private readonly Dictionary<int, Sprite> _iconCache = new();
        private int _currentWindowIndex;

        public override void Init()
        {
            base.Init();
            ResolveReferencesIfNeeded();
            BuildSubWindowContexts();
            BindWindowChangeEvent();
        }

        public override void OnShow()
        {
            base.OnShow();
            ResolveReferencesIfNeeded();
            BuildSubWindowContexts();

            _currentWindowIndex = windowManager != null
                ? Mathf.Clamp(windowManager.currentWindowIndex, 0, Math.Max(0, _contexts.Count - 1))
                : 0;

            RefreshCurrentWindow();
        }

        protected override void RegisterEventListener()
        {
            base.RegisterEventListener();
            InventoryManager.OnInventoryListChanged += OnInventoryListChanged;
        }

        protected override void UnRegisterEventListener()
        {
            base.UnRegisterEventListener();
            InventoryManager.OnInventoryListChanged -= OnInventoryListChanged;
        }

        private void OnDestroy()
        {
            if (windowManager != null)
            {
                windowManager.onWindowChange.RemoveListener(OnWindowChanged);
            }
        }

        private void ResolveReferencesIfNeeded()
        {
            if (windowManager == null)
            {
                windowManager = GetComponentInChildren<WindowManager>(true);
            }
        }

        private void BindWindowChangeEvent()
        {
            if (windowManager == null) return;

            windowManager.onWindowChange.RemoveListener(OnWindowChanged);
            windowManager.onWindowChange.AddListener(OnWindowChanged);
        }

        private void BuildSubWindowContexts()
        {
            _contexts.Clear();
            if (windowManager == null || windowManager.windows == null) return;

            for (int i = 0; i < windowManager.windows.Count; i++)
            {
                var item = windowManager.windows[i];
                if (item == null || item.windowObject == null) continue;

                var binding = GetBinding(i);
                if (binding == null)
                {
                    JKLog.Warning($"[InventoryUI] 页签 {i}:{item.windowName} 未配置 SubWindowBinding，已跳过。");
                    continue;
                }

                RectTransform content = binding.ResolveListContent();
                GameObject itemPrefab = binding.ResolveItemPrefab();
                if (content == null)
                {
                    JKLog.Warning($"[InventoryUI] 页签 {i}:{item.windowName} 缺少 inventoryContent 绑定，已跳过。");
                    continue;
                }

                if (itemPrefab == null)
                {
                    JKLog.Warning($"[InventoryUI] 页签 {i}:{item.windowName} 缺少 inventoryItemPrefab 绑定，已跳过。");
                    continue;
                }

                var ctx = new SubWindowContext
                {
                    WindowIndex = i,
                    WindowName = item.windowName,
                    FilterType = ResolveFilterType(i, item.windowName),
                    InventoryContent = content,
                    InventoryItemPrefab = itemPrefab,
                    ItemNameText = binding.itemNameText,
                    ItemDescText = binding.itemDescText,
                    BtnUse = binding.btnUse,
                    BtnDrop = binding.btnDrop
                };

                BindActionButtons(ctx, binding);
                _contexts.Add(ctx);
            }

            if (_contexts.Count == 0)
            {
                JKLog.Warning("[InventoryUI] 未构建任何子页签，请检查 WindowManager.windows[x].windowObject 与 subWindowBindings 配置。");
            }
        }

        private void BindActionButtons(SubWindowContext ctx, SubWindowBinding binding)
        {
            if (ctx.BtnUse != null)
            {
                if (binding.useAction != null)
                {
                    ctx.BtnUse.onClick.RemoveListener(binding.useAction);
                }

                binding.useAction = () => OnUseClicked(ctx);
                ctx.BtnUse.onClick.AddListener(binding.useAction);
            }

            if (ctx.BtnDrop != null)
            {
                if (binding.dropAction != null)
                {
                    ctx.BtnDrop.onClick.RemoveListener(binding.dropAction);
                }

                binding.dropAction = () => OnDropClicked(ctx);
                ctx.BtnDrop.onClick.AddListener(binding.dropAction);
            }
        }

        private ItemType? ResolveFilterType(int windowIndex, string windowName)
        {
            for (int i = 0; i < tabFilterRules.Count; i++)
            {
                var rule = tabFilterRules[i];
                if (rule.windowIndex != windowIndex) continue;
                return rule.enableFilter ? rule.filterType : null;
            }

            return GuessFilterTypeByWindowName(windowName);
        }

        private static ItemType? GuessFilterTypeByWindowName(string windowName)
        {
            if (string.IsNullOrEmpty(windowName)) return null;

            string name = windowName.ToLowerInvariant();
            if (name.Contains("all") || windowName.Contains("全部")) return null;
            if (name.Contains("consum") || windowName.Contains("消耗")) return ItemType.Consumable;
            if (name.Contains("material") || windowName.Contains("材料")) return ItemType.Material;
            if (name.Contains("equip") || windowName.Contains("装备")) return ItemType.Equipment;
            if (name.Contains("key") || windowName.Contains("关键") || windowName.Contains("任务")) return ItemType.KeyItem;
            if (name.Contains("gold") || windowName.Contains("金币")) return ItemType.Gold;

            return null;
        }

        private void OnWindowChanged(int index)
        {
            _currentWindowIndex = index;
            RefreshCurrentWindow();
        }

        private void OnInventoryListChanged()
        {
            RefreshCurrentWindow();
        }

        private void RefreshCurrentWindow()
        {
            if (_contexts.Count == 0) return;
            if (TryGetContextByWindowIndex(_currentWindowIndex, out var ctx))
            {
                RefreshSubWindow(ctx);
            }
            else
            {
                RefreshSubWindow(_contexts[0]);
            }
        }

        private void RefreshSubWindow(SubWindowContext ctx)
        {
            if (ctx.InventoryContent == null || ctx.InventoryItemPrefab == null)
            {
                JKLog.Warning($"[InventoryUI] 子窗口 {ctx.WindowName} 缺少列表 content/itemPrefab 引用，跳过刷新。");
                return;
            }

            InventoryManager.FillSnapshot(
                ctx.Snapshot,
                ctx.FilterType,
                InventorySortMode.ByTypeThenId);

            if (!ContainsItem(ctx.Snapshot, ctx.SelectedItemId))
            {
                ctx.SelectedItemId = ctx.Snapshot.Count > 0 ? ctx.Snapshot[0].ItemId : -1;
            }

            // 回收旧 Item
            for (int i = ctx.InventoryContent.childCount - 1; i >= 0; i--)
            {
                var child = ctx.InventoryContent.GetChild(i);
                child.gameObject.GameObjectPushPool();
            }

            ctx.ActiveButtons.Clear();
            ctx.ButtonItemMap.Clear();

            // 生成新 Item
            for (int i = 0; i < ctx.Snapshot.Count; i++)
            {
                var data = ctx.Snapshot[i];
                var go = ProjectUtility.GetOrInstantiateGameObject(ctx.InventoryItemPrefab, ctx.InventoryContent);
                go.SetActive(true);
                go.transform.SetAsLastSibling();

                var btn = go.GetComponent<ButtonManager>();
                if (btn == null)
                {
                    btn = go.GetComponentInChildren<ButtonManager>(true);
                }
                if (btn == null) continue;

                ctx.ActiveButtons.Add(btn);
                ctx.ButtonItemMap[btn] = data.ItemId;

                BindItemButton(ctx, btn, data);
            }

            RefreshInfoPanel(ctx);
            RebuildListLayout(ctx);
        }

        private void BindItemButton(SubWindowContext ctx, ButtonManager btn, InventoryItemViewData data)
        {
            int itemId = data.ItemId;
            bool isSelected = itemId == ctx.SelectedItemId;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnItemClicked(ctx, itemId));
            btn.SetText($"{data.DisplayName} x{data.Count}");

            if (isSelected)
                btn.StartCoroutine("SetHighlight");
            else
                btn.StartCoroutine("SetNormal");

            // 图标异步加载（有缓存直接用）
            if (data.Config != null && data.Config.Icon != null && data.Config.Icon.RuntimeKeyIsValid())
            {
                if (_iconCache.TryGetValue(data.ItemId, out var cached) && cached != null)
                {
                    btn.SetIcon(cached);
                }
                else
                {
                    var handle = data.Config.Icon.LoadAssetAsync<Sprite>();
                    handle.Completed += (AsyncOperationHandle<Sprite> op) =>
                    {
                        if (op.Status != AsyncOperationStatus.Succeeded) return;
                        var sprite = op.Result;
                        if (sprite == null) return;

                        _iconCache[data.ItemId] = sprite;

                        if (btn == null) return;
                        if (!ctx.ButtonItemMap.TryGetValue(btn, out int currentItemId)) return;
                        if (currentItemId != data.ItemId) return;

                        btn.SetIcon(sprite);
                    };
                }
            }
        }

        private void OnItemClicked(SubWindowContext ctx, int itemId)
        {
            ctx.SelectedItemId = itemId;
            RefreshSubWindow(ctx);
        }

        private void OnUseClicked(SubWindowContext ctx)
        {
            if (!TryGetSelectedItem(ctx, out var data)) return;
            if (data.Config == null) return;

            var attr = PlayerService.Instance?.GetCharacterController()?.CharacterAttribute;
            if (data.Config.ItemType != ItemType.Consumable || attr == null)
            {
                JKLog.Warning($"[InventoryUI] {data.DisplayName} 不是可使用消耗品。");
                return;
            }

            InventoryManager.UseConsumable(data.Config, attr);
        }

        private void OnDropClicked(SubWindowContext ctx)
        {
            if (!TryGetSelectedItem(ctx, out var data)) return;
            if (data.Config == null)
            {
                JKLog.Warning($"[InventoryUI] 物品 {data.ItemId} 缺少配置，无法丢弃到地面。");
                return;
            }
            if (data.Count <= 0) return;

            var player = PlayerService.Instance?.GetCharacterController();
            if (player == null || player.ModelTransform == null)
            {
                JKLog.Warning("[InventoryUI] 玩家对象为空，无法丢弃。");
                return;
            }

            Vector3 forward = player.ModelTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 dropPos = player.ModelTransform.position + forward * dropForwardDistance + Vector3.up * dropHeight;
            EventSystem.EventTrigger<ItemConfig, int, Vector3, float>(
                InventoryDropRequestEvent,
                data.Config,
                1,
                dropPos,
                dropLockDelay);
        }

        private void RefreshInfoPanel(SubWindowContext ctx)
        {
            if (ctx.ItemNameText == null || ctx.ItemDescText == null)
                return;

            if (!TryGetSelectedItem(ctx, out var data))
            {
                ctx.ItemNameText.text = emptyNameText;
                ctx.ItemDescText.text = emptyDescText;
                SetActionInteractable(ctx, false, false);
                return;
            }

            string name = data.DisplayName;
            string desc = data.Config != null ? data.Config.Description : "该物品配置缺失";
            if (string.IsNullOrEmpty(desc)) desc = "暂无描述";
            desc += $"\n数量: {data.Count}";

            ctx.ItemNameText.text = name;
            ctx.ItemDescText.text = desc;

            bool canUse = data.Config != null
                          && data.Config.ItemType == ItemType.Consumable
                          && data.Count > 0;
            bool canDrop = data.Count > 0;
            SetActionInteractable(ctx, canUse, canDrop);
        }

        private static void SetActionInteractable(SubWindowContext ctx, bool canUse, bool canDrop)
        {
            if (ctx.BtnUse != null) ctx.BtnUse.Interactable(canUse);
            if (ctx.BtnDrop != null) ctx.BtnDrop.Interactable(canDrop);
        }

        private static bool ContainsItem(List<InventoryItemViewData> snapshot, int itemId)
        {
            if (itemId < 0) return false;
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].ItemId == itemId) return true;
            }
            return false;
        }

        private static bool TryGetSelectedItem(SubWindowContext ctx, out InventoryItemViewData data)
        {
            for (int i = 0; i < ctx.Snapshot.Count; i++)
            {
                if (ctx.Snapshot[i].ItemId == ctx.SelectedItemId)
                {
                    data = ctx.Snapshot[i];
                    return true;
                }
            }

            data = default;
            return false;
        }

        private bool TryGetContextByWindowIndex(int windowIndex, out SubWindowContext context)
        {
            for (int i = 0; i < _contexts.Count; i++)
            {
                if (_contexts[i].WindowIndex == windowIndex)
                {
                    context = _contexts[i];
                    return true;
                }
            }

            context = null;
            return false;
        }

        private SubWindowBinding GetBinding(int windowIndex)
        {
            for (int i = 0; i < subWindowBindings.Count; i++)
            {
                var binding = subWindowBindings[i];
                if (binding == null) continue;
                if (binding.windowIndex == windowIndex) return binding;
            }

            return null;
        }

        private static void RebuildListLayout(SubWindowContext ctx)
        {
            var rt = ctx.InventoryContent;
            if (rt == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
