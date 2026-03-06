using Config;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;
using Manager;

namespace UI
{
    [UIWindowData(typeof(UI_SkillInfoPopupWindow), true, nameof(UI_SkillInfoPopupWindow), 1)]
    public class UI_SkillInfoPopupWindow : UI_WindowBase
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text skillName;
        [SerializeField] private Text description;

        private RectTransform rectTransform => (RectTransform)transform;
        
        public void Show(Vector3 slotWorldPos, float topOffset, SkillConfig skillConfig)
        {
            // 位置计算
            transform.position = slotWorldPos;
            Vector2 windowSize = rectTransform.sizeDelta;
            Vector3 uiPos = rectTransform.anchoredPosition;
            uiPos.y += topOffset;
            Vector2 canvasSize = GameManager.canvasSize;
            Vector2 widthRange = new Vector2(-canvasSize.x / 2 + windowSize.x / 2, canvasSize.x / 2f - windowSize.x / 2);
            Vector2 heightRange = new Vector2(-canvasSize.y / 2, canvasSize.y / 2f - windowSize.y / 2);
            uiPos.x = Mathf.Clamp(uiPos.x, widthRange.x, widthRange.y);
            uiPos.y = Mathf.Clamp(uiPos.y, heightRange.x, heightRange.y);
            rectTransform.anchoredPosition = uiPos;
            
            // 显示技能信息
            icon.sprite = skillConfig.skillIcon;
            skillName.text = skillConfig.skillName;
            description.text = skillConfig.skillDescription;
        }
    }
}