using System.Collections.Generic;
using Data;
using JKFrame;
using Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_SkillWindow), true, nameof(UI_SkillWindow), 1)]
    public class UI_SkillWindow : UI_WindowBase
    {
        [SerializeField] private Button closeBtn;
        [SerializeField] private Transform releaseSlotRoot;
        [SerializeField] private Transform passiveSlotRoot;
        [SerializeField] private GameObject slotPrefab;
        private const int slotCount = 12;
        private List<UI_SkillWindow_Slot> releaseSlotList = new(slotCount);
        private List<UI_SkillWindow_Slot> passiveSlotList = new(slotCount);

        public override void Init()
        {
            closeBtn.onClick.AddListener(OnCloseBtnClicked);
            for (int i = 0; i < slotCount; i++)
            {
                var slot1 = GameObject.Instantiate(slotPrefab, releaseSlotRoot).GetComponent<UI_SkillWindow_Slot>();
                var slot2 = GameObject.Instantiate(slotPrefab, passiveSlotRoot).GetComponent<UI_SkillWindow_Slot>();
                slot1.Init();
                slot2.Init();
                releaseSlotList.Add(slot1);
                passiveSlotList.Add(slot2);
            }
        }
        
        public override void OnShow()
        {
            InputManager.Instance.CharacterControl = false;
        }

        public override void OnClose()
        {
            if (UI_SkillSlotBase.currentEnterSlot != null)
            {
                UI_SkillSlotBase.currentEnterSlot.OnPointerExit(null);
            }
            InputManager.Instance.CharacterControl = true;
        }
        
        private void OnCloseBtnClicked()
        {
            UISystem.Close<UI_SkillWindow>();
        }

        public void Show(SkillLearnedDatas skillLearnedDatas)
        {
            int releaseSkillIndex = 0;
            int passiveSkillIndex = 0;
            var skillConfigs = PlayerManager.Instance.GetAllSkillConfig();
            foreach (var item in skillLearnedDatas.SkillLearnedDataDict.Dictionary)
            {
                var skillConfig = skillConfigs[item.Key];
                if (skillConfig.canRelease)
                {
                    releaseSlotList[releaseSkillIndex++].Show(item.Value, item.Key, skillConfig, true);
                }
                else
                {
                    passiveSlotList[passiveSkillIndex++].Show(item.Value, item.Key, skillConfig, false);
                }
            }

            for (int i = releaseSkillIndex; i < slotCount; i++)
            {
                releaseSlotList[i].Show(null, -1, null, true);
            }
            for (int i = passiveSkillIndex; i < slotCount; i++)
            {
                passiveSlotList[i].Show(null, -1, null, true);
            }
        }
    }
}