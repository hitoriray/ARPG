using JKFrame;
using UI;

namespace MenuScene
{
    public class MenuSceneManager : LogicManagerBase<MenuSceneManager>
    {
        void Start()
        {
            UIManager.Instance.Show<UI_MenuWindow>();
        }
    }
}
