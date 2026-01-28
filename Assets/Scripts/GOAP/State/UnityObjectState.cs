using System;

namespace GOAP
{
    public class UnityObjectState : GOAPStateBase<UnityObjectState, UnityEngine.Object, UnityObjectStateComparer>
    {
        public override bool EqualsValue(UnityObjectState other)
        {
            return this.value == other.value;
        }

        public override bool Compare(UnityObjectStateComparer comparer)
        {
            switch (comparer.symbol)
            {
                case BoolValue.是:
                    return this.value == comparer.value;
                    break;
                case BoolValue.否:
                    return this.value == comparer.value;
                    break;
            }
            return this.value == comparer.value;
        }

        public override void ApplyEffect(UnityObjectStateComparer comparer)
        {
            if (comparer.symbol == BoolValue.是)
            {
                this.value = comparer.value;
            }
        }
    }

    public class UnityObjectStateComparer : GOAPStateComparer<UnityObjectState, UnityObjectStateComparer>
    {
        public BoolValue symbol;
        public UnityEngine.Object value;
        public override bool EqualsComparator(UnityObjectStateComparer other)
        {
            switch (other.symbol)
            {
                case BoolValue.是:
                    return this.value == other.value;
                    break;
                case BoolValue.否:
                    return this.value != other.value;
                    break;
            }

            return false;
        }
    }
}