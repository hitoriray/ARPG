using Data;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_MenuSceneMenuWindow), false, "UI_MenuSceneMenuWindow", 2)]
    public class UI_MenuSceneMenuWindow : UI_WindowBase
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;

        public override void Init()
        {
            base.Init();
            
            startButton.onClick.AddListener(OnStartButtonClicked);
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            quitButton.onClick.AddListener(OnQuitButtonClicked);
            
            // 如果没有存档，隐藏继续按钮
            if (!DataManager.HasArchive)
            {
                continueButton.gameObject.SetActive(false);
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            // 释放自身资源
            ResSystem.UnloadInstance(gameObject);
        }

        #region 事件回调
        private void OnStartButtonClicked()
        {
            // 打开角色选择窗口
            UISystem.Show<UI_CharacterSelectionWindow>();
            // 关闭主菜单窗口
            UISystem.Close<UI_MenuSceneMenuWindow>();
        }

        private void OnContinueButtonClicked()
        {
            UISystem.Close<UI_MenuSceneMenuWindow>();
            GameManager.Instance.UseCurrentArchiveAndEnterGame();
        }
        
        private void OnQuitButtonClicked()
        {
            Application.Quit();
        }
        
        #endregion
    }
}
