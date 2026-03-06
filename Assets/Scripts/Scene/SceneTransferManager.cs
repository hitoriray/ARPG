using JKFrame;
using PixelCrushers.DialogueSystem;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// 场景切换管理器（单例）。
    /// 
    /// 在 GameSceneManager.Start 调用 Init()，即可在整个生命周期内：
    ///   - 向 Dialogue System Lua 注册 TravelToScene(sceneName) 函数
    ///   - 弹出 UI_LoadingWindow 并异步加载目标场景
    /// 
    /// 对话节点写法：
    ///   Sequence 字段（或 Script 字段）填写：
    ///     SendMessage(TravelToScene, GameScene);
    ///   或者在 On Dialogue Event → OnConversationEnd 里调用 Lua：
    ///     TravelToScene("GameScene");
    ///   推荐用 Dialogue System 的 Lua Script 字段直接写：
    ///     TravelToScene("TestScene");
    /// </summary>
    public class SceneTransferManager : SingletonMono<SceneTransferManager>
    {
        private bool _isTransferring;

        /// <summary>初始化并注册 Lua 函数，在 GameSceneManager.Start 调用一次即可。</summary>
        public void Init()
        {
            // 向 Dialogue System Lua 环境注册 C# 方法
            Lua.RegisterFunction(
                "TravelToScene",
                this,
                SymbolExtensions.GetMethodInfo(() => TravelToScene(string.Empty)));

            JKLog.Log("[SceneTransferManager] Lua 函数 TravelToScene 已注册。");
        }

        private void OnDestroy()
        {
            // 取消注册，防止场景切换后旧引用残留
            Lua.UnregisterFunction("TravelToScene");
        }

        /// <summary>
        /// Dialogue System Lua 调用入口：TravelToScene("SceneName")
        /// </summary>
        public void TravelToScene(string sceneName)
        {
            if (_isTransferring)
            {
                JKLog.Warning("[SceneTransferManager] 场景切换中，忽略重复请求。");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                JKLog.Error("[SceneTransferManager] TravelToScene: sceneName 为空。");
                return;
            }

            JKLog.Log($"[SceneTransferManager] 开始切换到场景：{sceneName}");
            _isTransferring = true;

            // 1. 关闭可能还开着的对话窗口（避免切换后 UI 残留）
            UISystem.Close("UI.UI_ConversationWindow");

            // 2. 弹出 Loading 窗口
            UISystem.Show<UI_LoadingWindow>();

            // 3. 保存玩家位置（在场景切换前顺手存一次）
            if (PlayerManager.Instance?.player != null)
            {
                var pos = PlayerManager.Instance.player.transform.position;
                var currentScene = SceneManager.GetActiveScene().name;
                DataManager.SavePlayerPosition(pos, currentScene);
            }

            // 4. 用 JKFrame SceneSystem 异步加载目标场景
            //    SceneSystem.LoadSceneAsync 会广播 LoadingSceneProgress / LoadSceneSucceed
            //    UI_LoadingWindow 已订阅这两个事件，进度条自动跑，加载完后自动关闭
            SceneSystem.LoadSceneAsync(sceneName);
        }
    }
}
