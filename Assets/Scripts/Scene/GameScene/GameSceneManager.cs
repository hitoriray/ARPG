using System;
using Data;
using JKFrame;
using Player;

namespace Scene
{
    public class GameSceneManager : LogicManagerBase<GameSceneManager>
    {
        #region Test

        public bool isTest;
        public bool isCreateArchive;
        
        #endregion
        
        private void Start()
        {
            #region Test...

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
            
            // 初始化角色
            PlayerController.Instance.Init();
        }
    }
}
