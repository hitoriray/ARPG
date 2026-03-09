using Battle.ECS;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

namespace Manager
{
    public class GameSceneManager : SingletonMono<GameSceneManager>
    {
        private const string GameSceneName = "Game";
        private const string InventoryWindowTypeKey = "UI.UI_InventoryWindow";
        private const string InventoryWindowAssetKey = "UI_InventoryWindow";
        private const int InventoryWindowLayer = 1;
        private const bool InventoryWindowCache = true;

        private const string DialogWindowTypeKey = "UI.UI_DialogWindow";
        private const string DialogWindowAssetKey = "UI_DialogWindow";
        private const int DialogWindowLayer = 1;
        private const bool DialogWindowCache = true;
        
        private const string SettingsWindowTypeKey = "UI.UI_GameSettingsWindow";
        private const string SettingsWindowAssetKey = "UI_GameSettingsWindow";
        private const int SettingsWindowLayer = 2;
        private const bool SettingsWindowCache = true;

        #region Test

        [LabelText("Create New Archive")] public bool isCreateArchive;
        [LabelText("Initial Character ID"), ShowIf("isCreateArchive", true)] public int initialCharacterId = 1004;
        
        #endregion

        private InputAction _bagAction;
        private InputAction _escAction;

        private async void Start()
        {
            try
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                RayDebug.Info($"[GameSceneManager] Start: scene={sceneName}, isCreateArchive={isCreateArchive}");

                if (isCreateArchive)
                {
                    DataManager.CreateArchive(initialCharacterId);
                }
                else
                {
                    if (DataManager.HasArchive)
                    {
                        if (!DataManager.LoadCurrentArchive())
                        {
                            JKLog.Warning("[GameSceneManager] Archive load failed, creating a new archive.");
                            DataManager.CreateArchive(initialCharacterId);
                        }
                    }
                    else
                    {
                        JKLog.Warning("[GameSceneManager] Archive not found, creating a new archive.");
                        DataManager.CreateArchive(initialCharacterId);
                    }
                }

                if (DataManager.GameData == null)
                {
                    JKLog.Error("[GameSceneManager] GameData is null, forcing a new archive.");
                    DataManager.CreateArchive(1001);
                }

                GameSettingsManager.Init();

                InventoryManager.InitializeForRuntime();
                EnsureInventoryWindowDataRegistered();
                EnsureDialogWindowDataRegistered();
                EnsureSettingsWindowDataRegistered();
                UIModalStack.Clear();
                RegisterBagInput();

                // Keep scene-switch behavior aligned with TestScene and avoid redundant full re-init.
                await PlayerManager.Instance.EnsureInitializedAsync();

                RegisterBagInput();
                RegisterEscInput();
                RayDebug.Info($"Game start, current character id: {DataManager.GameData.SelectedCharacterId}");

                var ecsRunner = BattleEcsRunner.Ensure();
                ecsRunner.RegisterCharacter(PlayerManager.Instance.player);

                if (LootDropManager.Instance != null)
                {
                    LootDropManager.Instance.RestoreScenePersistentDrops();
                }

                // 额外等待一小段时间，确保角色模型和动画状态完全初始化并应用，避免看到T-pose
                await Cysharp.Threading.Tasks.UniTask.Delay(200);
            }
            finally
            {
                EventSystem.EventTrigger(GameManager.GameSceneReadyEvent);
            }
        }

        private void OnDestroy()
        {
            UnregisterBagInput();
            UnregisterEscInput();
            SavePlayerArchiveSnapshot("OnDestroy");
        }

        private void OnApplicationQuit()
        {
            SavePlayerArchiveSnapshot("OnApplicationQuit");
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
        /// Register global ESC input and close the top modal UI.
        /// </summary>
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

        private void OnEscPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (UIModalStack.CloseTop())
                return;

            EnsureSettingsWindowDataRegistered();
            var settingsWindow = UISystem.GetWindow<UI.UI_GameSettingsWindow>();
            if (settingsWindow != null && settingsWindow.UIEnable)
            {
                UISystem.Close<UI.UI_GameSettingsWindow>();
            }
            else
            {
                UISystem.Show<UI.UI_GameSettingsWindow>();
            }
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

            JKLog.Warning($"[GameSceneManager] Runtime UIWindowData registration: {InventoryWindowTypeKey}");
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

            JKLog.Warning($"[GameSceneManager] Runtime UIWindowData registration: {DialogWindowTypeKey}");
        }

        private void EnsureSettingsWindowDataRegistered()
        {
            if (UISystem.TryGetUIWindowData(SettingsWindowTypeKey, out _))
            {
                return;
            }

            UISystem.AddUIWindowData(
                SettingsWindowTypeKey,
                new UIWindowData(SettingsWindowCache, SettingsWindowAssetKey, SettingsWindowLayer));

            JKLog.Warning($"[GameSceneManager] Runtime UIWindowData registration: {SettingsWindowTypeKey}");
        }

        private static void SavePlayerArchiveSnapshot(string reason)
        {
            if (PlayerManager.TryGetLatestPlayerWorldPosition(out var pos))
            {
                DataManager.SavePlayerPosition(pos, GameSceneName);
                JKLog.Log($"[GameSceneManager] 保存玩家存档快照: reason={reason}, scene={GameSceneName}, pos={pos}");
            }
            else
            {
                JKLog.Warning($"[GameSceneManager] 保存玩家存档快照失败: reason={reason}, 无可用玩家位置。");
            }

            DataManager.SaveGameData();
        }
    }
}
