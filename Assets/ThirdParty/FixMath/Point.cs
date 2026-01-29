using System;
using FixMath;

namespace FixMath
{
	public struct Area2D
	{
		public Point min;
		public Point max;

		public Area2D(Point min, Point max) {
			this.min = min;
			this.max = max;
		}

		public Point center { get { return new Point(TSMath.RoundToInt((min.X + max.X) * FP.Half) , TSMath.RoundToInt((min.Y + max.Y) * FP.Half)); } }
	}

    // [ProtoBuf.ProtoContract()]
	[Serializable]
	public struct Point : IEquatable<Point> , IComparable
	{
        public static readonly Point Zero = new Point(0, 0);//定义无穷小数为NULL
        public static readonly Point Empty = new Point(int.MinValue, int.MinValue);//定义无穷小数为NULL

		public Point(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		// [ProtoBuf.ProtoMember(1)]
		public int X;

		// [ProtoBuf.ProtoMember(2)]
		public int Y;

		public override string ToString() => $"({this.X}, {this.Y})";

		public bool Equals(Point other)
		{
			return this.X == other.X && this.Y == other.Y;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			return (obj is Point) && Equals((Point)obj);
		}

		public static bool operator ==(Point a, Point b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Point a, Point b)
		{
			return !a.Equals(b);
		}

		public static Point operator +(Point a, Point b)
		{
			return new Point(a.X + b.X, a.Y + b.Y);
		}

		public static Point operator -(Point a, Point b)
		{
			return new Point(a.X - b.X, a.Y - b.Y);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (this.X * 397) ^ this.Y;
			}
		}

		public int CompareTo(object obj)
		{
			if (obj == null || !(obj is Point))
				return -1;

			var other = (Point)obj;

			int value = GetHashCode().CompareTo(other.GetHashCode());
			if (value != 0)
				return value;

			return Equals(other) ? 0 : -1;
		}

		public int sqrMagnitude
		{
			get { return  X*X + Y*Y; }
		}

		public FP magnitude {
			get { return TSMath.Sqrt(sqrMagnitude) ; }
		}

		public TSVector2 ToTSVector2()
		{
			return new TSVector2(X,Y);
		}

		/*转换不是无损的  容易产生BUG
        public static implicit operator Point(TSVector2 p)
        {
            return new Point((int)p.x, (int)p.y);
        }
        */
	}


    public struct PointByte : IEquatable<PointByte>, IComparable
	{
		public static readonly PointByte Empty = new PointByte(-1, -1, 0);

		public PointByte(int x, int y, byte z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		public PointByte(Point position, byte z)
		{
			this.X = position.X;
			this.Y = position.Y;
			this.Z = z;
		}


		public int X { get; set; }
		public int Y { get; set; }
		public byte Z { get; set; }

		public override string ToString() => $"PositionByte: ({this.X}, {this.Y}, {this.Z})";

		public bool Equals(PointByte other)
		{
			return this.X == other.X && this.Y == other.Y && this.Z == other.Z;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			return (obj is PointByte) && Equals((PointByte)obj);
		}

		public static bool operator ==(PointByte a, PointByte b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(PointByte a, PointByte b)
		{
			return !a.Equals(b);
		}

		public static PointByte operator +(PointByte a, PointByte b)
		{
			return new PointByte(a.X + b.X, a.Y + b.Y, (byte)(a.Z + b.Z));
		}

		public static PointByte operator -(PointByte a, PointByte b)
		{
			return new PointByte(a.X - b.X, a.Y - b.Y, (byte)(a.Z - b.Z));
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (this.X * 397) ^ this.Y ^ this.Z;
			}
		}

		public int CompareTo(object obj)
		{
			if (obj == null || !(obj is PointByte))
				return -1;

			var other = (PointByte)obj;

			int value = GetHashCode().CompareTo(other.GetHashCode());
			if (value != 0)
				return value;

			return Equals(other) ? 0 : -1;
		}
	}
}
