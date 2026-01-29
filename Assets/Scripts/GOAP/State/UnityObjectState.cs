using System;

namespace GOAP
{
    public class UnityObjectState : GOAPStateBase<UnityObjectState, UnityEngine.Object, UnityObjectStateComparer>
    {
        public override bool EqualsValue(UnityObjectState other)
        {
            return value == other.value;
        }

        public override bool Compare(UnityObjectStateComparer comparer)
        {
            switch (comparer.symbol)
            {
                case BoolValue.是:
                    return value == comparer.value;
                case BoolValue.否:
                    return value != comparer.value;
            }
            return value == comparer.value;
        }

        public override void ApplyEffect(UnityObjectStateComparer comparer)
        {
            if (comparer.symbol == BoolValue.是)
            {
                value = comparer.value;
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
                    return value == other.value;
                case BoolValue.否:
                    return value != other.value;
            }
            return false;
        }
    }
}