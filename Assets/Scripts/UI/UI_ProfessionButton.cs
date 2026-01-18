using Config;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 职业选择按钮
    /// </summary>
    public class UI_ProfessionButton : MonoBehaviour
    {
        #region ui控件
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image selectedIcon;
        [SerializeField] private Text nameText;
        [SerializeField] private AudioClip clickedAudio;
        #endregion
    
        private UI_CreateCharacterWindow createCharacterWindow;
        public ProfessionType Profession { get; private set; }

        #region 颜色相关
        private static readonly Color[] Colors;
        static UI_ProfessionButton()
        {
            Colors = new[] { Color.white, new Color(0.964f, 0.882f, 0.611f) };
        }
        #endregion

        public void Init(UI_CreateCharacterWindow window, ProfessionType professionType)
        {
            createCharacterWindow = window;
            Profession = professionType;
            button.onClick.AddListener(OnButtonClicked);
            Unselect();
        }

        private void OnButtonClicked()
        {
            AudioManager.Instance.PlayOnShot(clickedAudio, Vector3.zero, 1, false);
            createCharacterWindow.SelectProfessionButton(this);
        }

        public void Select()
        {
            icon.color = Colors[1];
            nameText.color = Colors[1];
            selectedIcon.enabled = true;
        }

        public void Unselect()
        {
            icon.color = Colors[0];
            nameText.color = Colors[0];
            selectedIcon.enabled = false;
        }
    }
}