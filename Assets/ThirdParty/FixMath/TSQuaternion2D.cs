using System;

namespace FixMath
{
    [System.Serializable]
    public partial struct TSQuaternion2D : IEquatable<TSQuaternion2D>
    {
        static FP Pi = FP.Pi;
        static FP Pi2 = FP.Pi*2;
        static FP Rad2Deg = FP.Rad2Deg;
        static FP Deg2Rad = FP.Deg2Rad;
        public static readonly TSQuaternion2D identity;

        public FP yawRad;

        void Init(FP x,FP z)
        {
            this.yawRad = FP.Atan2(x, z);
        }

        void Init(FP yawRad)
        {
            yawRad %= Pi2;
            if (yawRad > Pi)
                yawRad = -(Pi2 - yawRad);
            if (yawRad < -Pi)
                yawRad = Pi2 + yawRad;
            this.yawRad = yawRad;
        }

        public TSVector3 eulerAngles
        {
            get
            {
                return new TSVector3(0,yawRad*Rad2Deg,0);
            }
        }

		public static TSQuaternion2D EulerRad(FP yawRad)
		{
			TSQuaternion2D quat = new TSQuaternion2D();
			quat.Init(yawRad);
			return quat;
		}

		public static TSQuaternion2D Euler(FP  angle)
        {
            TSQuaternion2D quat = new TSQuaternion2D();
            quat.Init(angle*Deg2Rad);
            return quat;
        }

        public static FP Angle(TSQuaternion2D a, TSQuaternion2D b)
        {
            var yaw = (a.yawRad - b.yawRad) ;
            if (yaw > Pi)
                yaw = -(Pi2 - yaw);
            if (yaw < -Pi)
                yaw = Pi2 + yaw;

            if (yaw < 0)
                yaw = -yaw;
            return  yaw * Rad2Deg;
        }

        public static TSQuaternion2D LookRotation(TSVector3 forward)
        {
            var newQ = new TSQuaternion2D();
            newQ.Init(forward.x, forward.z);
            return newQ;
        }

        public static TSQuaternion2D FromToRotation(TSVector3 fromVector, TSVector3 toVector)
        {
            fromVector.y = 0;
            toVector.y = 0;
            var rad = FP.Acos(fromVector.normalized * toVector.normalized);
            var vec = TSVector3.Cross(fromVector, toVector);
            var newQ = new TSQuaternion2D();
            newQ.Init(vec.z > 0 ? rad : -rad);
            return newQ;
        }

        public static TSQuaternion2D RotateTowards(TSQuaternion2D from, TSQuaternion2D to, FP maxDegreesDelta)
        {
            var toYawRad = to.yawRad;
            while(toYawRad < 0)
                 toYawRad = Pi2+ toYawRad;

            var delta = toYawRad - from.yawRad;

            bool isForward = delta > 0;
            delta = TSMath.Abs(delta);
            if (delta > Pi)
            {
                isForward = !isForward;
                delta = Pi2 - delta;
            }

            maxDegreesDelta *= FP.Deg2Rad;

            if (maxDegreesDelta >= delta)
            {
                return to;
            }

            var newQ = new TSQuaternion2D();
            newQ.Init(from.yawRad + (isForward ? maxDegreesDelta : -maxDegreesDelta));
            return newQ;
        }

        public static implicit operator TSQuaternion2D(TSQuaternion quaternion)
        {
            var vc = quaternion * TSVector3.forward;
            var x = vc.x;
            var z = vc.z;
            if (x == FP.Zero && z == FP.Zero)
                return identity;

            if (FP.Abs(z) < FP.EN6)
                z = FP.Zero;

            var newQ = new TSQuaternion2D();
            newQ.Init(x, z);
            return newQ;
        }

        public static implicit operator TSQuaternion(TSQuaternion2D q)
        {
            return TSQuaternion.Euler(0, q.yawRad * Rad2Deg, 0);
        }

		public static implicit operator UnityEngine.Quaternion(TSQuaternion2D q)
		{
			return UnityEngine.Quaternion.Euler(0, (q.yawRad * Rad2Deg).AsFloat(), 0);
		}

		public static explicit operator TSQuaternion2D(UnityEngine.Quaternion quaternion)
		{
			var vc = quaternion * TSVector3.forward;
			var x = vc.x;
			var z = vc.z;
			if (x == 0 && z == 0 )
				return identity;

			if (FP.Abs((FP)z) < FP.EN6)
				z = 0;

			var newQ = new TSQuaternion2D();
			newQ.Init((FP)x, (FP)z);
			return newQ;
		}

		public static TSQuaternion2D operator *(TSQuaternion2D lhs, TSQuaternion2D rhs)
        {
            var newQ = new TSQuaternion2D();
            newQ.Init(lhs.yawRad + rhs.yawRad);
            return newQ;
        }

        public static TSVector3 operator *(TSQuaternion2D quat, TSVector3 vec)
        {
            var sin = TSMath.Sin(quat.yawRad);
            var cos = TSMath.Cos(quat.yawRad);
            FP newx = vec.x * cos + vec.z * sin;
            FP newz = -vec.x * sin + vec.z * cos;
            vec.x = newx;
            vec.z = newz;
            return vec;
        }

        public static TSVector2 operator *(TSQuaternion2D quat, TSVector2 vec)
        {
            var sin = TSMath.Sin(quat.yawRad);
            var cos = TSMath.Cos(quat.yawRad);
            FP newx = vec.x * cos + vec.y * sin;
            FP newz = -vec.x * sin + vec.y * cos;
            vec.x = newx;
            vec.y = newz;
            return vec;
        }

        public static bool operator ==(TSQuaternion2D lhs, TSQuaternion2D rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TSQuaternion2D lhs, TSQuaternion2D rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static TSQuaternion2D Inverse(TSQuaternion2D q)
        {
            var newQ = new TSQuaternion2D();
            newQ.Init(-q.yawRad);
            return newQ;
        }

        public bool Equals(TSQuaternion2D other)
        {
            return yawRad == other.yawRad ;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TSQuaternion2D))
                return false;
            return base.Equals((TSQuaternion2D)obj);
        }

        public override int GetHashCode()
        {
            return yawRad.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0:f2}", yawRad.AsFloat());
        }
    }
}
