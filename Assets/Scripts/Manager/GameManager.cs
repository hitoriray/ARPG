using JKFrame;
using PixelCrushers.DialogueSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 全局游戏管理器（继承 MonoSingleton，带 DontDestroyOnLoad，跨场景持久存在）。
/// 
/// 场景切换流程：
///   1. 调用 LoadSceneWithLoading(sceneName, true) 弹出 Loading 界面并异步加载场景
///   2. 目标场景的 XSceneManager.Start() 完成初始化后触发 GameSceneReadyEvent
///   3. UI_LoadingWindow 收到事件后进度条推到 100% 并自动关闭
/// 
/// 对话触发切换：
///   在 Dialogue System 对话节点的 Script 字段填写：
///     TravelToScene("Test")
/// </summary>
namespace Manager
{
    public class GameManager : MonoSingleton<GameManager>
    {
        private const string LoadingWindowTypeKey = "UI.UI_LoadingWindow";
        private const string LoadingWindowAssetKey = "UI_LoadingWindow";
        private const int LoadingWindowLayer = 2;
        private const bool LoadingWindowCache = true;
        private const string CharacterSelectionSceneName = "CharacterSelection";
        private const string GameSceneName = "Game";

        public const string GameSceneReadyEvent = "GameSceneReady";

        public static Vector2 canvasSize { get; private set; } = new Vector2(1920, 1080);
        public bool WaitForSceneReadyEvent { get; private set; }

        protected override void Awake()
        {
            base.Awake(); // MonoSingleton.Awake() 处理 DontDestroyOnLoad 和重复实例销毁

            // 向 Dialogue System Lua 环境注册场景切换函数
            // 对话节点 Script 字段直接写：TravelToScene("Game")
            Lua.RegisterFunction(
                "TravelToScene",
                this,
                SymbolExtensions.GetMethodInfo(() => TravelToScene(string.Empty)));
        }

        // ── 对话系统 Lua 入口 ───────────────────────────────────────────

        /// <summary>
        /// 由 Dialogue System Lua 调用：TravelToScene("SceneName")
        /// 等同于 LoadSceneWithLoading，带 Loading 界面异步切换场景。
        /// </summary>
        public void TravelToScene(string sceneName)
        {
            // 关闭当前对话窗口（避免切换后 UI 残留）
            UISystem.Close("UI.UI_ConversationWindow");

            // 位置保存由 GameSceneManager.OnDestroy 负责，场景卸载时自动触发
            // 此处无需直接访问 PlayerManager

            LoadSceneWithLoading(sceneName, true);
        }

        // ── 公共接口 ────────────────────────────────────────────────────

        /// <summary>创建新存档后进入角色选择界面</summary>
        public void CreateNewArchiveAndEnterGame()
        {
            DataManager.CreateArchive();
            EnterCharacterSelectionWithLoading();
        }

        /// <summary>读取当前存档后进入游戏场景</summary>
        public void UseCurrentArchiveAndEnterGame()
        {
            if (!DataManager.LoadCurrentArchive())
            {
                JKLog.Warning("[GameManager] Continue game load failed. Creating a new archive.");
                DataManager.CreateArchive();
            }

            EnterGameSceneWithLoading();
        }

        /// <summary>进入主游戏场景（带 Loading 界面）</summary>
        public void EnterGameSceneWithLoading()
        {
            LoadSceneWithLoading(GameSceneName, true);
        }

        /// <summary>进入角色选择场景（带 Loading 界面）</summary>
        public void EnterCharacterSelectionWithLoading()
        {
            LoadSceneWithLoading(CharacterSelectionSceneName, true);
        }

        /// <summary>
        /// 通用场景切换入口：弹出 Loading 窗口并异步加载目标场景。
        /// </summary>
        /// <param name="sceneName">目标场景名（Build Settings 中的名称）</param>
        /// <param name="waitForSceneReadyEvent">
        ///   true  = 等待目标场景的 GameSceneReadyEvent 后 Loading 才关闭（有复杂初始化时用）
        ///   false = 场景文件加载完成后 Loading 立刻关闭
        /// </param>
        public void LoadSceneWithLoading(string sceneName, bool waitForSceneReadyEvent)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                JKLog.Error("[GameManager] sceneName is null or empty.");
                return;
            }

            LoadSceneWithLoadingAsync(sceneName, waitForSceneReadyEvent).Forget();
        }

        private async UniTaskVoid LoadSceneWithLoadingAsync(string sceneName, bool waitForSceneReadyEvent)
        {
            WaitForSceneReadyEvent = waitForSceneReadyEvent;
            EnsureLoadingWindowDataRegistered();

            UISystem.ShowAsync(LoadingWindowTypeKey);
            await UniTask.WaitUntil(() =>
            {
                var loadingWindow = UISystem.GetWindow(LoadingWindowTypeKey);
                return loadingWindow != null && loadingWindow.UIEnable;
            });
            
            Canvas.ForceUpdateCanvases();

            // Let LoadingWindow render at least one frame before scene load starts.
            await UniTask.NextFrame();

            SceneSystem.LoadSceneAsync(sceneName);
        }

        // ── 私有 ────────────────────────────────────────────────────────

        private static void EnsureLoadingWindowDataRegistered()
        {
            if (UISystem.TryGetUIWindowData(LoadingWindowTypeKey, out _)) return;

            UISystem.AddUIWindowData(
                LoadingWindowTypeKey,
                new UIWindowData(LoadingWindowCache, LoadingWindowAssetKey, LoadingWindowLayer));

            JKLog.Warning("[GameManager] Runtime UIWindowData registration: UI.UI_LoadingWindow");
        }
    }
}
