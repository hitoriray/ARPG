using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using Player;
using UnityEngine;

namespace UI
{
    [UIWindowData(typeof(UI_GameSceneMainWindow), true, nameof(UI_GameSceneMainWindow), 1)]
    public class UI_GameSceneMainWindow : UI_WindowBase
    {
        [SerializeField] private UI_ShortcutSkill_Slot[] shortcutSkillSlots;

        public void Show(ShortcutSkillSlotData shortcutSkillSlotData)
        {
            ShowShortcutSkillSlots(shortcutSkillSlotData);
        }

        public bool TryGetShortcutSkillSlot(int skillIndex, out UI_ShortcutSkill_Slot slot)
        {
            if (TryGetShortcutSkillSlotIndex(skillIndex, out int slotIndex))
            {
                slot = shortcutSkillSlots[slotIndex];
                return true;
            }
            slot = null;
            return false;
        }

        public bool TryGetShortcutSkillSlotIndex(int skillIndex, out int slotIndex)
        {
            for (int i = 0; i < shortcutSkillSlots.Length; i++)
            {
                if (shortcutSkillSlots[i].skillIndex == skillIndex)
                {
                    slotIndex = i;
                    return true;
                }
            }
            slotIndex = -1;
            return false;
        }

        public void ShowShortcutSkillSlots(ShortcutSkillSlotData shortcutSkillSlotData)
        {
            List<SkillConfig> skillConfigs = PlayerManager.Instance.GetAllSkillConfig();
            for (int i = 0; i < shortcutSkillSlotData.skillIds.Length; i++)
            {
                SkillConfig skillConfig = null;
                int skillIndex = shortcutSkillSlotData.skillIds[i];
                if (skillIndex != -1)
                {
                    skillConfig = skillConfigs[skillIndex];
                }
                // TODO: 技能格子与玩家实际的技能之间的绑定
                shortcutSkillSlots[i].Init(i);
                shortcutSkillSlots[i].Show(skillIndex, skillConfig);
            }
        }

        public void ChangeShortcutSkill(int slotIndex, int newSkillIndex)
        {
            SkillConfig skillConfig = null;
            if (newSkillIndex != -1)
            {
                skillConfig = PlayerManager.Instance.GetAllSkillConfig()[newSkillIndex];
            }
            shortcutSkillSlots[slotIndex].Show(newSkillIndex, skillConfig);
        }
    }
}