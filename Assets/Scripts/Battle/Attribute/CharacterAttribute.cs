using System;
using Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Attribute
{
    public class CharacterAttribute : SerializedMonoBehaviour
    {
        [ShowInInspector, ReadOnly] public float currentHp { get; private set; }
        [ShowInInspector, ReadOnly] public float currentMp { get; private set; }
        
        public FloatAttr maxHp = new();
        public FloatAttr maxMp = new();
        public FloatAttr attack = new();

        // TODO: 基于存档恢复当前血量等信息
        public void Init(CharacterConfig characterConfig, float currentHp = 100, float currentMp = 100)
        {
            maxHp.Init(characterConfig.hpBaseValue, null, null, null, OnMaxHpChanged);
            maxMp.Init(characterConfig.mpBaseValue, null, null, null, OnMaxMpChanged);
            attack.Init(characterConfig.attackBaseValue);
            this.currentHp = currentHp;
            this.currentMp = currentMp;
        }

        public void AddHp(float value)
        {
            SetHp(currentHp + value);
        }

        public void SetHp(float value)
        {
            currentHp = Mathf.Clamp(value, 0, maxHp.Total);
            // TODO: 同步给UI
        }

        public void AddMp(float value)
        {
            SetMp(currentMp + value);
        }

        public void SetMp(float value)
        {
            currentMp = Mathf.Clamp(value, 0, maxMp.Total);
            // TODO: 同步给UI
        }
        
        private void OnMaxHpChanged(float oldMaxHp, float newMaxHp)
        {
            // 当最大生命值发生变化时，当前生命值同步按比例变化
            currentHp = newMaxHp * currentHp / oldMaxHp;
            // TODO: 同步给UI
        }
        
        private void OnMaxMpChanged(float oldMaxMp, float newMaxMp)
        {
            currentMp = newMaxMp * currentMp / oldMaxMp;
            // TODO: 同步给UI
        }
    }

    public class FloatAttr
    {
        [SerializeField] private float baseValue;
        [SerializeField] private float fixedBonus;
        [SerializeField] private float multiplierBonus;
        
        private Action<float, float> onBaseValueChangedAction;
        private Action<float, float> onFixedValueChangedAction;
        private Action<float, float> onMultiplierValueChangedAction;
        private Action<float, float> onTotalValueChangeAction;
        
        public void Init(float baseValue, Action<float, float> onBaseValueChangedAction = null, Action<float, float> onFixedValueChangedAction = null, 
            Action<float, float> onMultiplierValueChangedAction = null, Action<float, float> onTotalValueChangeAction = null)
        {
            this.baseValue = baseValue;
            this.onBaseValueChangedAction = onBaseValueChangedAction;
            this.onFixedValueChangedAction = onFixedValueChangedAction;
            this.onMultiplierValueChangedAction = onMultiplierValueChangedAction;
            this.onTotalValueChangeAction = onTotalValueChangeAction;
        }
        
        #region 属性
        public float Total => baseValue + fixedBonus + (baseValue * multiplierBonus);

        public float BaseValue
        {
            get => baseValue;
            set
            {
                onBaseValueChangedAction?.Invoke(baseValue, value);
                if (onTotalValueChangeAction != null)
                {
                    float oldTotalValue = Total;
                    baseValue = value;
                    onTotalValueChangeAction(oldTotalValue, Total);
                }
                else
                {
                    baseValue = value;
                }
            }
        }

        public float FixedBonus
        {
            get => fixedBonus;
            set
            {
                onFixedValueChangedAction?.Invoke(fixedBonus, value);
                if (onTotalValueChangeAction != null)
                {
                    float oldTotalValue = Total;
                    fixedBonus = value;
                    onTotalValueChangeAction(oldTotalValue, Total);
                }
                else
                {
                    fixedBonus = value;
                }
            }
        }
        
        public float MultiplierBonus
        {
            get => multiplierBonus;
            set
            {
                onMultiplierValueChangedAction?.Invoke(multiplierBonus, value);
                if (onTotalValueChangeAction != null)
                {
                    float oldTotalValue = Total;
                    multiplierBonus = value;
                    onTotalValueChangeAction(oldTotalValue, Total);
                }
                else
                {
                    multiplierBonus = value;
                }
            }
        }        
        #endregion
    }
}