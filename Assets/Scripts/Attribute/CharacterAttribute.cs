using System;
using Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Attribute
{
    public class CharacterAttribute : SerializedMonoBehaviour
    {
        public float currentHp;
        public FloatAttr maxHp = new();
        public FloatAttr attack = new();

        public void Init(CharacterConfig characterConfig)
        {
            maxHp.Init(characterConfig.hpBaseValue, null, null, null, OnMaxHpChanged);
            attack.Init(characterConfig.attackBaseValue);
        }

        private void OnMaxHpChanged(float oldMaxHp, float newMaxHp)
        {
            // 当最大生命值发生变化时，当前生命值同步按比例变化
            currentHp = newMaxHp * currentHp / oldMaxHp;
            // TODO: 同步给UI
        }
        
        [Button]
        public void TestAddMaxHp(float value)
        {
            maxHp.FixedBonus += value;
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