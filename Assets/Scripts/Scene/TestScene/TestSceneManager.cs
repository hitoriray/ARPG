using Battle.ECS;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Manager
{
    /// <summary>
    /// 测试场景的初始化管理器。
    /// 与 GameSceneManager 共享同一套存档数据（DataManager），
    /// 不重新创建存档，直接使用切换前已加载的 GameData。
    /// 
    /// 使用方法：
    ///   1. 在 TestScene 中新建 Empty GameObject，命名 "TestSceneManager"
    ///   2. 挂上此脚本，同时挂上 SceneTransferManager（用于回到其他场景）
    ///   3. 运行时会自动初始化玩家、UI 和战斗系统
    /// </summary>
    public class TestSceneManager : SingletonMono<TestSceneManager>
    {
        [Title("Spawn")]
        [SerializeField] private bool forceSpawnAtFixedPoint = true;
        [SerializeField, ShowIf(nameof(forceSpawnAtFixedPoint))] private Transform fixedSpawnPoint;

        private InputAction _bagAction;
        private InputAction _escAction;

        private async void Start()
        {
            try
            {
                // 共享已有存档，如直接从 Test 场景运行（Editor 调试）则创建临时存档
                if (DataManager.GameData == null)
                {
                    JKLog.Warning("[TestSceneManager] 没有存档数据，创建临时存档。");
                    DataManager.CreateArchive(1001);
                }

                GameSettingsManager.Init();

                // 注册场景切换 Lua 函数
                SceneTransferManager.Instance?.Init();

                InventoryManager.InitializeForRuntime();
                EnsureUIWindowsRegistered();
                RegisterBagInput();

                await PlayerManager.Instance.EnsureInitializedAsync();

                if (PlayerManager.Instance?.player == null)
                {
                    JKLog.Error("[TestSceneManager] Player is not available after initialization.");
                    return;
                }

                ApplyFixedSpawnPointIfNeeded();

                RegisterBagInput();
                RegisterEscInput();

                var ecsRunner = BattleEcsRunner.Ensure();
                ecsRunner.RegisterCharacter(PlayerManager.Instance.player);

                if (LootDropManager.Instance != null)
                    LootDropManager.Instance.RestoreScenePersistentDrops();
            }
            finally
            {
                // 通知 UI_LoadingWindow 初始化完成，进度条跑到 100% 并自动关闭
                EventSystem.EventTrigger(GameManager.GameSceneReadyEvent);
            }
        }

        private void OnDestroy()
        {
            UnregisterBagInput();
            UnregisterEscInput();

            // 切场景前保存玩家位置
            if (PlayerManager.TryGetLatestPlayerWorldPosition(out var pos))
            {
                var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                DataManager.SavePlayerPosition(pos, sceneName);
            }

            DataManager.SaveGameData();
        }

        // ── 输入注册 ──────────────────────────────────────────────────

        private void RegisterBagInput()
        {
            _bagAction = InputService.Instance?.inputMap?.UI.Bag;
            if (_bagAction == null) return;
            _bagAction.performed -= OnBagPerformed;
            _bagAction.performed += OnBagPerformed;
        }

        private void UnregisterBagInput()
        {
            if (_bagAction == null) return;
            _bagAction.performed -= OnBagPerformed;
            _bagAction = null;
        }

        private void RegisterEscInput()
        {
            _escAction = InputService.Instance?.inputMap?.Global.ESC;
            if (_escAction == null) return;
            _escAction.performed -= OnEscPerformed;
            _escAction.performed += OnEscPerformed;
        }

        private void UnregisterEscInput()
        {
            if (_escAction == null) return;
            _escAction.performed -= OnEscPerformed;
            _escAction = null;
        }

        private void OnEscPerformed(InputAction.CallbackContext ctx)
        {
            UIModalStack.CloseTop();
        }

        private void OnBagPerformed(InputAction.CallbackContext ctx)
        {
            const string key = "UI.UI_InventoryWindow";
            var win = UISystem.GetWindow(key);
            if (win != null && win.UIEnable)
                UISystem.Close(key);
            else
                UISystem.Show(key);
        }

        // ── UI 注册 ───────────────────────────────────────────────────

        private void EnsureUIWindowsRegistered()
        {
            TryRegister("UI.UI_InventoryWindow", "UI_InventoryWindow", 1, true);
            TryRegister("UI.UI_DialogWindow", "UI_DialogWindow", 1, true);
            TryRegister("UI.UI_GameSettingsWindow", "UI_GameSettingsWindow", 2, true);
        }

        private static void TryRegister(string typeKey, string assetKey, int layer, bool cache)
        {
            if (UISystem.TryGetUIWindowData(typeKey, out _)) return;
            UISystem.AddUIWindowData(typeKey, new UIWindowData(cache, assetKey, layer));
        }

        private void ApplyFixedSpawnPointIfNeeded()
        {
            if (!forceSpawnAtFixedPoint || fixedSpawnPoint == null)
            {
                return;
            }

            var player = PlayerManager.Instance?.player;
            if (player == null)
            {
                return;
            }

            var controller = player.controller;
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(fixedSpawnPoint.position, fixedSpawnPoint.rotation);
            player.ChangeVerticalSpeed(0f);
            player.ClearHorizontalVelocity();

            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }
        }
    }
}
