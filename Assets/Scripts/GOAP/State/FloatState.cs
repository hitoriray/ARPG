using System;

namespace GOAP
{
    public class FloatState : GOAPStateBase<FloatState, float, FloatStateComparer>
    {
        public override bool EqualsValue(FloatState other)
        {
            return this.value == other.value;
        }

        public override bool Compare(FloatStateComparer comparer)
        {
            switch (comparer.symbol)
            {
                case NumberCompareSymbol.大于:
                    return value > comparer.value;
                case NumberCompareSymbol.小于:
                    return value < comparer.value;
                case NumberCompareSymbol.大于等于:
                    return value >= comparer.value;
                case NumberCompareSymbol.小于等于:
                    return value <= comparer.value;
                case NumberCompareSymbol.提升即可:
                    return value > 0;
                case NumberCompareSymbol.下降即可:
                    return value < 0;
                case NumberCompareSymbol.等于:
                    return value == comparer.value;
            }
            return false;
        }

        public override void ApplyEffect(FloatStateComparer comparer)
        {
            switch (comparer.symbol)
            {
                case NumberCompareSymbol.等于:
                    value = comparer.value;
                    break;
                default:
                    value += comparer.value;
                    break;
            }
        }
    }

    public class FloatStateComparer : GOAPStateComparer<FloatState, FloatStateComparer>
    {
        public NumberCompareSymbol symbol;
        public float value;
        public override bool EqualsComparator(FloatStateComparer other)
        {
            return symbol == other.symbol;
        }
    }
}