using System.Collections.Generic;
using Core.Item;
using JKFrame;
using Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Item
{
    /// <summary>
    /// 手动拾取交互管理器（单例 MonoBehaviour）。
    /// 维护玩家附近的手动拾取物候选集合，
    /// 通过 InputSystem Interactive Action 的 performed 事件触发拾取。
    /// 可扩展：NPC 对话、开宝箱等也可走此接口。
    /// </summary>
    public class InteractManager : SingletonMono<InteractManager>
    {
        [Header("交互候选显示半径（用于 Gizmo 调试）")]
        [SerializeField] private float _debugRadius = 3f;

        private readonly HashSet<WorldDropItem> _nearbyDrops = new();

        // ── 生命周期 ──────────────────────────────────────────────

        private void OnEnable()
        {
            var inputMap = InputService.Instance?.inputMap;
            if (inputMap != null)
                inputMap.Player.Interactive.performed += OnInteractPerformed;
        }

        private void OnDisable()
        {
            var inputMap = InputService.Instance?.inputMap;
            if (inputMap != null)
                inputMap.Player.Interactive.performed -= OnInteractPerformed;
        }

        // ── InputSystem 回调 ──────────────────────────────────────

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            TryPickupNearest();
        }

        // ── 注册 / 注销 ──────────────────────────────────────────

        public void RegisterDropItem(WorldDropItem item)
        {
            if (item != null) _nearbyDrops.Add(item);
        }

        public void UnregisterDropItem(WorldDropItem item)
        {
            _nearbyDrops.Remove(item);
        }

        // ── 拾取逻辑 ──────────────────────────────────────────────

        private void TryPickupNearest()
        {
            if (_nearbyDrops.Count == 0) return;
            if (LootDropManager.Instance == null) return;

            var player = PlayerService.Instance?.GetCharacterController();
            if (player == null) return;

            WorldDropItem nearest = null;
            float minSqrDist = float.MaxValue;
            Vector3 playerPos = player.ModelTransform.position;

            foreach (var drop in _nearbyDrops)
            {
                if (drop == null || drop.gameObject == null) continue;
                float sqrDist = (drop.transform.position - playerPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = drop;
                }
            }

            if (nearest != null)
                LootDropManager.Instance.Collect(nearest);
        }

        // ── Gizmo（编辑器可视化）─────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var player = PlayerService.Instance?.GetCharacterController();
            if (player == null) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawSphere(player.ModelTransform.position, _debugRadius);
        }
#endif
    }
}
