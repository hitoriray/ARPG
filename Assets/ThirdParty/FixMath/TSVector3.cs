/* Copyright (C) <2009-2011> <Thorben Linneweber, Jitter Physics>
*
*  This software is provided 'as-is', without any express or implied
*  warranty.  In no event will the authors be held liable for any damages
*  arising from the use of this software.
*
*  Permission is granted to anyone to use this software for any purpose,
*  including commercial applications, and to alter it and redistribute it
*  freely, subject to the following restrictions:
*
*  1. The origin of this software must not be misrepresented; you must not
*      claim that you wrote the original software. If you use this software
*      in a product, an acknowledgment in the product documentation would be
*      appreciated but is not required.
*  2. Altered source versions must be plainly marked as such, and must not be
*      misrepresented as being the original software.
*  3. This notice may not be removed or altered from any source distribution.
*/

using System;


namespace FixMath
{
    /// <summary>
    /// A vector structure.
    /// </summary>
    [Serializable]
    public partial struct TSVector3
    {

        private static FP ZeroEpsilonSq = TSMath.Epsilon;
        internal static TSVector3 InternalZero;
        internal static TSVector3 Arbitrary;

        public FP x;

        public FP y;

        public FP z;

        #region Static readonly variables
        /// <summary>
        /// A vector with components (0,0,0);
        /// </summary>
        public static readonly TSVector3 zero;
        /// <summary>
        /// A vector with components (-1,0,0);
        /// </summary>
        public static readonly TSVector3 left;
        /// <summary>
        /// A vector with components (1,0,0);
        /// </summary>
        public static readonly TSVector3 right;
        /// <summary>
        /// A vector with components (0,1,0);
        /// </summary>
        public static readonly TSVector3 up;
        /// <summary>
        /// A vector with components (0,-1,0);
        /// </summary>
        public static readonly TSVector3 down;
        /// <summary>
        /// A vector with components (0,0,-1);
        /// </summary>
        public static readonly TSVector3 back;
        /// <summary>
        /// A vector with components (0,0,1);
        /// </summary>
        public static readonly TSVector3 forward;
        /// <summary>
        /// A vector with components (1,1,1);
        /// </summary>
        public static readonly TSVector3 one;
        /// <summary>
        /// A vector with components
        /// (FP.MinValue,FP.MinValue,FP.MinValue);
        /// </summary>
        public static readonly TSVector3 MinValue;
        /// <summary>
        /// A vector with components
        /// (FP.MaxValue,FP.MaxValue,FP.MaxValue);
        /// </summary>
        public static readonly TSVector3 MaxValue;
        #endregion


        static TSVector3()
        {
            one = new TSVector3(1, 1, 1);
            zero = new TSVector3(0, 0, 0);
            left = new TSVector3(-1, 0, 0);
            right = new TSVector3(1, 0, 0);
            up = new TSVector3(0, 1, 0);
            down = new TSVector3(0, -1, 0);
            back = new TSVector3(0, 0, -1);
            forward = new TSVector3(0, 0, 1);
            MinValue = new TSVector3(FP.MinValue);
            MaxValue = new TSVector3(FP.MaxValue);
            Arbitrary = new TSVector3(1, 1, 1);
            InternalZero = zero;
        }


        public static TSVector3 Abs(TSVector3 other) {
            return new TSVector3(FP.Abs(other.x), FP.Abs(other.y), FP.Abs(other.z));
        }

        public FP this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
        }

        public static FP SqrMagnitude(TSVector3 vector)
        {
            return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
        }

        public FP sqrMagnitude {
            get {
                return (((this.x * this.x) + (this.y * this.y)) + (this.z * this.z));
            }
        }


        public FP magnitude {
            get {
                FP num = ((this.x * this.x) + (this.y * this.y)) + (this.z * this.z);
                return FP.Sqrt(num);
            }
        }

        public static TSVector3 ClampMagnitude(TSVector3 vector, FP maxLength) {
            return vector.sqrMagnitude <= maxLength* maxLength ?  vector : Normalize(vector) * maxLength;
        }


        public TSVector3 normalized {
            get {
                TSVector3 result = new TSVector3(this.x, this.y, this.z);
                result.Normalize();

                return result;
            }
        }


        public TSVector3(int x,int y,int z)
		{
			this.x = (FP)x;
			this.y = (FP)y;
			this.z = (FP)z;
		}

		public TSVector3(FP x, FP y, FP z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Scale(TSVector3 other) {
            this.x = x * other.x;
            this.y = y * other.y;
            this.z = z * other.z;
        }


        public void Set(FP x, FP y, FP z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

		public void Set(TSVector3 other)
		{
			x = other.x;
			y = other.y;
			z = other.z;
		}


        public TSVector3(FP xyz)
        {
            this.x = xyz;
            this.y = xyz;
            this.z = xyz;
        }

		public static TSVector3 Lerp(TSVector3 from, TSVector3 to, FP t) {
            t = TSMath.Clamp(t, FP.Zero, FP.One);
			return from + (to - from) * t;
		}


        public override string ToString() {
            return string.Format("({0}, {1}, {2})", x.AsFloat(), y.AsFloat(), z.AsFloat());
        }



        public override bool Equals(object obj)
        {
            if (!(obj is TSVector3)) return false;
            TSVector3 other = (TSVector3)obj;

            return (((x == other.x) && (y == other.y)) && (z == other.z));
        }


        public static TSVector3 Scale(TSVector3 vecA, TSVector3 vecB) {
            TSVector3 result;
            result.x = vecA.x * vecB.x;
            result.y = vecA.y * vecB.y;
            result.z = vecA.z * vecB.z;

            return result;
        }


        public static bool operator ==(TSVector3 value1, TSVector3 value2)
        {
            return (((value1.x == value2.x) && (value1.y == value2.y)) && (value1.z == value2.z));
        }



        public static bool operator !=(TSVector3 value1, TSVector3 value2)
        {
            if ((value1.x == value2.x) && (value1.y == value2.y))
            {
                return (value1.z != value2.z);
            }
            return true;
        }



        public static TSVector3 Min(TSVector3 value1, TSVector3 value2)
        {
            TSVector3 result;
            TSVector3.Min(ref value1, ref value2, out result);
            return result;
        }


        public static void Min(ref TSVector3 value1, ref TSVector3 value2, out TSVector3 result)
        {
            result.x = (value1.x < value2.x) ? value1.x : value2.x;
            result.y = (value1.y < value2.y) ? value1.y : value2.y;
            result.z = (value1.z < value2.z) ? value1.z : value2.z;
        }



        public static TSVector3 Max(TSVector3 value1, TSVector3 value2)
        {
            TSVector3 result;
            TSVector3.Max(ref value1, ref value2, out result);
            return result;
        }

		public static FP Distance(TSVector3 v1, TSVector3 v2) {
			return FP.Sqrt ((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z));
		}


        public static void Max(ref TSVector3 value1, ref TSVector3 value2, out TSVector3 result)
        {
            result.x = (value1.x > value2.x) ? value1.x : value2.x;
            result.y = (value1.y > value2.y) ? value1.y : value2.y;
            result.z = (value1.z > value2.z) ? value1.z : value2.z;
        }


        public void MakeZero()
        {
            x = FP.Zero;
            y = FP.Zero;
            z = FP.Zero;
        }



        public bool IsZero()
        {
            return (this.sqrMagnitude == FP.Zero);
        }


        public bool IsNearlyZero()
        {
            return (this.sqrMagnitude < ZeroEpsilonSq);
        }



        public static TSVector3 Transform(TSVector3 position, TSMatrix matrix)
        {
            TSVector3 result;
            TSVector3.Transform(ref position, ref matrix, out result);
            return result;
        }


        public static void Transform(ref TSVector3 position, ref TSMatrix matrix, out TSVector3 result)
        {
            FP num0 = ((position.x * matrix.M11) + (position.y * matrix.M21)) + (position.z * matrix.M31);
            FP num1 = ((position.x * matrix.M12) + (position.y * matrix.M22)) + (position.z * matrix.M32);
            FP num2 = ((position.x * matrix.M13) + (position.y * matrix.M23)) + (position.z * matrix.M33);

            result.x = num0;
            result.y = num1;
            result.z = num2;
        }


        public static void TransposedTransform(ref TSVector3 position, ref TSMatrix matrix, out TSVector3 result)
        {
            FP num0 = ((position.x * matrix.M11) + (position.y * matrix.M12)) + (position.z * matrix.M13);
            FP num1 = ((position.x * matrix.M21) + (position.y * matrix.M22)) + (position.z * matrix.M23);
            FP num2 = ((position.x * matrix.M31) + (position.y * matrix.M32)) + (position.z * matrix.M33);

            result.x = num0;
            result.y = num1;
            result.z = num2;
        }


        public static FP Dot(TSVector3 vector1, TSVector3 vector2)
        {
            return TSVector3.Dot(ref vector1, ref vector2);
        }

        public static FP Dot(ref TSVector3 vector1, ref TSVector3 vector2)
        {
            return ((vector1.x * vector2.x) + (vector1.y * vector2.y)) + (vector1.z * vector2.z);
        }


        public static TSVector3 Add(TSVector3 value1, TSVector3 value2)
        {
            TSVector3 result;
            TSVector3.Add(ref value1, ref value2, out result);
            return result;
        }


        public static void Add(ref TSVector3 value1, ref TSVector3 value2, out TSVector3 result)
        {
            FP num0 = value1.x + value2.x;
            FP num1 = value1.y + value2.y;
            FP num2 = value1.z + value2.z;

            result.x = num0;
            result.y = num1;
            result.z = num2;
        }



        public static TSVector3 Divide(TSVector3 value1, FP scaleFactor) {
            TSVector3 result;
            TSVector3.Divide(ref value1, scaleFactor, out result);
            return result;
        }


        public static void Divide(ref TSVector3 value1, FP scaleFactor, out TSVector3 result) {
            result.x = value1.x / scaleFactor;
            result.y = value1.y / scaleFactor;
            result.z = value1.z / scaleFactor;
        }


        public static TSVector3 Subtract(TSVector3 value1, TSVector3 value2)
        {
            TSVector3 result;
            TSVector3.Subtract(ref value1, ref value2, out result);
            return result;
        }


        public static void Subtract(ref TSVector3 value1, ref TSVector3 value2, out TSVector3 result)
        {
            FP num0 = value1.x - value2.x;
            FP num1 = value1.y - value2.y;
            FP num2 = value1.z - value2.z;

            result.x = num0;
            result.y = num1;
            result.z = num2;
        }



        public static TSVector3 Cross(TSVector3 vector1, TSVector3 vector2)
        {
            TSVector3 result;
            TSVector3.Cross(ref vector1, ref vector2, out result);
            return result;
        }


        public static void Cross(ref TSVector3 vector1, ref TSVector3 vector2, out TSVector3 result)
        {
            FP num3 = (vector1.y * vector2.z) - (vector1.z * vector2.y);
            FP num2 = (vector1.z * vector2.x) - (vector1.x * vector2.z);
            FP num = (vector1.x * vector2.y) - (vector1.y * vector2.x);
            result.x = num3;
            result.y = num2;
            result.z = num;
        }



        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }



        public void Negate()
        {
            this.x = -this.x;
            this.y = -this.y;
            this.z = -this.z;
        }


        public static TSVector3 Negate(TSVector3 value)
        {
            TSVector3 result;
            TSVector3.Negate(ref value,out result);
            return result;
        }


        public static void Negate(ref TSVector3 value, out TSVector3 result)
        {
            FP num0 = -value.x;
            FP num1 = -value.y;
            FP num2 = -value.z;

            result.x = num0;
            result.y = num1;
            result.z = num2;
        }



        public static TSVector3 Normalize(TSVector3 value)
        {
            TSVector3 result;
            TSVector3.Normalize(ref value, out result);
            return result;
        }


        public void Normalize()
        {
            FP num2 = ((this.x * this.x) + (this.y * this.y)) + (this.z * this.z);
			if (num2 < 0) {
				this = this / 10000;
				num2 = ((this.x * this.x) + (this.y * this.y)) + (this.z * this.z);
			}
			var sqt = FP.Sqrt(num2);
			FP num = sqt == 0 ? 0 : FP.One / sqt;
			this.x *= num;
            this.y *= num;
            this.z *= num;
        }


        public static void Normalize(ref TSVector3 value, out TSVector3 result)
        {
            FP num2 = ((value.x * value.x) + (value.y * value.y)) + (value.z * value.z);
            FP num = FP.One / FP.Sqrt(num2);
            result.x = value.x * num;
            result.y = value.y * num;
            result.z = value.z * num;
        }


        public static void Swap(ref TSVector3 vector1, ref TSVector3 vector2)
        {
            FP temp;

            temp = vector1.x;
            vector1.x = vector2.x;
            vector2.x = temp;

            temp = vector1.y;
            vector1.y = vector2.y;
            vector2.y = temp;

            temp = vector1.z;
            vector1.z = vector2.z;
            vector2.z = temp;
        }



        public static TSVector3 Multiply(TSVector3 value1, FP scaleFactor)
        {
            TSVector3 result;
            TSVector3.Multiply(ref value1, scaleFactor, out result);
            return result;
        }


        public static void Multiply(ref TSVector3 value1, FP scaleFactor, out TSVector3 result)
        {
            result.x = value1.x * scaleFactor;
            result.y = value1.y * scaleFactor;
            result.z = value1.z * scaleFactor;
        }


        public static TSVector3 operator %(TSVector3 value1, TSVector3 value2)
        {
            TSVector3 result; TSVector3.Cross(ref value1, ref value2, out result);
            return result;
        }



        public static FP operator *(TSVector3 value1, TSVector3 value2)
        {
            return TSVector3.Dot(ref value1, ref value2);
        }



        public static TSVector3 operator *(TSVector3 value1, FP value2)
        {
            TSVector3 result;
            TSVector3.Multiply(ref value1, value2,out result);
            return result;
        }



        public static TSVector3 operator *(FP value1, TSVector3 value2)
        {
            TSVector3 result;
            TSVector3.Multiply(ref value2, value1, out result);
            return result;
        }



        public static TSVector3 operator -(TSVector3 value1, TSVector3 value2)
        {
            TSVector3 result; TSVector3.Subtract(ref value1, ref value2, out result);
            return result;
        }



        public static TSVector3 operator +(TSVector3 value1, TSVector3 value2)
        {
            TSVector3 result; TSVector3.Add(ref value1, ref value2, out result);
            return result;
        }


		public static bool IsInPolygon(TSVector3[] vcs, TSVector3 pos)
		{
			int len = vcs.Length;
			for (int i = 0; i < len; i++)
			{
				int idx = i % len;
				int nextIdx = (i + 1) % len;
				TSVector3 cross = TSVector3.Cross(vcs[idx] - pos, vcs[nextIdx] - pos);
				if (cross.y < 0)
					return false;
			}
			return true;
		}


		public static TSVector3 operator /(TSVector3 value1, FP value2) {
            TSVector3 result;
            TSVector3.Divide(ref value1, value2, out result);
            return result;
        }

        public static FP Angle(TSVector3 a, TSVector3 b) {
            return FP.Acos(a.normalized * b.normalized) * FP.Rad2Deg;
        }

        public TSVector2 ToTSVector2() {
            return new TSVector2(this.x, this.y);
        }

        public static TSVector3 operator -(TSVector3 value)
        {
            value.x = -value.x;
            value.y = -value.y;
            value.z = -value.z;
            return value;
        }

		public static TSVector3 FromString(string str) {
			if (string.IsNullOrEmpty(str))
				return TSVector3.zero;
			var array = str.Split(',');
			if (array.Length < 3)
				return TSVector3.zero;
			FP x = (FP)array[0];
			FP y = (FP)array[1];
			FP z = (FP)array[2];
			return new TSVector3(x,y,z);
		}

		public static TSVector3 MoveTowards(TSVector3 cur,TSVector3 target,FP maxdelta) {
			TSVector3 dir = target - cur;
			TSVector3 move = dir.normalized* maxdelta;
			if (dir.sqrMagnitude <= move.sqrMagnitude) {
				return target;
			}
			return cur + move;
		}

        public readonly TSVector2 XZToTSVector2()
        {
            return new TSVector2(x, z);
        }

		public static implicit operator UnityEngine.Vector3(TSVector3 value)
		{
			return new UnityEngine.Vector3(value.x, value.y, value.z);
		}

		public static explicit operator TSVector3(UnityEngine.Vector3 value)
		{
			return new TSVector3((FP)value.x, (FP)value.y, (FP)value.z);
		}

	}
}
