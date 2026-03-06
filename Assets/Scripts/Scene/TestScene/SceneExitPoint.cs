using System.Collections.Generic;
using JKFrame;
using Manager;
using UI;
using UnityEngine;

namespace Scene
{
    /// <summary>
    /// 场景出口交互点（挂在 Test 等子场景中）。
    ///
    /// 使用方法：
    ///   1. 新建 Empty GameObject，添加 Sphere/Box Collider（勾选 Is Trigger）
    ///   2. 挂上此脚本，填写 displayName（交互列表中显示的名字）和 targetSceneName（目标场景名）
    ///   3. 确保玩家 GameObject 的 Tag 为 "Player"
    ///
    /// 效果：
    ///   玩家进入触发器 → 交互列表出现"返回主城"（或你配置的名字）
    ///   点击列表按钮   → 弹出 Confirm 窗口
    ///   点击确认       → 调用 GameManager.LoadSceneWithLoading 切换到目标场景
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneExitPoint : MonoBehaviour
    {
        [Header("交互配置")]
        [Tooltip("在交互列表中显示的名称，如：返回主城")]
        [SerializeField] private string displayName = "返回";

        [Tooltip("目标场景名（须在 Build Settings 中注册）")]
        [SerializeField] private string targetSceneName = "Game";

        [Header("Confirm 窗口文本")]
        [SerializeField] private string confirmTitle   = "离开场景";
        [SerializeField] private string confirmMessage = "确定要离开当前场景吗？";

        [Header("检测")]
        [SerializeField] private string playerTag = "Player";

        // ── 内部状态 ─────────────────────────────────────────────────
        private bool _playerInRange;
        private bool _lastInteractive;

        // 与 NpcController 共享同一个事件 key，使 UI_GameSceneMainWindow 统一渲染
        private const string UpdateInteractListEvent = "UpdateInteractList";

        // ── 生命周期 ─────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _playerInRange = true;
            BroadcastInteractList();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _playerInRange = false;
            _lastInteractive = false;
            BroadcastInteractList();
        }

        private void Update()
        {
            if (!_playerInRange) return;

            bool interactive = InputService.Instance != null && InputService.Instance.Interactive;
            
            // 按下交互键且未弹出其他窗口时触发
            // 如果 Confirm Window 已经出来了，就不重复弹
            if (interactive && !_lastInteractive)
            {
                if (UISystem.GetWindow("UI.UI_ConfirmWindow") == null || 
                    !UISystem.GetWindow("UI.UI_ConfirmWindow").UIEnable)
                {
                    OnInteract();
                }
            }
            
            _lastInteractive = interactive;
        }

        private void OnDestroy()
        {
            if (_playerInRange)
            {
                _playerInRange = false;
                BroadcastInteractList();
            }
        }

        // ── 交互触发（由 UI_GameSceneMainWindow 按 E 后回调） ────────

        /// <summary>
        /// UI_GameSceneMainWindow 的交互按钮点击时调用此方法。
        /// 在 Inspector 里把按钮 OnClick 指向此方法，或通过 Event 触发。
        /// </summary>
        public void OnInteract()
        {
            if (!_playerInRange) return;

            var confirmWindow = UISystem.Show<UI_ConfirmWindow>();
            confirmWindow.Show(
                confirmTitle,
                confirmMessage,
                confirmAction: () => GameManager.Instance.LoadSceneWithLoading(targetSceneName, true),
                cancelAction: null
            );
        }

        // ── 广播 ─────────────────────────────────────────────────────

        private void BroadcastInteractList()
        {
            var names = new List<string>();
            if (_playerInRange)
                names.Add(displayName);

            EventSystem.EventTrigger(UpdateInteractListEvent, names, 0);
        }

        // ── Gizmos ────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is SphereCollider sc)
            {
                Gizmos.DrawSphere(sc.center, sc.radius);
                Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
                Gizmos.DrawWireSphere(sc.center, sc.radius);
            }
            else if (col is BoxCollider bc)
            {
                Gizmos.DrawCube(bc.center, bc.size);
                Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
                Gizmos.DrawWireCube(bc.center, bc.size);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"[Exit] → {targetSceneName}",
                new GUIStyle { normal = { textColor = new Color(1f, 0.6f, 0f) }, fontSize = 11 });
#endif
        }
    }
}
