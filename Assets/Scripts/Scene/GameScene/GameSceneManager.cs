using Battle.ECS;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

namespace Manager
{
    public class GameSceneManager : SingletonMono<GameSceneManager>
    {
        private const string InventoryWindowTypeKey = "UI.UI_InventoryWindow";
        private const string InventoryWindowAssetKey = "UI_InventoryWindow";
        private const int InventoryWindowLayer = 1;
        private const bool InventoryWindowCache = true;

        private const string DialogWindowTypeKey = "UI.UI_DialogWindow";
        private const string DialogWindowAssetKey = "UI_DialogWindow";
        private const int DialogWindowLayer = 1;
        private const bool DialogWindowCache = true;

        #region Test

        [LabelText("是否创建新存档")] public bool isCreateArchive;
        [LabelText("初始角色ID"), ShowIf("isCreateArchive", true)] public int initialCharacterId = 1004;
        
        #endregion

        private InputAction _bagAction;
        private InputAction _escAction;
        
        private async void Start()
        {
            #region 测试逻辑

            if (isCreateArchive)
            {
                DataManager.CreateArchive(initialCharacterId);
            }
            else
            {
                // 正常游戏流程：检查是否有存档
                if (DataManager.HasArchive)
                {
                    if (!DataManager.LoadCurrentArchive())
                    {
                        JKLog.Warning("[GameSceneManager] 存档读取失败，创建新存档...");
                        DataManager.CreateArchive(initialCharacterId);
                    }
                }
                else
                {
                    // 没有存档，创建新存档（默认角色ID 1001）
                    JKLog.Warning("[GameSceneManager] 未找到存档，创建新存档...");
                    DataManager.CreateArchive(initialCharacterId);
                }
            }

            // ⚠️ 安全检查：确保 GameData 已正确初始化
            if (DataManager.GameData == null)
            {
                JKLog.Error("[GameSceneManager] GameData 为空！强制创建新存档...");
                DataManager.CreateArchive(1001);
            }

            // 初始化背包运行时数据（修复脏数据 + 推送一次全量刷新事件）
            InventoryManager.InitializeForRuntime();
            EnsureInventoryWindowDataRegistered();
            EnsureDialogWindowDataRegistered();
            RegisterBagInput();

            #endregion

            // 初始化角色
            await PlayerManager.Instance.InitAsync();
            // 兜底：某些时序下 InputService 可能稍后可用，这里再注册一次 Bag
            RegisterBagInput();
            RegisterEscInput();
            RayDebug.Info($"游戏开始！当前角色ID: {DataManager.GameData.SelectedCharacterId}");
            // 初始化ECS并注册玩家
            var ecsRunner = BattleEcsRunner.Ensure();
            ecsRunner.RegisterCharacter(PlayerManager.Instance.player);
            
            // 恢复当前场景所有无限时长掉落物
            if (Manager.LootDropManager.Instance != null)
            {
                Manager.LootDropManager.Instance.RestoreScenePersistentDrops();
            }
        }

        private void OnDestroy()
        {
            UnregisterBagInput();
            UnregisterEscInput();

            // TODO：模拟游戏退出时的存档
            DataManager.SaveGameData();
        }

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

        /// <summary>
        /// 注册全局 ESC 输入监听。
        /// 前提：用户已在 InputMap 中添加一个名为 "Global" 的 ActionMap，其中包含 "ESC" Action。
        /// </summary>
        private void RegisterEscInput()
        {
            // TODO: 用户在 InputMap 里添加 Global ActionMap 之后，将下面一行注释去掉并替换为实际路径
            // _escAction = InputService.Instance?.inputMap?.Global.ESC;
            // 目前先用 UI.ESC 公测，要求：UI ActionMap 需要在小对话框是否打开时进行切换
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

        private void OnEscPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            UIModalStack.CloseTop();
        }

        private void OnBagPerformed(InputAction.CallbackContext ctx)
        {
            const string windowKey = InventoryWindowTypeKey;
            EnsureInventoryWindowDataRegistered();

            var inventoryWindow = UISystem.GetWindow(windowKey);
            if (inventoryWindow != null && inventoryWindow.UIEnable)
            {
                UISystem.Close(windowKey);
            }
            else
            {
                UISystem.Show(windowKey);
            }
        }

        private void EnsureInventoryWindowDataRegistered()
        {
            if (UISystem.TryGetUIWindowData(InventoryWindowTypeKey, out _))
            {
                return;
            }

            UISystem.AddUIWindowData(
                InventoryWindowTypeKey,
                new UIWindowData(InventoryWindowCache, InventoryWindowAssetKey, InventoryWindowLayer));

            JKLog.Warning($"[GameSceneManager] 运行时补注册 UIWindowData: {InventoryWindowTypeKey}");
        }

        private void EnsureDialogWindowDataRegistered()
        {
            if (UISystem.TryGetUIWindowData(DialogWindowTypeKey, out _))
            {
                return;
            }

            UISystem.AddUIWindowData(
                DialogWindowTypeKey,
                new UIWindowData(DialogWindowCache, DialogWindowAssetKey, DialogWindowLayer));

            JKLog.Warning($"[GameSceneManager] 运行时补注册 UIWindowData: {DialogWindowTypeKey}");
        }
    }
}
