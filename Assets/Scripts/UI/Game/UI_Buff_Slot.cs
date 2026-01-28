using Config;
using JKFrame;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UI_Buff_Slot : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text stack;
        [SerializeField] private Image mask;

        public void Init(BuffConfig buffConfig)
        {
            icon.sprite = buffConfig.icon;
            stack.gameObject.SetActive(buffConfig.maxStack > 1);
        }

        public void UpdateStack(int newStack)
        {
            stack.text = newStack.ToString();
        }

        public void UpdateMask(float maxFillAmount)
        {
            mask.fillAmount = maxFillAmount;
        }

        public void Destroy()
        {
            icon.sprite = null;
            this.GameObjectPushPool();
        }
    }
}