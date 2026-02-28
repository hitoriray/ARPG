using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using Manager;
using UnityEngine;

namespace UI
{
    [UIWindowData(typeof(UI_GameSceneMainWindow), true, nameof(UI_GameSceneMainWindow), 1)]
    public class UI_GameSceneMainWindow : UI_WindowBase
    {
        #region 技能快捷栏
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
            List<SkillConfig> skillConfigs = PlayerService.Instance.GetAllSkillConfig();
            for (int i = 0; i < shortcutSkillSlotData.skillIds.Length; i++)
            {
                SkillConfig skillConfig = null;
                int skillIndex = shortcutSkillSlotData.skillIds[i];
                if (skillIndex != -1)
                {
                    skillConfig = skillConfigs[skillIndex];
                }
                shortcutSkillSlots[i].Init(i);
                shortcutSkillSlots[i].Show(skillIndex, skillConfig);
            }
        }

        public void ChangeShortcutSkill(int slotIndex, int newSkillIndex)
        {
            SkillConfig skillConfig = null;
            if (newSkillIndex != -1)
            {
                skillConfig = PlayerService.Instance.GetAllSkillConfig()[newSkillIndex];
            }
            shortcutSkillSlots[slotIndex].Show(newSkillIndex, skillConfig);

            // 使用新接口：更新当前角色的快捷栏数据
            var currentShortcutData = DataManager.GetCurrentCharacterShortcutSkills();
            currentShortcutData.skillIds[slotIndex] = newSkillIndex;
            DataManager.SaveGameData();
        }
        #endregion
        
        #region buff栏
        [SerializeField] private GameObject buffSlotPrefab;
        [SerializeField] private Transform buffSlotParent;
        private List<UI_Buff_Slot> buffSlotList = new();

        public UI_Buff_Slot AddBuff(BuffConfig buffConfig)
        {
            var buffSlot = ProjectUtility.GetOrInstantiateGameObject(buffSlotPrefab, buffSlotParent).GetComponent<UI_Buff_Slot>();
            buffSlotList.Add(buffSlot);
            buffSlot.Init(buffConfig);
            return buffSlot;
        }

        public void RemoveBuff(UI_Buff_Slot buffSlot)
        {
            buffSlot.Destroy();
            buffSlotList.Remove(buffSlot);
        }
        
        #endregion
    }
}