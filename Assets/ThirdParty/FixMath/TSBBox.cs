using System.Collections;

namespace FixMath
{

	[System.Serializable]
	// [ProtoBuf.ProtoContract]
	public struct TSBBox
    {
        //Box三种情况 Point 两种情况Disjoint/Contains
        public enum ContainmentType
        {
            Disjoint,

            Contains,

            Intersects
        }

		// [ProtoBuf.ProtoMember(1)]
        public TSVector3 min;

		// [ProtoBuf.ProtoMember(2)]
		public TSVector3 max;


        public static readonly TSBBox LargeBox;


        public static readonly TSBBox SmallBox;

        static TSBBox()
        {
            LargeBox.min = new TSVector3(FP.MinValue);
            LargeBox.max = new TSVector3(FP.MaxValue);
            SmallBox.min = new TSVector3(FP.MaxValue);
            SmallBox.max = new TSVector3(FP.MinValue);
        }


        public TSBBox(TSVector3 center, TSVector3 size)
        {
            this.min = center - size/2;
            this.max = center + size /2;
        }

        internal void InverseTransform(ref TSVector3 position, ref TSMatrix orientation)
        {
            TSVector3.Subtract(ref max, ref position, out max);
            TSVector3.Subtract(ref min, ref position, out min);

            TSVector3 center;
            TSVector3.Add(ref max, ref min, out center);
            center.x *= FP.Half; center.y *= FP.Half; center.z *= FP.Half;

            TSVector3 halfExtents;
            TSVector3.Subtract(ref max, ref min, out halfExtents);
            halfExtents.x *= FP.Half; halfExtents.y *= FP.Half; halfExtents.z *= FP.Half;

            TSVector3.TransposedTransform(ref center, ref orientation, out center);

            TSMatrix abs; TSMath.Absolute(ref orientation, out abs);
            TSVector3.TransposedTransform(ref halfExtents, ref abs, out halfExtents);

            TSVector3.Add(ref center, ref halfExtents, out max);
            TSVector3.Subtract(ref center, ref halfExtents, out min);
        }

        public void Transform(ref TSMatrix orientation)
        {
            TSVector3 halfExtents = FP.Half * (max - min);
            TSVector3 center = FP.Half * (max + min);

            TSVector3.Transform(ref center, ref orientation, out center);

            TSMatrix abs; TSMath.Absolute(ref orientation, out abs);
            TSVector3.Transform(ref halfExtents, ref abs, out halfExtents);

            max = center + halfExtents;
            min = center - halfExtents;
        }



        private bool Intersect1D(FP start, FP dir, FP min, FP max,
            ref FP enter, ref FP exit)
        {
            if (dir * dir < TSMath.Epsilon * TSMath.Epsilon) return (start >= min && start <= max);

            FP t0 = (min - start) / dir;
            FP t1 = (max - start) / dir;

            if (t0 > t1) { FP tmp = t0; t0 = t1; t1 = tmp; }

            if (t0 > exit || t1 < enter) return false;

            if (t0 > enter) enter = t0;
            if (t1 < exit) exit = t1;
            return true;
        }


        public bool SegmentIntersect(ref TSVector3 origin, ref TSVector3 direction)
        {
            FP enter = FP.Zero, exit = FP.One;

            if (!Intersect1D(origin.x, direction.x, min.x, max.x, ref enter, ref exit))
                return false;

            if (!Intersect1D(origin.y, direction.y, min.y, max.y, ref enter, ref exit))
                return false;

            if (!Intersect1D(origin.z, direction.z, min.z, max.z, ref enter, ref exit))
                return false;

            return true;
        }

		public bool LineIntersectRadius(ref TSVector3 start, ref TSVector3 dir,  ref TSVector3 r,FP length)
		{
			FP enter = FP.Zero, exit = FP.MaxValue;

			if (!Intersect1D(start.x, dir.x, min.x - r.x , max.x + r.x, ref enter, ref exit))
				return false;

			if (!Intersect1D(start.y, dir.y, min.y, max.y, ref enter, ref exit))
				return false;

			if (!Intersect1D(start.z, dir.z, min.z - r.z, max.z + r.z, ref enter, ref exit))
				return false;

			return !(enter > length || exit < 0);
		}

		public bool LineIntersectRadius(ref TSVector3 start, ref TSVector3 dir, ref TSVector3 r, ref FP enter, FP length)
		{
			enter = FP.Zero;
			FP exit = FP.MaxValue;

			if (!Intersect1D(start.x, dir.x, min.x - r.x, max.x + r.x, ref enter, ref exit))
				return false;

			if (!Intersect1D(start.y, dir.y, min.y, max.y, ref enter, ref exit))
				return false;

			if (!Intersect1D(start.z, dir.z, min.z - r.z, max.z + r.z, ref enter, ref exit))
				return false;

			return !(enter > length || exit < 0);
		}

		public bool RayIntersect(ref TSVector3 origin, ref TSVector3 direction)
        {
            FP enter = FP.Zero, exit = FP.MaxValue;

            if (!Intersect1D(origin.x, direction.x, min.x, max.x, ref enter, ref exit))
                return false;

            if (!Intersect1D(origin.y, direction.y, min.y, max.y, ref enter, ref exit))
                return false;

            if (!Intersect1D(origin.z, direction.z, min.z, max.z, ref enter, ref exit))
                return false;

            return true;
        }

        public bool SegmentIntersect(TSVector3 origin, TSVector3 direction)
        {
            return SegmentIntersect(ref origin, ref direction);
        }

        public bool RayIntersect(TSVector3 origin, TSVector3 direction)
        {
            return RayIntersect(ref origin, ref direction);
        }

        public ContainmentType ContainsBase(TSVector3 point)
        {
            return this.Contains(ref point);
        }

        public bool Contains(TSVector3 point)
        {
            return this.Contains(ref point) != ContainmentType.Disjoint;
        }


        public ContainmentType Contains(ref TSVector3 point)
        {
            return ((((this.min.x <= point.x) && (point.x <= this.max.x)) &&
                ((this.min.y <= point.y) && (point.y <= this.max.y))) &&
                ((this.min.z <= point.z) && (point.z <= this.max.z))) ? ContainmentType.Contains : ContainmentType.Disjoint;
        }

        public TSVector3 ClosestPoint(TSVector3 v)
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
                    v.y = min.y ;
                else if (v.y > max.y)
                    v.y = max.y;

                if (v.z < min.z)
                    v.z = min.z;
                else if (v.z > max.z)
                    v.z = max.z;

               return v;
             }
        }

        public void GetCorners(TSVector3[] corners)
        {
            corners[0].Set(this.min.x, this.max.y, this.max.z);
            corners[1].Set(this.max.x, this.max.y, this.max.z);
            corners[2].Set(this.max.x, this.min.y, this.max.z);
            corners[3].Set(this.min.x, this.min.y, this.max.z);
            corners[4].Set(this.min.x, this.max.y, this.min.z);
            corners[5].Set(this.max.x, this.max.y, this.min.z);
            corners[6].Set(this.max.x, this.min.y, this.min.z);
            corners[7].Set(this.min.x, this.min.y, this.min.z);
        }

		public void GetCorners(TSVector2[] corners)
		{
			corners[0].Set(this.min.x, this.min.z);
			corners[1].Set(this.min.x, this.max.z);
			corners[2].Set(this.max.x, this.max.z);
			corners[3].Set(this.max.x, this.min.z);
		}


		public void AddPoint(TSVector3 point)
        {
            AddPoint(ref point);
        }

        public void AddPoint(ref TSVector3 point)
        {
            TSVector3.Max(ref this.max, ref point, out this.max);
            TSVector3.Min(ref this.min, ref point, out this.min);
        }



        public static TSBBox CreateFromPoints(TSVector3[] points)
        {
            TSVector3 vector3 = new TSVector3(FP.MaxValue);
            TSVector3 vector2 = new TSVector3(FP.MinValue);

            for (int i = 0; i < points.Length; i++)
            {
                TSVector3.Min(ref vector3, ref points[i], out vector3);
                TSVector3.Max(ref vector2, ref points[i], out vector2);
            }
            var box = new TSBBox();
            box.SetMinMax(vector3,vector2);
            return box;
        }

        public ContainmentType Contains(TSBBox box)
        {
            return this.Contains(ref box);
        }

        public bool Intersects(TSBBox box)
        {
            return this.Intersects(ref box);
        }

        public bool Intersects(ref TSBBox box)
        {
            return this.max.x >= box.min.x && this.min.x <= box.max.x && this.max.y >= box.min.y && this.min.y <= box.max.y && this.max.z >= box.min.z && this.min.z <= box.max.z;
        }

        public bool Overlaps(TSBBox box2)
        {
            return this.Intersects(ref box2);
        }

        public ContainmentType Contains(ref TSBBox box)
        {
            ContainmentType result = ContainmentType.Disjoint;
            if (Intersects(ref box))
            {
                result = this.min.x <= box.min.x && box.max.x <= this.max.x && this.min.y <= box.min.y && box.max.y <= this.max.y && this.min.z <= box.min.z && box.max.z <= this.max.z
                    ? ContainmentType.Contains : ContainmentType.Intersects;
            }
            return result;
        }


        public static TSBBox CreateFromCenter(TSVector3 center, TSVector3 size) {
            TSVector3 half = size * FP.Half;
            var box = new TSBBox();
            box.SetMinMax(center - half, center + half);
            return box;
        }


        public static TSBBox CreateMerged(TSBBox original, TSBBox additional)
        {
            TSBBox result;
            TSBBox.CreateMerged(ref original, ref additional, out result);
            return result;
        }

        public static void CreateMerged(ref TSBBox original, ref TSBBox additional, out TSBBox result)
        {
            TSVector3 vector;
            TSVector3 vector2;
            TSVector3.Min(ref original.min, ref additional.min, out vector2);
            TSVector3.Max(ref original.max, ref additional.max, out vector);
            result.min = vector2;
            result.max = vector;
        }

        public TSVector3 center {
            get {
                return (min + max) * (FP.Half);
            }
            set {
                TSVector3 halfSize = size* FP.Half;
                this.max = value + halfSize;
                this.min = value - halfSize;
            }
        }

        public TSVector3 size {
            get {
                return (max - min);
            }
            set {
                TSVector3 tempCenter = center;
                this.max = tempCenter + value * FP.Half;
                this.min = tempCenter - value * FP.Half;
            }
        }

        public TSVector3 extents {
            get {
                return size * FP.Half;
            }
        }

        internal FP Perimeter
        {
            get
            {
                return (2 * FP.One) * ((max.x - min.x) * (max.y - min.y) +
                    (max.x - min.x) * (max.z - min.z) +
                    (max.z - min.z) * (max.y - min.y));
            }
        }

        public override string ToString() {
            return string.Format("Center: ({0}), Extents: ({1})",center,extents) ;
        }

        public void SetMinMax(TSVector3 min ,TSVector3 max)
        {
            this.min = min;
            this.max = max;
        }

        public void Expand(FP fp)
        {
            TSVector3 halfSize = (size + TSVector3.one * fp) * FP.Half;
            TSVector3 tmpCenter = center;
            this.max = tmpCenter + halfSize;
            this.min = tmpCenter - halfSize;
        }

        public System.Collections.Generic.IEnumerable<TSVector3> GetPointsOfY(FP y)
		{
			var min = this.min;
			var max = this.max;
			yield return new TSVector3(min.x, y, max.z);
			yield return new TSVector3(max.x, y, max.z);
			yield return new TSVector3(max.x, y, min.z);
			yield return new TSVector3(min.x, y, min.z);
		}

		public System.Collections.Generic.IEnumerable<TSVector3> GetPoints(TSVector3 vc)
        {
			var min = this.min;
			var max = this.max;

			if (vc.y > min.y && vc.y < max.y)
			{
				yield return new TSVector3(min.x, vc.y, max.z);
				yield return new TSVector3(max.x, vc.y, max.z);
				yield return new TSVector3(max.x, vc.y, min.z);
				yield return new TSVector3(min.x, vc.y, min.z);
			}


			yield return new TSVector3(min.x, max.y, max.z);
			yield return new TSVector3(max.x, max.y, max.z);
			yield return new TSVector3(max.x, min.y, max.z);
			yield return new TSVector3(min.x, min.y, max.z);
			yield return new TSVector3(min.x, max.y, min.z);
			yield return new TSVector3(max.x, max.y, min.z);
			yield return new TSVector3(max.x, min.y, min.z);
			yield return new TSVector3(min.x, min.y, min.z);
		}
	}


}
