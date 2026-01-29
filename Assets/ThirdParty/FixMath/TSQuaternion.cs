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
    /// A TSQuaternion representing an orientation.
    /// </summary>
    [Serializable]
    public struct TSQuaternion :IEquatable<TSQuaternion>
    {
		public static FP EqualThreshold = FP.One - FP.EN6;

		public FP x;

        public FP y;

        public FP z;

        public FP w;

        public static readonly TSQuaternion identity;

        static TSQuaternion() {
            identity = new TSQuaternion(0, 0, 0, 1);
        }

        public TSQuaternion(FP x, FP y, FP z, FP w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public void Set(FP new_x, FP new_y, FP new_z, FP new_w) {
            this.x = new_x;
            this.y = new_y;
            this.z = new_z;
            this.w = new_w;
        }

		public void SetFromToRotation(TSVector3 fromDirection, TSVector3 toDirection) {
            TSQuaternion targetRotation = TSQuaternion.FromToRotation(fromDirection, toDirection);
            this.Set(targetRotation.x, targetRotation.y, targetRotation.z, targetRotation.w);
        }

        public TSVector3 eulerAngles {
            get {
                TSVector3 result = new TSVector3();

                FP ysqr = y * y;
                FP t0 = -2 * (ysqr + z * z) + 1;
                FP t1 = +2 * (x * y - w * z);
                FP t2 = -2 * (x * z + w * y);
                FP t3 = +2 * (y * z - w * x);
                FP t4 = -2 * (x * x + ysqr) + 1;

                t2 = t2 > 1.0f ? FP.One : t2;
                t2 = t2 < -1.0f ? -FP.One : t2;

                result.x = FP.Atan2(t3, t4) * FP.Rad2Deg;
                result.y = FP.Asin(t2) * FP.Rad2Deg;
                result.z = FP.Atan2(t1, t0) * FP.Rad2Deg;

                return result * -1;
            }
        }

		public static FP Angle(TSQuaternion a, TSQuaternion b) {
            TSQuaternion aInv = TSQuaternion.Inverse(a);
            TSQuaternion f = b * aInv;

            FP angle = FP.Acos(f.w) * 2 * FP.Rad2Deg;

            if (angle > 180) {
                angle = 360 - angle;
            }

            return angle;
        }


        public static TSQuaternion Add(TSQuaternion quaternion1, TSQuaternion quaternion2)
        {
            TSQuaternion result;
            TSQuaternion.Add(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        public static TSQuaternion LookRotation(TSVector3 forward) {
            return CreateFromMatrix(TSMatrix.LookAt(forward, TSVector3.up));
        }

        public static TSQuaternion LookRotation(TSVector3 forward, TSVector3 upwards) {
            return CreateFromMatrix(TSMatrix.LookAt(forward, upwards));
        }

        public static TSQuaternion Slerp(TSQuaternion from, TSQuaternion to, FP t) {
            t = TSMath.Clamp(t, 0, 1);

            FP dot = Dot(from, to);

            if (dot < 0.0f) {
                to = Multiply(to, -1);
                dot = -dot;
            }

            FP halfTheta = FP.Acos(dot);

            return Multiply(Multiply(from, FP.Sin((1 - t) * halfTheta)) + Multiply(to, FP.Sin(t * halfTheta)), 1 / FP.Sin(halfTheta));
        }

        public static TSQuaternion RotateTowards(TSQuaternion from, TSQuaternion to, FP maxDegreesDelta) {
            FP dot = Dot(from, to);

            if (dot < 0.0f) {
                to = Multiply(to, -1);
                dot = -dot;
            }

            FP halfTheta = FP.Acos(dot);
            FP theta = halfTheta * 2;

            maxDegreesDelta *= FP.Deg2Rad;

            if (maxDegreesDelta >= theta) {
                return to;
            }

            maxDegreesDelta /= theta;

            return Multiply(Multiply(from, FP.Sin((1 - maxDegreesDelta) * halfTheta)) + Multiply(to, FP.Sin(maxDegreesDelta * halfTheta)), 1 / FP.Sin(halfTheta));
        }

        public static TSQuaternion Euler(FP x, FP y, FP z) {
            x *= FP.Deg2Rad;
            y *= FP.Deg2Rad;
            z *= FP.Deg2Rad;

            TSQuaternion rotation;
            TSQuaternion.CreateFromYawPitchRoll(y, x, z, out rotation);

            return rotation;
        }

        public static TSQuaternion Euler(TSVector3 eulerAngles) {
            return Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
        }

        public static TSQuaternion AngleAxis(FP angle, TSVector3 axis) {
            axis = axis * FP.Deg2Rad;
            axis.Normalize();

            FP halfAngle = angle * FP.Deg2Rad * FP.Half;

            TSQuaternion rotation;
            FP sin = FP.Sin(halfAngle);

            rotation.x = axis.x * sin;
            rotation.y = axis.y * sin;
            rotation.z = axis.z * sin;
            rotation.w = FP.Cos(halfAngle);

            return rotation;
        }

        public static void CreateFromYawPitchRoll(FP yaw, FP pitch, FP roll, out TSQuaternion result)
        {
            FP num9 = roll * FP.Half;
            FP num6 = FP.Sin(num9);
            FP num5 = FP.Cos(num9);
            FP num8 = pitch * FP.Half;
            FP num4 = FP.Sin(num8);
            FP num3 = FP.Cos(num8);
            FP num7 = yaw * FP.Half;
            FP num2 = FP.Sin(num7);
            FP num = FP.Cos(num7);
            result.x = ((num * num4) * num5) + ((num2 * num3) * num6);
            result.y = ((num2 * num3) * num5) - ((num * num4) * num6);
            result.z = ((num * num3) * num6) - ((num2 * num4) * num5);
            result.w = ((num * num3) * num5) + ((num2 * num4) * num6);
        }

        public static void Add(ref TSQuaternion quaternion1, ref TSQuaternion quaternion2, out TSQuaternion result)
        {
            result.x = quaternion1.x + quaternion2.x;
            result.y = quaternion1.y + quaternion2.y;
            result.z = quaternion1.z + quaternion2.z;
            result.w = quaternion1.w + quaternion2.w;
        }


        public static TSQuaternion Conjugate(TSQuaternion value)
        {
            TSQuaternion quaternion;
            quaternion.x = -value.x;
            quaternion.y = -value.y;
            quaternion.z = -value.z;
            quaternion.w = value.w;
            return quaternion;
        }

        public static FP Dot(TSQuaternion a, TSQuaternion b) {
            return a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static TSQuaternion Inverse(TSQuaternion rotation) {
            FP invNorm = FP.One / ((rotation.x * rotation.x) + (rotation.y * rotation.y) + (rotation.z * rotation.z) + (rotation.w * rotation.w));
            return TSQuaternion.Multiply(TSQuaternion.Conjugate(rotation), invNorm);
        }

		// copy from OGRE:OgreVector.h getRotationTo line:822
        public static TSQuaternion FromToRotation(TSVector3 fromVector, TSVector3 toVector) {
			var a = TSMath.Sqrt(fromVector.sqrMagnitude * toVector.sqrMagnitude);
			var b = a + TSVector3.Dot(toVector, fromVector);

			if (a == 0 || b == 2 * a)
			{
				return identity;
			}

			TSVector3 axis;
			if (b < a * FP.EN6)
			{
				b = FP.Zero;
				axis = TSMath.Abs(fromVector.x) > TSMath.Abs(fromVector.z) ?
					new TSVector3(-fromVector.y, fromVector.x, FP.Zero) :
					new TSVector3(FP.Zero, -fromVector.z, fromVector.y);
			}
			else
			{
				axis = TSVector3.Cross(fromVector, toVector);
			}

			TSQuaternion q = new TSQuaternion(axis.x, axis.y, axis.z, b);
			q.Normalize();
			return q;
        }

        public static TSQuaternion Lerp(TSQuaternion a, TSQuaternion b, FP t) {
            t = TSMath.Clamp(t, FP.Zero, FP.One);

            return LerpUnclamped(a, b, t);
        }

        public static TSQuaternion LerpUnclamped(TSQuaternion a, TSQuaternion b, FP t) {
            TSQuaternion result = TSQuaternion.Multiply(a, (1 - t)) + TSQuaternion.Multiply(b, t);
            result.Normalize();

            return result;
        }

        public static TSQuaternion Subtract(TSQuaternion quaternion1, TSQuaternion quaternion2)
        {
            TSQuaternion result;
            TSQuaternion.Subtract(ref quaternion1, ref quaternion2, out result);
            return result;
        }


        public static void Subtract(ref TSQuaternion quaternion1, ref TSQuaternion quaternion2, out TSQuaternion result)
        {
            result.x = quaternion1.x - quaternion2.x;
            result.y = quaternion1.y - quaternion2.y;
            result.z = quaternion1.z - quaternion2.z;
            result.w = quaternion1.w - quaternion2.w;
        }

        public static TSQuaternion Multiply(TSQuaternion quaternion1, TSQuaternion quaternion2)
        {
            TSQuaternion result;
            TSQuaternion.Multiply(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        public static void Multiply(ref TSQuaternion quaternion1, ref TSQuaternion quaternion2, out TSQuaternion result)
        {
            FP x = quaternion1.x;
            FP y = quaternion1.y;
            FP z = quaternion1.z;
            FP w = quaternion1.w;
            FP num4 = quaternion2.x;
            FP num3 = quaternion2.y;
            FP num2 = quaternion2.z;
            FP num = quaternion2.w;
            FP num12 = (y * num2) - (z * num3);
            FP num11 = (z * num4) - (x * num2);
            FP num10 = (x * num3) - (y * num4);
            FP num9 = ((x * num4) + (y * num3)) + (z * num2);
            result.x = ((x * num) + (num4 * w)) + num12;
            result.y = ((y * num) + (num3 * w)) + num11;
            result.z = ((z * num) + (num2 * w)) + num10;
            result.w = (w * num) - num9;
        }


        public static TSQuaternion Multiply(TSQuaternion quaternion1, FP scaleFactor)
        {
            TSQuaternion result;
            TSQuaternion.Multiply(ref quaternion1, scaleFactor, out result);
            return result;
        }

        public static void Multiply(ref TSQuaternion quaternion1, FP scaleFactor, out TSQuaternion result)
        {
            result.x = quaternion1.x * scaleFactor;
            result.y = quaternion1.y * scaleFactor;
            result.z = quaternion1.z * scaleFactor;
            result.w = quaternion1.w * scaleFactor;
        }

        public void Normalize()
        {
            FP num2 = (((this.x * this.x) + (this.y * this.y)) + (this.z * this.z)) + (this.w * this.w);
            FP num = 1 / (FP.Sqrt(num2));
            this.x *= num;
            this.y *= num;
            this.z *= num;
            this.w *= num;
        }

        public static TSQuaternion CreateFromMatrix(TSMatrix matrix)
        {
            TSQuaternion result;
            TSQuaternion.CreateFromMatrix(ref matrix, out result);
            return result;
        }

        public static void CreateFromMatrix(ref TSMatrix matrix, out TSQuaternion result)
        {
            FP num8 = (matrix.M11 + matrix.M22) + matrix.M33;
            if (num8 > FP.Zero)
            {
                FP num = FP.Sqrt((num8 + FP.One));
                result.w = num * FP.Half;
                num = FP.Half / num;
                result.x = (matrix.M23 - matrix.M32) * num;
                result.y = (matrix.M31 - matrix.M13) * num;
                result.z = (matrix.M12 - matrix.M21) * num;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                FP num7 = FP.Sqrt((((FP.One + matrix.M11) - matrix.M22) - matrix.M33));
                FP num4 = FP.Half / num7;
                result.x = FP.Half * num7;
                result.y = (matrix.M12 + matrix.M21) * num4;
                result.z = (matrix.M13 + matrix.M31) * num4;
                result.w = (matrix.M23 - matrix.M32) * num4;
            }
            else if (matrix.M22 > matrix.M33)
            {
                FP num6 = FP.Sqrt((((FP.One + matrix.M22) - matrix.M11) - matrix.M33));
                FP num3 = FP.Half / num6;
                result.x = (matrix.M21 + matrix.M12) * num3;
                result.y = FP.Half * num6;
                result.z = (matrix.M32 + matrix.M23) * num3;
                result.w = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                FP num5 = FP.Sqrt((((FP.One + matrix.M33) - matrix.M11) - matrix.M22));
                FP num2 = FP.Half / num5;
                result.x = (matrix.M31 + matrix.M13) * num2;
                result.y = (matrix.M32 + matrix.M23) * num2;
                result.z = FP.Half * num5;
                result.w = (matrix.M12 - matrix.M21) * num2;
            }
        }

        public static TSQuaternion operator *(TSQuaternion value1, TSQuaternion value2)
        {
            TSQuaternion result;
            TSQuaternion.Multiply(ref value1, ref value2,out result);
            return result;
        }

        public static TSQuaternion operator +(TSQuaternion value1, TSQuaternion value2)
        {
            TSQuaternion result;
            TSQuaternion.Add(ref value1, ref value2, out result);
            return result;
        }

        public static TSQuaternion operator -(TSQuaternion value1, TSQuaternion value2)
        {
            TSQuaternion result;
            TSQuaternion.Subtract(ref value1, ref value2, out result);
            return result;
        }

		public static bool operator ==(TSQuaternion lhs, TSQuaternion rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(TSQuaternion lhs, TSQuaternion rhs)
		{
			return !(lhs == rhs);
		}

		public static TSVector3 operator *(TSQuaternion quat, TSVector3 vec) {
            FP num = quat.x * 2;
            FP num2 = quat.y * 2;
            FP num3 = quat.z * 2;
            FP num4 = quat.x * num;
            FP num5 = quat.y * num2;
            FP num6 = quat.z * num3;
            FP num7 = quat.x * num2;
            FP num8 = quat.x * num3;
            FP num9 = quat.y * num3;
            FP num10 = quat.w * num;
            FP num11 = quat.w * num2;
            FP num12 = quat.w * num3;

            TSVector3 result;
            result.x = (1 - (num5 + num6)) * vec.x + (num7 - num12) * vec.y + (num8 + num11) * vec.z;
            result.y = (num7 + num12) * vec.x + (1 - (num4 + num6)) * vec.y + (num9 - num10) * vec.z;
            result.z = (num8 - num11) * vec.x + (num9 + num10) * vec.y + (1 - (num4 + num5)) * vec.z;

            return result;
        }

		public override bool Equals(object obj)
		{
			if (!(obj is TSQuaternion quaternion))
                return false;
            return Equals(quaternion);
		}

		public bool Equals(TSQuaternion other)
		{
			return Dot(this, other) > EqualThreshold;
		}

		public override int GetHashCode()
		{
			return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
		}

		public override string ToString() {
            return $"({x.AsFloat():f1}, {y.AsFloat():f1}, {z.AsFloat():f1}, {w.AsFloat():f1})";
        }

		public static implicit operator UnityEngine.Quaternion(TSQuaternion value)
		{
			return new UnityEngine.Quaternion(value.x, value.y, value.z, value.w);
		}

		public static explicit operator TSQuaternion(UnityEngine.Quaternion value)
		{
			return new TSQuaternion((FP)value.x, (FP)value.y, (FP)value.z, (FP)value.w);
		}
    }
}
