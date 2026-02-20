using Manager;
using JKFrame;
using UnityEngine;

public class GameManager : SingletonMono<GameManager>
{
    public static Vector2 canvasSize { get; private set; } = new Vector2(1920, 1080);

    /// <summary>
    /// 创建新存档并且进入游戏
    /// </summary>
    public void CreateNewArchiveAndEnterGame()
    {
        // 初始化存档
        DataManager.CreateArchive();
        // 进入自定义角色场景
        SceneSystem.LoadScene("CreateCharacter");
    }

    /// <summary>
    /// 使用当前存档并且进入游戏
    /// </summary>
    public void UseCurrentArchiveAndEnterGame()
    {
        // 加载当前存档
        if (!DataManager.LoadCurrentArchive())
        {
            JKLog.Warning("[GameManager] 继续游戏读取存档失败，创建新存档。");
            DataManager.CreateArchive();
        }
        // 进入游戏场景
        SceneSystem.LoadScene("Game");
    }
}
