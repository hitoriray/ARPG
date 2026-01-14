using System.Collections;
using System.Collections.Generic;
using Config;
using JKFrame;
using UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 职业选择按钮
/// </summary>
public class UI_ProfessionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image selectedIcon;
    [SerializeField] private Text nameText;
    [SerializeField] private AudioClip clickedAudio;

    private static Color[] _colors;

    private UI_CreateCharacterWindow createCharacterWindow;
    public ProfessionType Profession { get; private set; }

    static UI_ProfessionButton()
    {
        _colors = new[] { Color.white, new Color(0.964f, 0.882f, 0.611f) };
    }

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
        icon.color = _colors[1];
        nameText.color = _colors[1];
        selectedIcon.enabled = true;
    }

    public void Unselect()
    {
        icon.color = _colors[0];
        nameText.color = _colors[0];
        selectedIcon.enabled = false;
    }
}