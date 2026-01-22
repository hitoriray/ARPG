using JKFrame;
using UI;

namespace Scene
{
    public class MenuSceneManager : SingletonMono<MenuSceneManager>
    {
        void Start()
        {
            UISystem.Show<UI_MenuSceneMenuWindow>();
        }
    }
}
