using Data;
using JKFrame;
using UI;

namespace CreateCharacterScene
{
    public class CreateCharacterSceneManager : LogicManagerBase<CreateCharacterSceneManager>
    {
        private void Start()
        {
            // 初始化自定义角色数据
            DataManager.InitCustomCharacterData();
            // 初始化角色创建者
            CharacterCreator.Instance.Init();
            
            // 显示创建角色的主窗口
            UIManager.Instance.Show<UI_CreateCharacterWindow>();
        }
    }
}