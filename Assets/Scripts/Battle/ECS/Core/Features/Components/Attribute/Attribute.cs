using System.Collections.Generic;
using Config;
using FixMath;

namespace Battle.ECS.Component
{
    public struct Attribute
    {
        private Dictionary<AttributeType, List<FP>> modifierStacks;

        public FP Attack;        // 攻击力
        public FP MaxHp;
        public FP MaxMp;
        public FP Defense;
        public FP Speed;
        public FP CritRate;
        public FP CritDamage;

        public Attribute(FP attack, FP maxHp, FP maxMp, FP defense, FP speed)
        {
            Attack = attack;
            MaxHp = maxHp;
            MaxMp = maxMp;
            Defense = defense;
            Speed = speed;
            CritRate = FP.Zero;
            CritDamage = FP.FromFloat(1.5f);
            modifierStacks = new Dictionary<AttributeType, List<FP>>();
        }

        public void AddModifier(AttributeType type, FP value, bool isPercent)
        {
            modifierStacks ??= new();
            if (!modifierStacks.ContainsKey(type))
                modifierStacks[type] = new List<FP>();
            modifierStacks[type].Add(value);
            ApplyModifier(type, value, isPercent, true);
        }

        public void RemoveModifier(AttributeType type, FP value, bool isPercent)
        {
            if (modifierStacks == null || !modifierStacks.ContainsKey(type))
                return;
            
            var stack = modifierStacks[type];
            if (stack.Remove(value))
            {
                ApplyModifier(type, value, isPercent, false);
            }
        }

        private void ApplyModifier(AttributeType type, FP value, bool isPercent, bool isAdd)
        {
            int sign = isAdd ? 1 : -1;
            FP actualValue = value * sign;
            switch (type)
            {
                case AttributeType.Attack:
                    Attack += isPercent ? Attack * actualValue : actualValue;
                    break;
                case AttributeType.Defense:
                    Defense += isPercent ? Defense * actualValue : actualValue;
                    break;
                case AttributeType.MaxHP:
                    MaxHp += isPercent ? MaxHp * actualValue : actualValue;
                    break;
                case AttributeType.MaxMP:
                    MaxMp += isPercent ? MaxMp * actualValue : actualValue;
                    break;
                case AttributeType.Speed:
                    Speed += isPercent ? Speed * actualValue : actualValue;
                    break;
                case AttributeType.CritRate:
                    CritRate += actualValue;
                    break;
                case AttributeType.CritDamage:
                    CritDamage += actualValue;
                    break;
            }
        }
    }
}