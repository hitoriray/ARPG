using JKFrame;
using UI;

namespace Scene
{
    public class MenuSceneManager : LogicManagerBase<MenuSceneManager>
    {
        void Start()
        {
            UIManager.Instance.Show<UI_MenuWindow>();
        }
    }
}
