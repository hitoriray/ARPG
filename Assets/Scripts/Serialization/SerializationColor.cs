using System;
using UnityEngine;

namespace Serialization
{
    [Serializable]
    public struct SerializationColor
    {
        public float r, g, b, a;

        public SerializationColor(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + r.GetHashCode();
                hash = hash * 31 + g.GetHashCode();
                hash = hash * 31 + b.GetHashCode();
                hash = hash * 31 + a.GetHashCode();
                return hash;
            }
        }
        
        public override bool Equals(object obj)
        {
            return obj is SerializationColor other && Equals(other);
        }

        public bool Equals(SerializationColor other)
        {
            return r.Equals(other.r)
                   && g.Equals(other.g)
                   && b.Equals(other.b)
                   && a.Equals(other.a);;
        }

        public override string ToString()
        {
            return $"({r}, {g}, {b}, {a})";
        }
    }

    public static class SerializationColorExtensions
    {
        public static Color ConvertToUnityColor(this SerializationColor sColor)
        {
            return new Color(sColor.r, sColor.g, sColor.b, sColor.a);
        }

        public static SerializationColor ConvertToSerializationColor(this Color color)
        {
            return new SerializationColor(color.r, color.g, color.b, color.a);
        }
    }
}
