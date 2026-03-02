using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using Manager;
using Michsky.MUIP;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        
        #region 获取物品提示栏

        [SerializeField] private NotificationManager pickupNotifyPrefab;
        [SerializeField] private RectTransform pickupNotifyRoot;
        [SerializeField, Min(0.5f)] private float pickupNotifyDuration = 2.5f;
        [SerializeField, Min(1)] private int pickupNotifyMax = 6;

        private readonly List<NotificationManager> _activePickupNotifies = new();
        private readonly Dictionary<int, Sprite> _itemIconCache = new();

        public void ShowPickupNotification(ItemConfig config, int amount)
        {
            if (config == null || amount <= 0) return;

            EnsurePickupNotifyRoot();
            if (pickupNotifyPrefab == null || pickupNotifyRoot == null) return;

            var go = Instantiate(pickupNotifyPrefab.gameObject, pickupNotifyRoot);
            var notif = go.GetComponent<NotificationManager>();
            if (notif == null) return;

            ForceUseCustomContent(notif, true);
            notif.enableTimer = true;
            notif.timer = pickupNotifyDuration;
            notif.closeOnClick = false;
            notif.useStacking = false;
            notif.startBehaviour = NotificationManager.StartBehaviour.None;
            notif.closeBehaviour = NotificationManager.CloseBehaviour.Destroy;

            string pickUpInfo;
            if (amount > 1) pickUpInfo = $"{config.ItemName} x{amount}";
            else pickUpInfo = $"{config.ItemName}";
            notif.title = pickUpInfo;
            notif.description = string.Empty;
            ApplyNotificationIcon(notif, config);
            notif.UpdateUI();

            // 如果 descriptionObj 与 titleObj 是同一个引用，避免把标题隐藏掉
            if (notif.titleObj != null)
                notif.titleObj.text = pickUpInfo;

            notif.onClose.AddListener(() =>
            {
                _activePickupNotifies.Remove(notif);
            });

            _activePickupNotifies.Add(notif);
            TrimPickupNotifies();
            notif.Open();
        }

        private void TrimPickupNotifies()
        {
            if (pickupNotifyMax <= 0) return;
            while (_activePickupNotifies.Count > pickupNotifyMax)
            {
                var oldest = _activePickupNotifies[0];
                _activePickupNotifies.RemoveAt(0);
                if (oldest != null)
                    Destroy(oldest.gameObject);
            }
        }

        private void EnsurePickupNotifyRoot()
        {
            if (pickupNotifyRoot != null) return;

            var root = new GameObject("PickupNotifyRoot", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            pickupNotifyRoot = root.GetComponent<RectTransform>();
            pickupNotifyRoot.anchorMin = new Vector2(0f, 0.5f);
            pickupNotifyRoot.anchorMax = new Vector2(0f, 0.5f);
            pickupNotifyRoot.pivot = new Vector2(0f, 0.5f);
            pickupNotifyRoot.anchoredPosition = new Vector2(40f, 0f);
            pickupNotifyRoot.sizeDelta = new Vector2(420f, 300f);

            var layout = root.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void ApplyNotificationIcon(NotificationManager notif, ItemConfig config)
        {
            if (notif == null || config == null) return;

            if (_itemIconCache.TryGetValue(config.ItemId, out var cached) && cached != null)
            {
                notif.icon = cached;
                if (notif.iconObj != null)
                    notif.iconObj.sprite = cached;
                return;
            }

            if (config.Icon == null || !config.Icon.RuntimeKeyIsValid()) return;

            var handle = config.Icon.LoadAssetAsync<Sprite>();
            handle.Completed += op =>
            {
                if (op.Status != AsyncOperationStatus.Succeeded) return;
                var sprite = op.Result;
                if (sprite == null) return;
                _itemIconCache[config.ItemId] = sprite;
                if (notif != null)
                {
                    notif.icon = sprite;
                    if (notif.iconObj != null)
                    {
                        notif.iconObj.sprite = sprite;
                    }
                    else if (notif.descriptionObj != notif.titleObj)
                    {
                        // 仅在 title/desc 分离时刷新，避免覆盖标题
                        notif.UpdateUI();
                    }
                }
            };
        }

        private static readonly System.Reflection.FieldInfo UseCustomContentField =
            typeof(NotificationManager).GetField("useCustomContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static void ForceUseCustomContent(NotificationManager notif, bool value)
        {
            if (notif == null || UseCustomContentField == null) return;
            UseCustomContentField.SetValue(notif, value);
        }

        #endregion
    }
}
