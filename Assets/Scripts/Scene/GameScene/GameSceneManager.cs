using Battle.ECS;
using Data;
using JKFrame;
using RayPlayer;
using Sirenix.OdinInspector;

namespace Scene
{
    public class GameSceneManager : SingletonMono<GameSceneManager>
    {
        #region Test

        [LabelText("是否创建新存档")] public bool isCreateArchive;
        [LabelText("初始角色ID"), ShowIf("isCreateArchive", true)] public int initialCharacterId = 1004;
        [LabelText("是否启用ECS")] public bool isEcs;
        
        #endregion
        
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
                    DataManager.LoadCurrentArchive();
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

            #endregion

            // 初始化角色
            await PlayerManager.Instance.InitAsync();
            RayDebug.Info($"游戏开始！当前角色ID: {DataManager.GameData.SelectedCharacterId}");
            // 初始化ECS并注册玩家
            var ecsRunner = BattleEcsRunner.Ensure();
            ecsRunner.RegisterPlayer(PlayerManager.Instance.player);

        }

        private void OnDestroy()
        {
            // TODO：模拟游戏退出时的存档
            DataManager.SaveGameData();
        }
    }
}
