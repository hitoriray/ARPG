using Config;
using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 属性修改器
    /// </summary>
    public readonly struct AttrModifier
    {
        public readonly AttributeType Type;
        public readonly FP Value;
        public readonly bool IsPercent;

        public AttrModifier(AttributeType type, FP value, bool isPercent)
        {
            Type = type;
            Value = value;
            IsPercent = isPercent;
        }
    }
}