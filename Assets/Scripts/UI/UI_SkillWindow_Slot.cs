using Config;
using Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class UI_SkillWindow_Slot :MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject selected;
        [SerializeField] private Image icon;
        [SerializeField] private Text lv;
        
        public void Init()
        {
            OnPointerExit(null);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            selected.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            selected.gameObject.SetActive(false);
        }

        public void Show(SkillLearnedData skillLearnedData, SkillConfig skillConfig, bool canRelease)
        {
            icon.gameObject.SetActive(skillLearnedData != null);
            lv.gameObject.SetActive(skillLearnedData != null);
            if (skillLearnedData != null)
            {
                icon.sprite = skillConfig.skillIcon;
                lv.text = $"LV.{skillLearnedData.lv}";
            }
        }
    }
}