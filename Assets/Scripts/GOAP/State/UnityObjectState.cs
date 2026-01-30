namespace GOAP
{
    public class UnityObjectState : GOAPStateBase<UnityObjectState, UnityEngine.Object, UnityObjectStateComparer>
    {
        public override bool EqualsValue(UnityObjectState other)
        {
            return value == other.value;
        }

        public override bool CompareForPrecondition(UnityObjectStateComparer comparer)
        {
            switch (comparer.symbol)
            {
                case UnityObjectSymbol.是:
                    return value == comparer.value;
                case UnityObjectSymbol.否:
                    return value != comparer.value;
                case UnityObjectSymbol.为空:
                    return value == null;
                case UnityObjectSymbol.不为空:
                    return value != null;
            }
            return value == comparer.value;
        }

        public override bool CompareForEffect(UnityObjectStateComparer comparer)
        {
            return CompareForPrecondition(comparer);
        }

        public override void ApplyEffect(UnityObjectStateComparer comparer)
        {
            switch (comparer.symbol)
            {
                case UnityObjectSymbol.是:
                    this.value = comparer.value;
                    break;
                case UnityObjectSymbol.为空:
                    this.value = null;
                    break;
            }
        }
    }

    public class UnityObjectStateComparer : GOAPStateComparer<UnityObjectState, UnityObjectStateComparer>
    {
        public UnityObjectSymbol symbol;
        public UnityEngine.Object value;
        public override bool EqualsComparer(UnityObjectStateComparer other)
        {
            if (other.symbol != symbol)
                return false;
            switch (other.symbol)
            {
                case UnityObjectSymbol.是:
                case UnityObjectSymbol.否:
                    return value == other.value;
                case UnityObjectSymbol.不为空:
                    break;
            }
            return true;
        }
    }

    public enum UnityObjectSymbol
    {
        是,
        否,
        为空,
        不为空
    }
}