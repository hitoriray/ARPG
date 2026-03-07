using System.Collections.Generic;
using Data;
using JKFrame;
using Manager;
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
            base.OnShow();
            PlayerService.Instance?.SetCharacterControl(false);
            InputService.Instance?.inputMap?.UI.Disable();
            PlayerService.Instance?.PushUICursor();
            UIModalStack.Push(CloseThisWindow);
        }

        public override void OnClose()
        {
            base.OnClose();
            if (UI_SkillSlotBase.currentEnterSlot != null)
            {
                UI_SkillSlotBase.currentEnterSlot.OnPointerExit(null);
            }
            PlayerService.Instance?.SetCharacterControl(true);
            InputService.Instance?.inputMap?.UI.Enable();
            PlayerService.Instance?.PopUICursor();
            UIModalStack.Remove(CloseThisWindow);
        }
        
        private void OnCloseBtnClicked()
        {
            UISystem.Close<UI_SkillWindow>();
        }

        private void CloseThisWindow()
        {
            UISystem.Close<UI_SkillWindow>();
        }

        public void Show(SkillLearnedDatas skillLearnedDatas)
        {
            int releaseSkillIndex = 0;
            int passiveSkillIndex = 0;
            var skillConfigs = PlayerService.Instance.GetAllSkillConfig();
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
