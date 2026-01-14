using Config;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 角色创建窗口 - 外观菜单标签按钮
    /// </summary>
    public class UI_FacadeMenuButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image selectedIcon;
        [SerializeField] private AudioClip clickedAudio;
        
        private UI_CreateCharacterWindow createCharacterWindow;
        public CharacterPartType CharacterPartType { get; private set; }
    
        #region 颜色相关
        private static readonly Color[] Colors;
        static UI_FacadeMenuButton()
        {
            Colors = new[] { Color.white, new Color(0.964f, 0.882f, 0.611f) };
        }
        #endregion
        
        public void Init(UI_CreateCharacterWindow window, CharacterPartType type)
        {
            createCharacterWindow = window;
            CharacterPartType = type;
            button?.onClick?.AddListener(OnButtonClicked);
            Unselect();
        }

        private void OnButtonClicked()
        {
            AudioManager.Instance.PlayOnShot(clickedAudio, Vector3.zero, 1, false);
            createCharacterWindow.SelectFacadeMenuButton(this);
        }
        
        public void Select()
        {
            icon.color = Colors[1];
            selectedIcon.enabled = true;
        }

        public void Unselect()
        {
            icon.color = Colors[0];
            selectedIcon.enabled = false;
        }
    }
}
