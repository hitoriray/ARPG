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

        #region Test

        [LabelText("是否创建新存档")] public bool isCreateArchive;
        [LabelText("初始角色ID"), ShowIf("isCreateArchive", true)] public int initialCharacterId = 1004;
        
        #endregion

        private InputAction _bagAction;
        
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
            RegisterBagInput();

            #endregion

            // 初始化角色
            await PlayerManager.Instance.InitAsync();
            // 兜底：某些时序下 InputService 可能稍后可用，这里再注册一次 Bag
            RegisterBagInput();
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

            // JKFrame 的 UIWindowData 默认来自 JKFrameSetting.asset；这里做运行时兜底，避免漏配导致背包无法打开。
            UISystem.AddUIWindowData(
                InventoryWindowTypeKey,
                new UIWindowData(InventoryWindowCache, InventoryWindowAssetKey, InventoryWindowLayer));

            JKLog.Warning($"[GameSceneManager] 运行时补注册 UIWindowData: {InventoryWindowTypeKey}（请在编辑器刷新 JKFrameSetting 的 UIWindowDataDic）。");
        }
    }
}
