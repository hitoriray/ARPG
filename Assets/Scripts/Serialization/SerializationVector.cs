using System;
using UnityEngine;

namespace Serialization
{
    [Serializable]
    public struct SerializationVector3
    {
        public float x, y, z;

        public SerializationVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x.GetHashCode();
                hash = hash * 31 + y.GetHashCode();
                hash = hash * 31 + z.GetHashCode();
                return hash;
            }
        }
        
        public override bool Equals(object obj)
        {
            return obj is SerializationVector3 other && Equals(other);
        }

        public bool Equals(SerializationVector3 other)
        {
            return x.Equals(other.x)
                   && y.Equals(other.y)
                   && z.Equals(other.z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }
    
    [Serializable]
    public struct SerializationVector2
    {
        public float x, y;

        public SerializationVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x.GetHashCode();
                hash = hash * 31 + y.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is SerializationVector2 other && Equals(other);
        }

        public bool Equals(SerializationVector2 other)
        {
            return x.Equals(other.x)
                   && y.Equals(other.y);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }

    public static class SerializationVectorExtensions
    {
        #region Vector3
        public static Vector3 ConvertToUnityVector3(this SerializationVector3 sVector3)
        {
            return new Vector3(sVector3.x, sVector3.y, sVector3.z);
        }

        public static SerializationVector3 ConvertToSerializationVector3(this Vector3 vector3)
        {
            return new SerializationVector3(vector3.x, vector3.y, vector3.z);
        }

        public static Vector3Int ConvertToUnityVector3Int(this SerializationVector3 sVector3)
        {
            return new Vector3Int((int)sVector3.x, (int)sVector3.y, (int)sVector3.z);
        }

        public static SerializationVector3 ConvertToSerializationVector3(this Vector3Int vector3)
        {
            return new SerializationVector3(vector3.x, vector3.y, vector3.z);
        }
        #endregion
        
        #region Vector2
        public static Vector3 ConvertToUnityVector2(this SerializationVector2 sVector2)
        {
            return new Vector2(sVector2.x, sVector2.y);
        }

        public static SerializationVector2 ConvertToSerializationVector2(this Vector2 vector2)
        {
            return new SerializationVector2(vector2.x, vector2.y);
        }

        public static Vector2Int ConvertToUnityVector2Int(this SerializationVector2 sVector2)
        {
            return new Vector2Int((int)sVector2.x, (int)sVector2.y);
        }

        public static SerializationVector2 ConvertToSerializationVector2(this Vector2Int vector2)
        {
            return new SerializationVector2(vector2.x, vector2.y);
        }
        #endregion
    }
}