using UnityEngine;
using JKFrame;

public class TestColorSelector : MonoBehaviour
{
    public GameObject Cube;
    void Start()
    {
        UI_ColorSelectorWindow colorSelectorWindow = UISystem.Show<UI_ColorSelectorWindow>();
        Cube.GetComponent<MeshRenderer>().material.color = colorSelectorWindow.GetColor();// 主动获取
        colorSelectorWindow.Init(OnColorSelected,Color.white);    // 事件获取

    }

    private void OnColorSelected(Color obj)
    {
        Cube.GetComponent<MeshRenderer>().material.color = obj;
    }

}
