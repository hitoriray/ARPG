using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_SkillLearnWindow), false, "UI_SkillLearnWindow", 2)]
    public class UI_SkillLearnWindow : UI_WindowBase
    {
        private class ItemInfo
        {
            public int itemIndex;
            public SkillConfig skillConfig;
            public SkillLearnedData skillLearnedData;
            public UI_SkillLearnWindow_Item item;
        }

        [SerializeField] private Transform itemRoot;
        [SerializeField] private GameObject itemPrefab;

        [SerializeField] private Button backBtn;
        [SerializeField] private Button learnBtn;
        [SerializeField] private Text skillTotalPoint;
        [SerializeField] private Text skillDescription;
        [SerializeField] private Text skillCd;
        [SerializeField] private Text skillPointRequire;
        [SerializeField] private Text attack;
        
        private List<UI_SkillLearnWindow_Item> itemList;
        private SkillLearnedDatas skillLearnedDatas;

        public override void Init()
        {
            backBtn.onClick.AddListener(OnBackBtnClicked);
            learnBtn.onClick.AddListener(OnLearnBtnClicked);
        }

        public void Init(SkillLearnedDatas skillLearnedDatas)
        {
            this.skillLearnedDatas = skillLearnedDatas;
            var skillConfigList = PlayerManager.Instance.GetAllSkillConfig();
            itemList = new(skillConfigList.Count);
            for (int i = 0; i < skillConfigList.Count; i++)
            {
                var item = CreateItem();
                skillLearnedDatas.SkillLearnedDataDict.Dictionary.TryGetValue(i, out SkillLearnedData skillLearnedData);
                item.Init(skillConfigList[i], skillLearnedData);
                ItemInfo info = new ItemInfo
                {
                    itemIndex = i, 
                    skillLearnedData = skillLearnedData, 
                    skillConfig = skillConfigList[i],
                    item = item,
                };
                item.OnClickDown(OnSelectItem, info);
                itemList.Add(item);
                if (i == 0) // 默认选中第一个技能
                {
                    OnSelectItem(null, info);
                }
            }

            // 更新技能点
            UpdateSkillTotalPoint(skillLearnedDatas.SkillTotalPoint);
        }


        private ItemInfo selectedItemInfo;
        private void OnSelectItem(PointerEventData data, ItemInfo newItemInfo)
        {
            if (selectedItemInfo == newItemInfo)
                return;
            newItemInfo.item.Select();
            if (selectedItemInfo != null)
            {
                selectedItemInfo?.item.Unselect();
            }
            selectedItemInfo = newItemInfo;
            // 更新描述
            UpdateRightPanel(selectedItemInfo);
        }

        private void UpdateRightPanel(ItemInfo itemInfo)
        {
            skillDescription.text = itemInfo.skillConfig.skillDescription;
            skillPointRequire.text = $"升级所需技能点数: {itemInfo.skillConfig.skillPointRequired}";
            int lv = itemInfo.skillLearnedData == null ? 1 : itemInfo.skillLearnedData.lv;
            skillCd.text = $"冷却时间: {itemInfo.skillConfig.GetCdTimeByLv(lv)}/{itemInfo.skillConfig.basicCdTime}秒";
            attack.text = $"攻击力: {itemInfo.skillConfig.GetAttackValueByLv(lv)}/{itemInfo.skillConfig.basicAttackValue}";
            // 如果满级，禁止学习
            if (itemInfo.skillLearnedData != null && itemInfo.skillLearnedData.lv == itemInfo.skillConfig.maxLv)
            {
                learnBtn.interactable = false;
            }
            // 技能点不够，禁止学习
            else if (skillLearnedDatas.SkillTotalPoint < itemInfo.skillConfig.skillPointRequired)
            {
                learnBtn.interactable = false;
            }
            else
            {
                learnBtn.interactable = true;
            }
        }

        private UI_SkillLearnWindow_Item CreateItem()
        {
            return GameObject.Instantiate(itemPrefab, itemRoot).GetComponent<UI_SkillLearnWindow_Item>();
        }

        public override void OnShow()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        public override void OnClose()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnBackBtnClicked()
        {
            UISystem.Close<UI_SkillLearnWindow>();
        }

        private void OnLearnBtnClicked()
        {
            if (!skillLearnedDatas.SkillLearnedDataDict.Dictionary.TryGetValue(selectedItemInfo.itemIndex,
                    out var skillLearnedData))
            {
                skillLearnedData = new();
                skillLearnedData.lv = 1;
                selectedItemInfo.skillLearnedData = skillLearnedData;
                skillLearnedDatas.SkillLearnedDataDict.Dictionary.Add(selectedItemInfo.itemIndex, skillLearnedData);
            }
            else
            {
                skillLearnedData.lv += 1;
            }

            skillLearnedDatas.SkillTotalPoint -= selectedItemInfo.skillConfig.skillPointRequired; // 扣除技能点
            UpdateSkillTotalPoint(skillLearnedDatas.SkillTotalPoint);
            selectedItemInfo.item.Init(selectedItemInfo.skillConfig, skillLearnedData);
            UpdateRightPanel(selectedItemInfo);
        }

        private void UpdateSkillTotalPoint(int num)
        {
            skillTotalPoint.text = num.ToString();
        }
    }
}