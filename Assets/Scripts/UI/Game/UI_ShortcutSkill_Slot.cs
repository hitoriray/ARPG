using Config;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UI_ShortcutSkill_Slot : UI_SkillSlotBase
    {
        [SerializeField] private Image cdMask;
        private int slotIndex;
        public int skillIndex { get; private set; }

        public void Init(int slotIndex)
        {
            this.slotIndex = slotIndex;
            UpdateCdTime(0);
            Init();
        }

        public void Show(int skillIndex, SkillConfig skillConfig)
        {
            this.skillIndex = skillIndex;
            this.skillConfig = skillConfig;
            Show(skillConfig);
            InputManager.Instance.BindSkillKeyCode(slotIndex, skillIndex);
        }

        public void ChangeSkill(int newSkillIndex)
        {
            UISystem.GetWindow<UI_GameSceneMainWindow>().ChangeShortcutSkill(slotIndex, newSkillIndex);
        }

        protected override void OnDragToNewSlot(UI_SkillSlotBase newSlot)
        {
            if (newSlot is not UI_ShortcutSkill_Slot)
            {
                ChangeSkill(-1);
                return;
            }
            
            UI_ShortcutSkill_Slot otherSlot = (UI_ShortcutSkill_Slot)newSlot;
            int tmpIndex = this.skillIndex;
            // 自己变成对方的技能
            this.ChangeSkill(otherSlot.skillIndex);
            // 对方变成自己
            otherSlot.ChangeSkill(tmpIndex);
        }

        public void UpdateCdTime(float fillAmount)
        {
            cdMask.fillAmount = fillAmount;
        }

        public void UpdateCdTimeAndMaskColor(float fillAmount, Color color)
        {
            UpdateCdTime(fillAmount);
            cdMask.color = color;
        }

        public void UpdateIcon(Sprite sprite)
        {
            this.icon.sprite = sprite;
        }
    }
}
