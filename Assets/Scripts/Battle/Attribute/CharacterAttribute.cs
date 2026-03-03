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
        
        /// <summary>当前血量/最大血量发生变化时触发 (current, max)</summary>
        public event Action<float, float> OnHpChanged;
        /// <summary>当前魔量/最大魔量发生变化时触发 (current, max)</summary>
        public event Action<float, float> OnMpChanged;
        
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

        /// <summary>
        /// 根据等级和成长配置更新属性基础值。
        /// 在角色初始化后、每次升级后都应调用此方法。
        /// </summary>
        /// <param name="level">当前等级</param>
        /// <param name="baseConfig">角色基础配置（提供 1 级时的属性原始值）</param>
        /// <param name="growthConfig">成长曲线配置</param>
        public void ApplyLevel(int level, CharacterConfig baseConfig, Config.LevelGrowthConfig growthConfig)
        {
            if (baseConfig == null || growthConfig == null) return;

            float hpMult     = growthConfig.GetHpMultiplier(level);
            float atkMult    = growthConfig.GetAttackMultiplier(level);

            maxHp.BaseValue  = baseConfig.hpBaseValue  * hpMult;
            maxMp.BaseValue  = baseConfig.mpBaseValue;            // MP 暂不随等级成长（可自行扩展）
            attack.BaseValue = baseConfig.attackBaseValue * atkMult;

            // 同步当前 HP/MP 到新上限（防止溢出）
            SetHp(currentHp);
            SetMp(currentMp);
        }

        public void AddHp(float value)
        {
            SetHp(currentHp + value);
        }

        public void SetHp(float value)
        {
            currentHp = Mathf.Clamp(value, 0, maxHp.Total);
            OnHpChanged?.Invoke(currentHp, maxHp.Total);
        }

        public void AddMp(float value)
        {
            SetMp(currentMp + value);
        }

        public void SetMp(float value)
        {
            currentMp = Mathf.Clamp(value, 0, maxMp.Total);
            OnMpChanged?.Invoke(currentMp, maxMp.Total);
        }
        
        private void OnMaxHpChanged(float oldMaxHp, float newMaxHp)
        {
            // 当最大生命值发生变化时，当前生命值同步按比例变化
            currentHp = newMaxHp * currentHp / oldMaxHp;
            OnHpChanged?.Invoke(currentHp, newMaxHp);
        }
        
        private void OnMaxMpChanged(float oldMaxMp, float newMaxMp)
        {
            currentMp = newMaxMp * currentMp / oldMaxMp;
            OnMpChanged?.Invoke(currentMp, newMaxMp);
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