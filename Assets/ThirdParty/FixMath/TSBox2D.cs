using System.Collections;
using System;

namespace FixMath
{
    [System.Serializable]
    public struct TSBBox2D : IEquatable<TSBBox2D>
    {

        public enum ContainmentType
        {

            Disjoint,

            Contains,

            Intersects
        }


        public TSVector2 min;


        public TSVector2 max;


        public static readonly TSBBox2D LargeBox;


        public static readonly TSBBox2D SmallBox;

        static TSBBox2D()
        {
            LargeBox.min = new TSVector2(FP.MinValue);
            LargeBox.max = new TSVector2(FP.MaxValue);
            SmallBox.min = new TSVector2(FP.MaxValue);
            SmallBox.max = new TSVector2(FP.MinValue);
        }


        public TSBBox2D(TSVector2 center, TSVector2 size)
        {
            this.min = center - size / 2;
            this.max = center + size / 2;
        }


        public ContainmentType ContainsBase(TSVector2 point)
        {
            return this.Contains(ref point);
        }

        public bool Contains(TSVector2 point)
        {
            return this.Contains(ref point) != ContainmentType.Disjoint;
        }


        public ContainmentType Contains(ref TSVector2 point)
        {
            return ((((this.min.x <= point.x) && (point.x <= this.max.x)) &&
                ((this.min.y <= point.y) && (point.y <= this.max.y)))) ? ContainmentType.Contains : ContainmentType.Disjoint;
        }

        public TSVector2 ClosestPoint(TSVector2 v)
        {

            if (Contains(v))
                return v;
            else
            {


                if (v.x < min.x)
                    v.x = min.x;
                else if (v.x > max.x)
                    v.x = max.x;

                if (v.y < min.y)
                    v.y = min.y;
                else if (v.y > max.y)
                    v.y = max.y;

                return v;
            }
        }

        public void GetCorners(TSVector2[] corners)
        {
            corners[0] = this.min;
            corners[1].Set(this.min.x, this.max.y);
            corners[2] = this.max;
            corners[3].Set(this.max.x, this.min.y);
        }


        public void AddPoint(TSVector2 point)
        {
            AddPoint(ref point);
        }

        public void AddPoint(ref TSVector2 point)
        {
            TSVector2.Max(ref this.max, ref point, out this.max);
            TSVector2.Min(ref this.min, ref point, out this.min);
        }



        public static TSBBox2D CreateFromPoints(TSVector2[] points)
        {
            TSVector2 vector3 = new TSVector2(FP.MaxValue);
            TSVector2 vector2 = new TSVector2(FP.MinValue);

            for (int i = 0; i < points.Length; i++)
            {
                TSVector2.Min(ref vector3, ref points[i], out vector3);
                TSVector2.Max(ref vector2, ref points[i], out vector2);
            }
            var box = new TSBBox2D();
            box.SetMinMax(vector3, vector2);
            return box;
        }

        public ContainmentType Contains(TSBBox2D box)
        {
            return this.Contains(ref box);
        }

        public bool Intersects(TSBBox2D box)
        {
            return this.Intersects(ref box);
        }

        public bool Intersects(ref TSBBox2D box)
        {
            return  this.max.x >= box.min.x && this.min.x <= box.max.x && this.max.y >= box.min.y && this.min.y <= box.max.y;
        }

        public bool Overlaps(TSBBox2D box2)
        {
            return this.Intersects(ref box2);
        }

        public ContainmentType Contains(ref TSBBox2D box)
        {
            ContainmentType result = ContainmentType.Disjoint;
            if (Intersects(ref box))
                result = this.min.x <= box.min.x && box.max.x <= this.max.x && this.min.y <= box.min.y && box.max.y <= this.max.y
                    ? ContainmentType.Contains : ContainmentType.Intersects;
            return result;
        }


        public static TSBBox2D CreateFromCenter(TSVector2 center, TSVector2 size)
        {
            TSVector2 half = size * FP.Half;
            var box = new TSBBox2D();
            box.SetMinMax(center - half, center + half);
            return box;
        }


        public static TSBBox2D CreateMerged(TSBBox2D original, TSBBox2D additional)
        {
            TSBBox2D result;
            TSBBox2D.CreateMerged(ref original, ref additional, out result);
            return result;
        }

        public static void CreateMerged(ref TSBBox2D original, ref TSBBox2D additional, out TSBBox2D result)
        {
            TSVector2 vector;
            TSVector2 vector2;
            TSVector2.Min(ref original.min, ref additional.min, out vector2);
            TSVector2.Max(ref original.max, ref additional.max, out vector);
            result.min = vector2;
            result.max = vector;
        }

        public TSVector2 TopCenter
        {
            get
            {
                return new TSVector2(center.x, max.y);
            }
        }

        public TSVector2 BottomCenter
        {
            get
            {
                return new TSVector2(center.x, min.y);
            }
        }

        public TSVector2 center
        {
            get
            {
                return (min + max) * (FP.Half);
            }
            set
            {
                TSVector2 halfSize = size * FP.Half;
                this.max = value + halfSize;
                this.min = value - halfSize;
            }
        }

        public TSVector2 size
        {
            get
            {
                return (max - min);
            }
            set
            {
                TSVector2 tempCenter = center;
                this.max = tempCenter + value * FP.Half;
                this.min = tempCenter - value * FP.Half;
            }
        }

        public TSVector2 extents
        {
            get
            {
                return size * FP.Half;
            }
        }

        public override string ToString()
        {
            return string.Format("Center: ({0}), Extents: ({1})", center, extents);
        }

        public void SetMinMax(TSVector2 min, TSVector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public void SetMinMax(FP minX, FP maxX, FP minZ, FP maxZ)
        {
            this.min = new TSVector2(minX,minZ);
            this.max = new TSVector2(maxX,maxZ);
        }

        public void Expand(FP fp)
        {
            TSVector2 halfSize = (size + TSVector2.one * fp) * FP.Half;
            TSVector2 tmpCenter = center;
            this.max = tmpCenter + halfSize;
            this.min = tmpCenter - halfSize;
        }

		//从BottomLeft开始，顺时针
		public int SegmentRayCast(TSVector2 originPos, TSVector2 dir,out TSVector2 hitPoint)
		{
			hitPoint = originPos;

			TSVector2[] conners = new TSVector2[4];
			GetCorners(conners);
			for (int i = 0; i < conners.Length; i++)
			{
				if (TSVector2.SegmentRayCast(conners[i], conners[(i + 1) % 4], originPos,  dir, out hitPoint))
				{
					return i;
				}
			}
			return -1;
		}

        public void Union(TSVector2 min, TSVector2 max)
        {
            if (this.min.x > min.x)
            {
                this.min.x = min.x;
            }
            if (this.max.x < max.x)
            {
                this.max.x = max.x;
            }
            if (this.min.y > min.y)
            {
                this.min.y = min.y;
            }
            if (this.max.y < max.y)
            {
                this.max.y = max.y;
            }
        }

        public bool Equals(TSBBox2D other)
        {
            return min == other.min && max == other.max;
        }

        public override int GetHashCode()
        {
            return (min.GetHashCode()*397) ^ max.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TSBBox))
                return false;
            return base.Equals((TSBBox)obj);
        }

        public static bool operator ==(TSBBox2D value1, TSBBox2D value2)
        {
            return value1.Equals(value2);
        }


        public static bool operator !=(TSBBox2D value1, TSBBox2D value2)
        {
            return !value1.Equals(value2);
        }
    }
}
