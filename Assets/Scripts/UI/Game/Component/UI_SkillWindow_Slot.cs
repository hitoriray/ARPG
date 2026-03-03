using Config;
using Data;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UI_SkillWindow_Slot : UI_SkillSlotBase
    {
        [SerializeField] private Text lv;
        private int skillIndex = -1;
        
        public void Show(SkillLearnedData skillLearnedData, int skillIndex, SkillConfig skillConfig, bool canRelease)
        {
            base.Show(skillConfig);
            this.skillIndex = skillIndex;
            lv.gameObject.SetActive(skillLearnedData != null);
            if (skillLearnedData != null)
            {
                lv.text = $"LV.{skillLearnedData.lv}";
            }
        }

        protected override void OnDragToNewSlot(UI_SkillSlotBase newSlot)
        {
            // 忽视同类
            if (newSlot is UI_SkillWindow_Slot)
                return;
            
            // 快捷栏
            if (newSlot is UI_ShortcutSkill_Slot)
            {
                // 避免在快捷栏中重复技能
                UI_GameSceneMainWindow mainWindow = UISystem.GetWindow<UI_GameSceneMainWindow>();
                if (mainWindow.TryGetShortcutSkillSlotIndex(skillIndex, out var slotIndex))
                {
                    mainWindow.ChangeShortcutSkill(slotIndex, -1);
                }
                ((UI_ShortcutSkill_Slot)newSlot).ChangeSkill(skillIndex);
            }
        }
    }
}