#region License

/*
MIT License
Copyright © 2006 The Mono.Xna Team

All rights reserved.

Authors
 * Alan McGovern

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FixMath {

    [Serializable]
    public partial struct TSVector2 : IEquatable<TSVector2>
    {

        private static TSVector2 zeroVector = new TSVector2(0, 0);
        private static TSVector2 oneVector = new TSVector2(1, 1);

        private static TSVector2 rightVector = new TSVector2(1, 0);
        private static TSVector2 leftVector = new TSVector2(-1, 0);

        private static TSVector2 upVector = new TSVector2(0, 1);
        private static TSVector2 downVector = new TSVector2(0, -1);




        public FP x;
        public FP y;


        public FP X {
            get { return x; }
            set { x = value; }
        }

        public FP Y
        {
            get { return y; }
            set { y = value; }
        }

        public static TSVector2 zero
        {
            get { return zeroVector; }
        }

        public static TSVector2 one
        {
            get { return oneVector; }
        }

        public static TSVector2 right
        {
            get { return rightVector; }
        }

        public static TSVector2 left {
            get { return leftVector; }
        }

        public static TSVector2 up
        {
            get { return upVector; }
        }

        public static TSVector2 down {
            get { return downVector; }
        }



        public TSVector2(FP x, FP y)
        {
            this.x = x;
            this.y = y;
        }


        public TSVector2(FP value)
        {
            x = value;
            y = value;
        }

        public void Set(FP x, FP y) {
            this.x = x;
            this.y = y;
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

                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
        }


        public TSVector2 RotateRadian(FP angleRad)
        {
            var sin = TSMath.Sin(angleRad);
            var cos = TSMath.Cos(angleRad);
            FP newx = x * cos - y * sin;
            FP newy = x * sin + y * cos;
            return new TSVector2(newx, newy);
        }

        public static void Reflect(ref TSVector2 vector, ref TSVector2 normal, out TSVector2 result)
        {
            FP dot = Dot(vector, normal);
            result.x = vector.x - 2 * dot * normal.x;
            result.y = vector.y - 2 * dot * normal.y;
        }

        public static TSVector2 Reflect(TSVector2 vector, TSVector2 normal)
        {
            TSVector2 result;
            Reflect(ref vector, ref normal, out result);
            return result;
        }

        public static TSVector2 Add(TSVector2 value1, TSVector2 value2)
        {
            value1.x += value2.x;
            value1.y += value2.y;
            return value1;
        }

        public static void Add(ref TSVector2 value1, ref TSVector2 value2, out TSVector2 result)
        {
            result.x = value1.x + value2.x;
            result.y = value1.y + value2.y;
        }


        public static TSVector2 Barycentric(TSVector2 value1, TSVector2 value2, TSVector2 value3, FP amount1, FP amount2)
        {
            return new TSVector2(
                TSMath.Barycentric(value1.x, value2.x, value3.x, amount1, amount2),
                TSMath.Barycentric(value1.y, value2.y, value3.y, amount1, amount2));
        }

        public static void Barycentric(ref TSVector2 value1, ref TSVector2 value2, ref TSVector2 value3, FP amount1,
                                       FP amount2, out TSVector2 result)
        {
            result = new TSVector2(
                TSMath.Barycentric(value1.x, value2.x, value3.x, amount1, amount2),
                TSMath.Barycentric(value1.y, value2.y, value3.y, amount1, amount2));
        }

        public static TSVector2 CatmullRom(TSVector2 value1, TSVector2 value2, TSVector2 value3, TSVector2 value4, FP amount)
        {
            return new TSVector2(
                TSMath.CatmullRom(value1.x, value2.x, value3.x, value4.x, amount),
                TSMath.CatmullRom(value1.y, value2.y, value3.y, value4.y, amount));
        }

        public static void CatmullRom(ref TSVector2 value1, ref TSVector2 value2, ref TSVector2 value3, ref TSVector2 value4,
                                      FP amount, out TSVector2 result)
        {
            result = new TSVector2(
                TSMath.CatmullRom(value1.x, value2.x, value3.x, value4.x, amount),
                TSMath.CatmullRom(value1.y, value2.y, value3.y, value4.y, amount));
        }

        public static TSVector2 Clamp(TSVector2 value1, TSVector2 min, TSVector2 max)
        {
            return new TSVector2(
                TSMath.Clamp(value1.x, min.x, max.x),
                TSMath.Clamp(value1.y, min.y, max.y));
        }

        public static void Clamp(ref TSVector2 value1, ref TSVector2 min, ref TSVector2 max, out TSVector2 result)
        {
            result = new TSVector2(
                TSMath.Clamp(value1.x, min.x, max.x),
                TSMath.Clamp(value1.y, min.y, max.y));
        }


        public static FP Distance(TSVector2 value1, TSVector2 value2)
        {
            FP result;
            DistanceSquared(ref value1, ref value2, out result);
            return (FP)FP.Sqrt(result);
        }


        public static void Distance(ref TSVector2 value1, ref TSVector2 value2, out FP result)
        {
            DistanceSquared(ref value1, ref value2, out result);
            result = (FP)FP.Sqrt(result);
        }

        public static FP SqrMagnitude(TSVector2 vector)
        {
            return vector.x * vector.x + vector.y * vector.y;
        }

        public static FP DistanceSquared(TSVector2 value1, TSVector2 value2)
        {
            FP result;
            DistanceSquared(ref value1, ref value2, out result);
            return result;
        }

        public static void DistanceSquared(ref TSVector2 value1, ref TSVector2 value2, out FP result)
        {
            result = (value1.x - value2.x) * (value1.x - value2.x) + (value1.y - value2.y) * (value1.y - value2.y);
        }


        public static TSVector2 Divide(TSVector2 value1, TSVector2 value2)
        {
            value1.x /= value2.x;
            value1.y /= value2.y;
            return value1;
        }

        public static void Divide(ref TSVector2 value1, ref TSVector2 value2, out TSVector2 result)
        {
            result.x = value1.x / value2.x;
            result.y = value1.y / value2.y;
        }

        public static TSVector2 Divide(TSVector2 value1, FP divider)
        {
            FP factor = 1 / divider;
            value1.x *= factor;
            value1.y *= factor;
            return value1;
        }

        public static void Divide(ref TSVector2 value1, FP divider, out TSVector2 result)
        {
            FP factor = 1 / divider;
            result.x = value1.x * factor;
            result.y = value1.y * factor;
        }

        public static FP Dot(TSVector2 value1, TSVector2 value2)
        {
            return value1.x * value2.x + value1.y * value2.y;
        }

        public FP Dot(TSVector2 p)
        {
            return X * p.X + Y * p.Y;
        }


        public static void Dot(ref TSVector2 value1, ref TSVector2 value2, out FP result)
        {
            result = value1.x * value2.x + value1.y * value2.y;
        }

        public static FP Cross(TSVector2 a, TSVector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        public FP Cross(TSVector2 b)
        {
            return x * b.y - y * b.x;
        }

        public static bool SegmentCast(TSVector2 p01, TSVector2 p02, TSVector2 p11, TSVector2 p12)
        {
            FP c1 = Cross(p02 - p01, p11 - p01), c2 = Cross(p02 - p01, p12 - p01);
            FP d1 = Cross(p12 - p11, p01 - p11), d2 = Cross(p12 - p11, p02 - p11);
            return TSMath.Sign(c1) * TSMath.Sign(c2) < 0 && TSMath.Sign(d1) * TSMath.Sign(d2) < 0;
        }

		public static bool SegmentRayCast(TSVector2 p01, TSVector2 p02, TSVector2 rayStart, TSVector2 dir,out TSVector2 result)
		{
			result = rayStart;
			TSVector2 uA = p01 - rayStart;
			TSVector2 uC = p02 - rayStart;
			bool isIn = Cross(uA, dir) * Cross(uA, uC) >= 0
				&& Cross(uC, dir) * Cross(uC, uA) >= 0;
			if (!isIn)
				return false;

			TSVector2 line1 = p02 - p01;
			FP d1 = Cross(dir, uA);
			FP t = d1 / Cross(line1, dir);
			result = p01 + line1 * t;
			return t >= 0 && t<=1;
		}

		public static bool SegmentCast(TSVector2 p01, TSVector2 p02, TSVector2 p11, TSVector2 p12,  ref TSVector2 hitPos)
        {
            TSVector2 tmpLine = TSVector2.zero;
            FP tmpCross = 0, tmpHitPercent = 0;
            return SegmentCast(p01,p02,p11,p12,ref tmpLine,ref tmpCross,ref tmpHitPercent,ref hitPos);
        }

        public static bool SegmentCast(TSVector2 p01, TSVector2 p02, TSVector2 p11, TSVector2 p12, ref TSVector2 line1, ref FP p11ToP01_Line1_cross, ref FP hitPercent, ref TSVector2 hitPos)
		 {
			line1 = p02 - p01; TSVector2 line2 = p12 - p11;
			TSVector2 u = p01 - p11;
			p11ToP01_Line1_cross = Cross(line1, p11 - p01); FP c2 = Cross(line1, p12 - p01);
			FP d1 = Cross(line2, u), d2 = Cross(line2, p02 - p11);
			bool result = TSMath.Sign(p11ToP01_Line1_cross) * TSMath.Sign(c2) < 0 && TSMath.Sign(d1) * TSMath.Sign(d2) < 0;
			if (result)
			{
				hitPercent = d1 / Cross(line1, line2);
				hitPos = p01 + line1 * hitPercent;
			}
			return result;
		}

        public static bool SegmentPolygonCast(TSVector2 start, TSVector2 end, TSVector2[] array, out TSVector2 normal, out TSVector2 hitPos, out FP sqrDist)
        {
            return SegmentPolygonCast(start, end, array, out normal, out hitPos,out sqrDist, out _);
        }

        public static bool SegmentPolygonCast(TSVector2 start, TSVector2 end, TSVector2[] array, out TSVector2 normal, out TSVector2 hitPos,out FP sqrDist,out int select)
        {
            select = -1;

            sqrDist = FP.MaxValue;
            hitPos = TSVector2.zero;
            normal = TSVector2.zero;

            if (TSVector2.SegmentPolygonCast(start, end, array, ref select, ref sqrDist, ref hitPos))
            {
                var vec = array[(select + 1) % array.Length] - array[select];
                normal = new TSVector2(-vec.y, vec.x).normalized;
                return true;
            }
            return false;
        }


		public static bool SegmentPolygonCast(TSVector2 start,TSVector2 end,TSVector2[] array,ref int select,ref FP sqrDistToStart,ref TSVector2 hitPos)
        {
            TSVector2 tmpHitPos = TSVector2.zero;

            sqrDistToStart = FP.MaxValue;
            select = -1;


            for (int i = 0; i < array.Length; i++)
            {
                int nextIdx = (i + 1) % array.Length;

                if (!SegmentCast(array[i], array[nextIdx], start, end, ref tmpHitPos))
                    continue;

                var sqrDist = (tmpHitPos - start).sqrMagnitude;
                if (sqrDist < sqrDistToStart)
                {
                    sqrDistToStart = sqrDist;
                    select = i;
                    hitPos = tmpHitPos;
                }
            }
            return select >= 0;
        }


        public static bool TryGetClosestPositionOnLine(TSVector2 lineStart, TSVector2 lineEnd, TSVector2 position, out FP dist,out FP percent)
		{
			percent = 0;
			dist = 0;
			TSVector2 line = (lineEnd - lineStart);
			FP lineSqr = line.sqrMagnitude;
			if (lineSqr == 0f)
				return false;
			var toStart = position - lineStart;
			FP dot0 = toStart.x * line.x + toStart.y * line.y;
			percent = dot0 / lineSqr;
			if (percent < 0 || percent > 1)
				return false;
			FP dot1 = toStart.x * line.y - toStart.y * line.x;
			dist = TSMath.Abs(dot1) / FP.Sqrt(lineSqr);
			return true;
		}

		public override bool Equals(object obj)
        {
            return (obj is TSVector2) ? this == ((TSVector2)obj) : false;
        }

        public bool Equals(TSVector2 other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return (int)(x + y);
        }

        public static TSVector2 Hermite(TSVector2 value1, TSVector2 tangent1, TSVector2 value2, TSVector2 tangent2, FP amount)
        {
            TSVector2 result = new TSVector2();
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
            return result;
        }

        public static void Hermite(ref TSVector2 value1, ref TSVector2 tangent1, ref TSVector2 value2, ref TSVector2 tangent2,
                                   FP amount, out TSVector2 result)
        {
            result.x = TSMath.Hermite(value1.x, tangent1.x, value2.x, tangent2.x, amount);
            result.y = TSMath.Hermite(value1.y, tangent1.y, value2.y, tangent2.y, amount);
        }

        public FP magnitude
        {
            get {
                FP result;
                DistanceSquared(ref this, ref zeroVector, out result);
                return FP.Sqrt(result);
            }
        }

        public FP sqrMagnitude
        {
            get {
                return LengthSquared();
            }
        }

        public FP Length() {
            return magnitude;
        }

        public static TSVector2 ClampMagnitude(TSVector2 vector, FP maxLength) {
            return Normalize(vector) * maxLength;
        }

        public FP LengthSquared()
        {
            FP result;
            DistanceSquared(ref this, ref zeroVector, out result);
            return result;
        }


        public static TSVector2 Lerp(TSVector2 value1, TSVector2 value2, FP amount) {
            amount = TSMath.Clamp(amount, 0, 1);

            return new TSVector2(
                TSMath.Lerp(value1.x, value2.x, amount),
                TSMath.Lerp(value1.y, value2.y, amount));
        }

        public static TSVector2 LerpUnclamped(TSVector2 value1, TSVector2 value2, FP amount)
        {
            return new TSVector2(
                TSMath.Lerp(value1.x, value2.x, amount),
                TSMath.Lerp(value1.y, value2.y, amount));
        }

        public static void LerpUnclamped(ref TSVector2 value1, ref TSVector2 value2, FP amount, out TSVector2 result)
        {
            result = new TSVector2(
                TSMath.Lerp(value1.x, value2.x, amount),
                TSMath.Lerp(value1.y, value2.y, amount));
        }

        public static TSVector2 Max(TSVector2 value1, TSVector2 value2)
        {
            return new TSVector2(
                TSMath.Max(value1.x, value2.x),
                TSMath.Max(value1.y, value2.y));
        }

        public static void Max(ref TSVector2 value1, ref TSVector2 value2, out TSVector2 result)
        {
            result.x = TSMath.Max(value1.x, value2.x);
            result.y = TSMath.Max(value1.y, value2.y);
        }

        public static TSVector2 Min(TSVector2 value1, TSVector2 value2)
        {
            return new TSVector2(
                TSMath.Min(value1.x, value2.x),
                TSMath.Min(value1.y, value2.y));
        }

        public static void Min(ref TSVector2 value1, ref TSVector2 value2, out TSVector2 result)
        {
            result.x = TSMath.Min(value1.x, value2.x);
            result.y = TSMath.Min(value1.y, value2.y);
        }

        public void Scale(TSVector2 other) {
            this.x = x * other.x;
            this.y = y * other.y;
        }

        public static TSVector2 Scale(TSVector2 value1, TSVector2 value2) {
            TSVector2 result;
            result.x = value1.x * value2.x;
            result.y = value1.y * value2.y;

            return result;
        }

        public static TSVector2 Multiply(TSVector2 value1, TSVector2 value2)
        {
            value1.x *= value2.x;
            value1.y *= value2.y;
            return value1;
        }

        public static TSVector2 Multiply(TSVector2 value1, FP scaleFactor)
        {
            value1.x *= scaleFactor;
            value1.y *= scaleFactor;
            return value1;
        }

        public static void Multiply(ref TSVector2 value1, FP scaleFactor, out TSVector2 result)
        {
            result.x = value1.x*scaleFactor;
            result.y = value1.y*scaleFactor;
        }

        public static void Multiply(ref TSVector2 value1, ref TSVector2 value2, out TSVector2 result)
        {
            result.x = value1.x*value2.x;
            result.y = value1.y*value2.y;
        }

        public static TSVector2 Negate(TSVector2 value)
        {
            value.x = -value.x;
            value.y = -value.y;
            return value;
        }

        public static void Negate(ref TSVector2 value, out TSVector2 result)
        {
            result.x = -value.x;
            result.y = -value.y;
        }

        public void Normalize()
        {
            Normalize(ref this, out this);
        }

		public TSVector2 Normalized(out FP length)
		{
			FP sqrDis = x * x + y * y;
			length = FP.Sqrt(sqrDis);
			sqrDis = 1/ length;
			return new TSVector2(x * sqrDis, y * sqrDis);
		}

		public static TSVector2 Normalize(TSVector2 value)
        {
            Normalize(ref value, out value);
            return value;
        }

        public TSVector2 normalized {
            get {
                TSVector2 result;
                TSVector2.Normalize(ref this, out result);

                return result;
            }
        }

		public static void Normalize(ref TSVector2 value, out TSVector2 result)
        {
			FP factor = value.x * value.x + value.y * value.y;
            factor = 1/FP.Sqrt(factor);
            result.x = value.x*factor;
            result.y = value.y*factor;
        }

		public static TSVector2 SmoothStep(TSVector2 value1, TSVector2 value2, FP amount)
        {
            return new TSVector2(
                TSMath.SmoothStep(value1.x, value2.x, amount),
                TSMath.SmoothStep(value1.y, value2.y, amount));
        }

        public static void SmoothStep(ref TSVector2 value1, ref TSVector2 value2, FP amount, out TSVector2 result)
        {
            result = new TSVector2(
                TSMath.SmoothStep(value1.x, value2.x, amount),
                TSMath.SmoothStep(value1.y, value2.y, amount));
        }

        public static TSVector2 Subtract(TSVector2 value1, TSVector2 value2)
        {
            value1.x -= value2.x;
            value1.y -= value2.y;
            return value1;
        }

        public static void Subtract(ref TSVector2 value1, ref TSVector2 value2, out TSVector2 result)
        {
            result.x = value1.x - value2.x;
            result.y = value1.y - value2.y;
        }

		public static TSVector2 MoveTowards(TSVector2 from, TSVector2 to, FP maxDelta)
		{
			TSVector2 a = to - from;
			FP magnitude = a.magnitude;
			if (!(magnitude <= maxDelta) && magnitude != 0)
			{
				return from + a / magnitude * maxDelta;
			}
			return to;
		}

		public static FP Angle(TSVector2 a, TSVector2 b) {
            return FP.Acos(a.normalized * b.normalized) * FP.Rad2Deg;
        }

		public static bool IsInPolygon(TSVector2[] vcs, TSVector2 pos)
		{
			return IsInPolygon(vcs,vcs.Length,pos);
		}

		public static bool IsInPolygon(TSVector2[] vcs, int len,TSVector2 pos)
		{
			int preV = 0;
			for (int i = 0; i < len; i++)
			{
				int idx = i % len;
				int nextIdx = (i + 1) % len;
				FP cross = Cross(vcs[idx] - pos, vcs[nextIdx] - pos);
				int v = TSMath.Sign(cross);
				if (preV != 0 && v != preV )
					return false;
				preV = v;
			}
			return true;
		}


		public static bool CheckWinding(TSVector2[] vcs, bool clockwise)
        {
            int len = vcs.Length;
            for (int i = 1; i < len; i++)
            {
                int idx = i % len;
                int nIdx = (i + 1) % len;
                int nnIdx = (i + 2) % len;

                FP cross = Cross(vcs[nnIdx] - vcs[nIdx], vcs[idx] - vcs[nIdx]);
                if (clockwise && cross > FP.EN8 || !clockwise && cross < -FP.EN8)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CheckWinding(List<TSVector2> vcs, bool clockwise)
        {
            int len = vcs.Count;
            for (int i = 1; i < len; i++)
            {
                int idx = i % len;
                int nIdx = (i + 1) % len;
                int nnIdx = (i + 2) % len;

                FP cross = Cross(vcs[nnIdx] - vcs[nIdx], vcs[idx] - vcs[nIdx]);
                if (clockwise && cross > FP.EN8 || !clockwise && cross < -FP.EN8)
                {
                    return false;
                }
            }
            return true;
        }

        public static List<TSVector2[]> NormalWinding(List<TSVector2[]> list, bool clockwise)
        {
            for (int i = 1; i < list.Count; i++)
            {
                list[i] = TSVector2.NormalWinding(list[i], true);
            }
            return list;
        }

        public static TSVector2[] NormalWinding(TSVector2[] vcs,bool clockwise)
        {
            if (!CheckWinding(vcs,clockwise))
            {
                Array.Reverse(vcs);
            }
            return vcs;
        }

        public static List<TSVector2> NormalWinding(List<TSVector2>  vcs, bool clockwise)
        {
            if (!CheckWinding(vcs, clockwise))
            {
                vcs.Reverse();
            }
            return vcs;
        }

        public TSVector3 ToTSVector() {
            return new TSVector3(this.x, this.y, FP.Zero);
        }

        public override string ToString() {
            return string.Format("({0:f1}, {1:f1})", x.AsFloat(), y.AsFloat());
        }


        public static TSVector2 operator -(TSVector2 value)
        {
            value.x = -value.x;
            value.y = -value.y;
            return value;
        }


        public static bool operator ==(TSVector2 value1, TSVector2 value2)
        {
            return value1.x == value2.x && value1.y == value2.y;
        }


        public static bool operator !=(TSVector2 value1, TSVector2 value2)
        {
            return value1.x != value2.x || value1.y != value2.y;
        }


        public static TSVector2 operator +(TSVector2 value1, TSVector2 value2)
        {
            value1.x += value2.x;
            value1.y += value2.y;
            return value1;
        }


        public static TSVector2 operator -(TSVector2 value1, TSVector2 value2)
        {
            value1.x -= value2.x;
            value1.y -= value2.y;
            return value1;
        }


        public static FP operator *(TSVector2 value1, TSVector2 value2)
        {
            return TSVector2.Dot(value1, value2);
        }


        public static TSVector2 operator *(TSVector2 value, FP scaleFactor)
        {
            value.x *= scaleFactor;
            value.y *= scaleFactor;
            return value;
        }


        public static TSVector2 operator *(FP scaleFactor, TSVector2 value)
        {
            value.x *= scaleFactor;
            value.y *= scaleFactor;
            return value;
        }


        public static TSVector2 operator /(TSVector2 value1, TSVector2 value2)
        {
            value1.x /= value2.x;
            value1.y /= value2.y;
            return value1;
        }


        public static TSVector2 operator /(TSVector2 value1, FP divider)
        {
            FP factor = 1/divider;
            value1.x *= factor;
            value1.y *= factor;
            return value1;
        }

        public Point ToPoint()
        {
            return new Point((int)x, (int)y);
        }

        public TSVector3 ToTSVector3XZ()
        {
            return new TSVector3(x, 0, y);
        }

		public TSVector3 ToTSVector3XZ(FP height)
		{
			return new TSVector3(x, height, y);
		}

        public static implicit operator Vector2(TSVector2 value)
        {
            return new Vector2(value.x, value.y);
        }
        public static explicit operator TSVector2(Vector2 value)
        {
            return new TSVector2((FP)value.x, (FP)value.y);
        }
        public static implicit operator TSVector2(Vector2Int value)
        {
            return new TSVector2(value.x, value.y);
        }
    }
}
