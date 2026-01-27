using Config;
using Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UI_SkillLearnWindow_Item : MonoBehaviour
    {
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image bgImage;
        [SerializeField] private Text skillName;
        [SerializeField] private Text skillLevel;
        [SerializeField] private Text canRelease;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectColor = Color.yellow;
        

        public void Init(SkillConfig skillConfig, SkillLearnedData skillLearnedData)
        {
            if (skillLearnedData != null)
            {
                skillLevel.text = $"LV.{skillLearnedData.lv}";
            }
            else
            {
                skillLevel.text = $"LV.0";
            }

            canRelease.text = skillConfig.canRelease ? "主动" : "被动";
            skillName.text = skillConfig.skillName;
            skillIcon.sprite = skillConfig.skillIcon;
        }

        public void Select()
        {
            bgImage.color = selectColor;
        }

        public void Unselect()
        {
            bgImage.color = normalColor;
        }
    }
}