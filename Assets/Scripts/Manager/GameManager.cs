using JKFrame;
using Manager;
using UnityEngine;

public class GameManager : SingletonMono<GameManager>
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

    /// <summary>
    /// Create a new archive, then enter character selection.
    /// </summary>
    public void CreateNewArchiveAndEnterGame()
    {
        DataManager.CreateArchive();
        EnterCharacterSelectionWithLoading();
    }

    /// <summary>
    /// Continue with current archive, then enter Game scene with loading UI.
    /// </summary>
    public void UseCurrentArchiveAndEnterGame()
    {
        if (!DataManager.LoadCurrentArchive())
        {
            JKLog.Warning("[GameManager] Continue game load failed. Creating a new archive.");
            DataManager.CreateArchive();
        }

        EnterGameSceneWithLoading();
    }

    /// <summary>
    /// Enter Game scene with loading UI and async scene loading.
    /// </summary>
    public void EnterGameSceneWithLoading()
    {
        LoadSceneWithLoading(GameSceneName, true);
    }

    /// <summary>
    /// Enter character selection scene with loading UI.
    /// </summary>
    public void EnterCharacterSelectionWithLoading()
    {
        LoadSceneWithLoading(CharacterSelectionSceneName, false);
    }

    /// <summary>
    /// Generic scene entry with loading UI.
    /// </summary>
    public void LoadSceneWithLoading(string sceneName, bool waitForSceneReadyEvent)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            JKLog.Error("[GameManager] sceneName is null or empty.");
            return;
        }

        WaitForSceneReadyEvent = waitForSceneReadyEvent;
        EnsureLoadingWindowDataRegistered();

        UISystem.Show(LoadingWindowTypeKey);
        SceneSystem.LoadSceneAsync(sceneName);
    }

    private static void EnsureLoadingWindowDataRegistered()
    {
        if (UISystem.TryGetUIWindowData(LoadingWindowTypeKey, out _))
        {
            return;
        }

        UISystem.AddUIWindowData(
            LoadingWindowTypeKey,
            new UIWindowData(LoadingWindowCache, LoadingWindowAssetKey, LoadingWindowLayer));

        JKLog.Warning($"[GameManager] Runtime UIWindowData registration: {LoadingWindowTypeKey}");
    }
}
