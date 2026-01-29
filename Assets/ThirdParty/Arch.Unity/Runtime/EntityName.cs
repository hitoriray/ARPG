using System;

namespace Arch.Unity
{
    public readonly struct EntityName : IEquatable<EntityName>
    {
        public EntityName(in string value)
        {
            Value = value;
        }

        public readonly string Value;

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is not EntityName entityName) return false;
            return entityName.Equals(this);
        }

        public bool Equals(EntityName other)
        {
            return other.Value.Equals(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}