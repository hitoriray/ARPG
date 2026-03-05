using JKFrame;
using UI;

namespace Manager
{
    public class MenuSceneManager : SingletonMono<MenuSceneManager>
    {
        void Start()
        {
            GameSettingsManager.Init();
            UISystem.Show<UI_MenuSceneMenuWindow>();
        }
    }
}
