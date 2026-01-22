using Data;
using JKFrame;
using Player;
using UnityEngine;

namespace Scene
{
    public class GameSceneManager : SingletonMono<GameSceneManager>
    {
        #region Test

        public bool isTest;
        public bool isCreateArchive;
        
        #endregion
        
        private void Start()
        {
            #region 测试逻辑

            if (isTest)
            {
                if (isCreateArchive)
                {
                    DataManager.CreateArchive();
                }
                else
                {
                    DataManager.LoadCurrentArchive();
                }
            }
            
            #endregion
            
            Cursor.lockState = CursorLockMode.Locked;
            // 初始化角色
            PlayerController.Instance.Init();
        }
    }
}
