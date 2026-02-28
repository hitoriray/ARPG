using JKFrame;
using UI;

namespace Manager
{
    public class MenuSceneManager : SingletonMono<MenuSceneManager>
    {
        void Start()
        {
            UISystem.Show<UI_MenuSceneMenuWindow>();
        }
    }
}
