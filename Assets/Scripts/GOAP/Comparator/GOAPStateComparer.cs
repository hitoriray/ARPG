namespace GOAP
{
    public abstract class GOAPStateComparer
    {
        public abstract bool EqualsComparator(GOAPStateComparer other);
    }

    public abstract class GOAPStateComparer<S, C> : GOAPStateComparer
        where S : GOAPStateBase where C : GOAPStateComparer
    {
        public abstract bool EqualsComparator(C other);
        public override bool EqualsComparator(GOAPStateComparer other)
        {
            return EqualsComparator((C)other);
        }
    }
}

