using JKFrame;
using UI;

namespace Manager
{
    public class CharacterSelectionManager : SingletonMono<CharacterSelectionManager>
    {
        private void Start()
        {
            UISystem.Show<UI_CharacterSelectionWindow>();
        }
    }
}
