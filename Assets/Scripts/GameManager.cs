using System.Collections;
using System.Collections.Generic;
using Data;
using JKFrame;
using UnityEngine;

public class GameManager : SingletonMono<GameManager>
{
    /// <summary>
    /// 创建新存档并且进入游戏
    /// </summary>
    public void CreateNewArchiveAndEnterGame()
    {
        // 初始化存档
        DataManager.CreateArchive();
        // 进入自定义角色场景
        SceneManager.LoadScene("CreateCharacter");
    }

    /// <summary>
    /// 使用当前存档并且进入游戏
    /// </summary>
    public void UseCurrentArchiveAndEnterGame()
    {
        // 加载当前存档
        DataManager.LoadCurrentArchive();
        // 进入游戏场景
        SceneManager.LoadScene("Game");
    }
}
