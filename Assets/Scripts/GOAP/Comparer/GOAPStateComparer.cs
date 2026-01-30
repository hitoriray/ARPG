namespace GOAP
{
    public abstract class GOAPStateComparer
    {
        public abstract bool EqualsComparer(GOAPStateComparer other);
    }

    public abstract class GOAPStateComparer<S, C> : GOAPStateComparer
        where S : GOAPStateBase where C : GOAPStateComparer
    {
        public abstract bool EqualsComparer(C other);
        public override bool EqualsComparer(GOAPStateComparer other)
        {
            return EqualsComparer((C)other);
        }
    }
}

