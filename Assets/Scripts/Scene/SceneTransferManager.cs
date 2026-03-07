using JKFrame;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// Scene transfer bridge for Dialogue System and runtime scene switches.
    /// </summary>
    public class SceneTransferManager : SingletonMono<SceneTransferManager>
    {
        private bool _isTransferring;

        /// <summary>
        /// Register Lua function: TravelToScene("SceneName")
        /// </summary>
        public void Init()
        {
            Lua.RegisterFunction(
                "TravelToScene",
                this,
                SymbolExtensions.GetMethodInfo(() => TravelToScene(string.Empty)));

            JKLog.Log("[SceneTransferManager] Lua function TravelToScene registered.");
        }

        private void OnDestroy()
        {
            Lua.UnregisterFunction("TravelToScene");
        }

        /// <summary>
        /// Dialogue System Lua entry point: TravelToScene("SceneName")
        /// </summary>
        public void TravelToScene(string sceneName)
        {
            if (_isTransferring)
            {
                JKLog.Warning("[SceneTransferManager] Scene is already transferring. Ignore duplicate request.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                JKLog.Error("[SceneTransferManager] TravelToScene: sceneName is null or empty.");
                return;
            }

            JKLog.Log($"[SceneTransferManager] Start transfer to scene: {sceneName}");
            _isTransferring = true;

            // Close dialog window first to avoid UI leftovers.
            UISystem.Close("UI.UI_ConversationWindow");

            // Save current player position before switching.
            if (PlayerManager.TryGetLatestPlayerWorldPosition(out var pos))
            {
                var currentScene = SceneManager.GetActiveScene().name;
                DataManager.SavePlayerPosition(pos, currentScene);
            }

            // Route to GameManager to ensure loading UI is rendered before scene load starts.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSceneWithLoading(sceneName, true);
            }
            else
            {
                SceneSystem.LoadSceneAsync(sceneName);
            }
        }
    }
}
