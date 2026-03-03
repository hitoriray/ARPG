using System.Collections.Generic;
using System.Linq;
using Config;
using JKFrame;
using Manager;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Item
{
    /// <summary>
    /// 手动拾取交互管理器（单例 MonoBehaviour）。
    /// 维护玩家附近的手动拾取物候选集合（有序列表），
    /// 支持鼠标滚轮切换当前选中项，并通过 Interactive Action 的 performed 事件触发拾取。
    /// 可扩展：NPC 对话、开宝箱等也可走此接口。
    /// </summary>
    public class InteractManager : SingletonMono<InteractManager>
    {
        private readonly List<WorldDropItem> _nearbyDrops = new();
        private readonly List<string> _nearbyDropNames = new();
        private readonly List<DropGroup> _groupedDrops = new();
        private int _selectedIndex = 0;

        private sealed class DropGroup
        {
            public ItemConfig Config;
            public int TotalCount;
            public readonly List<WorldDropItem> Items = new();
        }

        // ── 生命周期 ──────────────────────────────────────────────

        private void OnEnable()
        {
            var inputMap = InputService.Instance?.inputMap;
            if (inputMap != null)
            {
                inputMap.Player.Interactive.performed += OnInteractPerformed;
                inputMap.Player.Scroll.performed += OnScrollPerformed;
            }
            EventSystem.AddEventListener("RequestInteractListUpdate", PublishListUpdate);
        }

        private void OnDisable()
        {
            var inputMap = InputService.Instance?.inputMap;
            if (inputMap != null)
            {
                inputMap.Player.Interactive.performed -= OnInteractPerformed;
                inputMap.Player.Scroll.performed -= OnScrollPerformed;
            }
            EventSystem.RemoveEventListener("RequestInteractListUpdate", PublishListUpdate);

            _nearbyDrops.Clear();
            _nearbyDropNames.Clear();
            _groupedDrops.Clear();
            _selectedIndex = 0;
        }

        private void PublishListUpdate()
        {
            RebuildGroups();

            _nearbyDropNames.Clear();
            foreach (var group in _groupedDrops)
            {
                if (group?.Config == null) continue;
                var displayCount = group.TotalCount;
                var label = displayCount > 1
                    ? $"{group.Config.ItemName} x{displayCount}"
                    : group.Config.ItemName;
                _nearbyDropNames.Add(label);
            }

            if (_groupedDrops.Count == 0)
            {
                _selectedIndex = 0;
            }
            else
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _groupedDrops.Count - 1);
            }

            EventSystem.EventTrigger("UpdateInteractList", _nearbyDropNames, _selectedIndex);
        }

        // ── InputSystem 回调 ──────────────────────────────────────

        private void OnScrollPerformed(InputAction.CallbackContext ctx)
        {
            if (_groupedDrops.Count <= 1) return;

            Vector2 scroll = ctx.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) < 0.1f) return;

            // 往下滚 (y < 0) -> 列表向下选择 (Index ++)
            // 往上滚 (y > 0) -> 列表向上选择 (Index --)
            int newIndex = _selectedIndex;
            if (scroll.y > 0)
                newIndex--;
            else
                newIndex++;

            newIndex = Mathf.Clamp(newIndex, 0, _groupedDrops.Count - 1);
            if (newIndex == _selectedIndex) return;

            _selectedIndex = newIndex;
            PublishListUpdate();
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            TryPickupSelected();
        }

        // ── 注册 / 注销 ──────────────────────────────────────────

        public void RegisterDropItem(WorldDropItem item)
        {
            if (item != null && !_nearbyDrops.Contains(item))
            {
                bool wasEmpty = _nearbyDrops.Count == 0;
                _nearbyDrops.Add(item);
                
                if (wasEmpty)
                {
                    // UISystem.Show<UI_InteractListWindow>();
                }
                
                PublishListUpdate();
            }
        }

        public void UnregisterDropItem(WorldDropItem item)
        {
            if (_nearbyDrops.Remove(item))
            {
                // 超界保护
                if (_selectedIndex >= _nearbyDrops.Count)
                {
                    _selectedIndex = Mathf.Max(0, _nearbyDrops.Count - 1);
                }
                
                if (_nearbyDrops.Count > 0)
                    PublishListUpdate();
            }
        }

        // ── 拾取逻辑 ──────────────────────────────────────────────

        private void TryPickupSelected()
        {
            RebuildGroups();
            if (_groupedDrops.Count == 0) return;
            if (LootDropManager.Instance == null) return;

            // _selectedIndex 防御性校验
            if (_selectedIndex < 0 || _selectedIndex >= _groupedDrops.Count)
            {
                _selectedIndex = 0;
            }

            var group = _groupedDrops[_selectedIndex];
            if (group == null || group.Config == null) return;

            int currentCount = InventoryManager.GetCount(group.Config.ItemId);
            int remaining = group.Config.MaxStackCount - currentCount;
            if (remaining <= 0)
            {
                JKLog.Warning($"[Interact] {group.Config.ItemName} 已达最大堆叠上限 {group.Config.MaxStackCount}");
                return;
            }

            var player = PlayerService.Instance?.GetCharacterController();
            if (player != null)
            {
                var playerPos = player.ModelTransform.position;
                group.Items.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    float da = (a.transform.position - playerPos).sqrMagnitude;
                    float db = (b.transform.position - playerPos).sqrMagnitude;
                    return da.CompareTo(db);
                });
            }

            int totalAdded = 0;
            for (int i = 0; i < group.Items.Count; i++)
            {
                if (remaining <= 0) break;
                var item = group.Items[i];
                if (item == null || item.Config == null) continue;

                int take = Mathf.Min(item.Count, remaining);
                int realAdd = InventoryManager.AddItem(item.Config, take);
                if (realAdd <= 0) break;

                totalAdded += realAdd;
                remaining -= realAdd;

                if (realAdd >= item.Count)
                {
                    _nearbyDrops.Remove(item);
                    LootDropManager.Instance.RemoveDrop(item);
                }
                else
                {
                    item.SetCount(item.Count - realAdd);
                }
            }

            if (totalAdded > 0)
            {
                var ui = UISystem.GetWindow<UI_GameSceneMainWindow>();
                if (ui != null)
                    ui.ShowPickupNotification(group.Config, totalAdded);
            }

            PublishListUpdate();
        }

        private void RebuildGroups()
        {
            _groupedDrops.Clear();

            // 清理失效项
            for (int i = _nearbyDrops.Count - 1; i >= 0; i--)
            {
                var drop = _nearbyDrops[i];
                if (drop == null || drop.Config == null)
                {
                    _nearbyDrops.RemoveAt(i);
                }
            }

            if (_nearbyDrops.Count == 0) return;

            var indexByItemId = new Dictionary<int, int>(_nearbyDrops.Count);
            foreach (var drop in _nearbyDrops)
            {
                if (drop == null || drop.Config == null) continue;
                int itemId = drop.Config.ItemId;
                if (!indexByItemId.TryGetValue(itemId, out int groupIndex))
                {
                    groupIndex = _groupedDrops.Count;
                    indexByItemId[itemId] = groupIndex;
                    _groupedDrops.Add(new DropGroup { Config = drop.Config });
                }

                var group = _groupedDrops[groupIndex];
                group.Items.Add(drop);
                group.TotalCount += Mathf.Max(1, drop.Count);
            }
        }

        private WorldDropItem PickBestItemFromGroup(DropGroup group)
        {
            if (group == null || group.Items.Count == 0) return null;

            var player = PlayerService.Instance?.GetCharacterController();
            if (player == null) return group.Items[0];

            var playerPos = player.ModelTransform.position;
            WorldDropItem best = null;
            float bestSqr = float.MaxValue;
            foreach (var item in group.Items)
            {
                if (item == null) continue;
                float sqr = (item.transform.position - playerPos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = item;
                }
            }

            return best ?? group.Items.FirstOrDefault();
        }
    }
}
