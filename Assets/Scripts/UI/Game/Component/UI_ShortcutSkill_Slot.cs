using Config;
using JKFrame;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    public class UI_ShortcutSkill_Slot : UI_SkillSlotBase, IPointerDownHandler
    {
        [SerializeField] private Image cdMask;
        [SerializeField] private TMP_Text shortCut;
        private int slotIndex;
        public int skillIndex { get; private set; }

        private Image comboGlowImage;
        private Coroutine glowCoroutine;

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
            
            // 技能详情日志打印
            string skillName = skillConfig != null ? skillConfig.skillName : "空";
            RayDebug.Info($"SlotIndex:{slotIndex} 加载技能 Index:{skillIndex} Name:{skillName}");

            if (shortCut != null)
            {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
                if (Application.isMobilePlatform)
                {
                    shortCut.gameObject.SetActive(false);
                }
                else
                {
                    shortCut.gameObject.SetActive(true);
                    shortCut.text = GetShortcutKeyName(slotIndex);
                }
#else
                shortCut.gameObject.SetActive(true);
                shortCut.text = GetShortcutKeyName(slotIndex);
#endif
            }
        }

        private string GetShortcutKeyName(int slotIndex)
        {
            var inputMap = InputService.Instance?.inputMap;
            if (inputMap == null) return (slotIndex + 1).ToString();

            // 按照常规，0号普通攻击是BasicAttack，1-3是Skill1~Skill3
            InputAction action = null;
            if (slotIndex == 0)
            {
                action = inputMap.Player.BasicAttack;
            }
            else if (slotIndex == 1)
            {
                action = inputMap.Player.Skill1;
            }
            else if (slotIndex == 2)
            {
                action = inputMap.Player.Skill2;
            }
            else if (slotIndex == 3)
            {
                action = inputMap.Player.Skill3;
            }

            if (action != null)
            {
                // 获取当前正在生效的绑定文本 (排除掉手柄摇杆等非PC按键，这里直接取PC键盘/鼠标的名字更直接)
                // 或者直接利用GetBindingDisplayString()
                string displayString = action.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                
                // 比如如果返回的是 "LMB" 可以手动换成更好的文本，也可以直接返回
                if (!string.IsNullOrEmpty(displayString))
                {
                    return displayString;
                }
            }
            
            return (slotIndex + 1).ToString();
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

        /// <summary>
        /// 无美术资源的代码发光特效实现（呼吸缩放+Alpha闪烁）
        /// </summary>
        public void SetComboGlow(bool active)
        {
            if (active)
            {
                if (comboGlowImage == null)
                {
                    GameObject glowObj = new GameObject("ComboGlow");
                    glowObj.transform.SetParent(icon.transform.parent, false);
                    glowObj.transform.SetSiblingIndex(icon.transform.GetSiblingIndex()); // 放在icon的下面(背后)
                    
                    comboGlowImage = glowObj.AddComponent<Image>();
                    comboGlowImage.sprite = icon.sprite;
                    // 设置Layout参数和icon一致
                    RectTransform rt = comboGlowImage.rectTransform;
                    rt.anchorMin = icon.rectTransform.anchorMin;
                    rt.anchorMax = icon.rectTransform.anchorMax;
                    rt.sizeDelta = icon.rectTransform.sizeDelta;
                    rt.anchoredPosition = icon.rectTransform.anchoredPosition;
                }
                
                comboGlowImage.sprite = icon.sprite; // 保持同款图标
                if (!comboGlowImage.gameObject.activeSelf)
                    comboGlowImage.gameObject.SetActive(true);
                
                if (glowCoroutine == null)
                    glowCoroutine = StartCoroutine(GlowRoutine());
            }
            else
            {
                if (comboGlowImage != null && comboGlowImage.gameObject.activeSelf)
                {
                    comboGlowImage.gameObject.SetActive(false);
                }
                if (glowCoroutine != null)
                {
                    StopCoroutine(glowCoroutine);
                    glowCoroutine = null;
                }
            }
        }
        
        private System.Collections.IEnumerator GlowRoutine()
        {
            while(true)
            {
                // 金色呼吸光效
                float alpha = 0.3f + Mathf.PingPong(Time.time * 2.5f, 0.6f); // 0.3 -> 0.9 透明度呼吸
                comboGlowImage.color = new Color(1f, 0.84f, 0f, alpha);
                
                // 大小呼吸缩放 (1.05倍到1.15倍)
                float scale = 1.05f + Mathf.PingPong(Time.time * 1.5f, 0.1f);
                comboGlowImage.rectTransform.localScale = Vector3.one * scale;
                
                yield return null;
            }
        }

        /// <summary>
        /// 更新技能能否被释放的状态
        /// </summary>
        public void UpdateSkillReleaseState(bool canRelease)
        {
            icon.color = canRelease ? Color.white : Color.black;
        }

        public void UpdateIcon(Sprite sprite)
        {
            this.icon.sprite = sprite;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 只有在手机端，或者编辑器下测试手机UI时才响应点击作为技能释放
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            if (skillIndex != -1 && skillConfig != null)
            {
                var playerCtrl = PlayerService.Instance.GetCharacterController();
                if (playerCtrl != null)
                {
                    playerCtrl.TryReleaseSkillBySkillIndex(skillIndex);
                }
            }
#endif
        }
    }
}
