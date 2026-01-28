using System;

namespace GOAP
{
    public class BoolState : GOAPStateBase<BoolState, bool, BoolStateComparer>
    {
        public override bool EqualsValue(BoolState other)
        {
            return this.value == other.value;
        }
        
        public override bool Compare(BoolStateComparer comparer)
        {
            switch (comparer.value)
            {
                case BoolValue.是:
                    return value;
                case BoolValue.否:
                    return !value;
            }
            return false;
        }

        public override void ApplyEffect(BoolStateComparer comparer)
        {
            switch (comparer.value)
            {
                case BoolValue.是:
                    value = true;
                    break;
                case BoolValue.否:
                    value = false;
                    break;
            }
        }
    }
    
    public class BoolStateComparer : GOAPStateComparer<BoolState, BoolStateComparer>
    {
        public BoolValue value;
        public override bool EqualsComparator(BoolStateComparer other)
        {
            return this.value == other.value;
        }
    }

    public enum BoolValue
    {
        是,
        否,
    }
}