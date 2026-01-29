// using ProtoBuf;
// using System;
// using System.IO;
// using System.Runtime.CompilerServices;
//
// namespace FixMath
// {
//     [ProtoContract]
//     public partial struct FP
//     {
//         [ProtoMember(1)]
//         private long SerializeRawValue
//         {
//             get { return _serializedValue; }
//             set { _serializedValue = value;}
//         }
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static bool TryParse(string text, out FP result)
// 		{
// 			return TryParse(text, out result, false);
// 		}
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static bool TryParse(string text, out FP result, bool checkPercent)
// 		{
// 			result = new FP();
//
// 			if (string.IsNullOrEmpty(text))
// 				return false;
//
// 			bool percent = false;
// 			if (checkPercent)
// 			{
// 				text = text.Trim();
// 				if (string.IsNullOrEmpty(text))
// 					return false;
//
// 				int last = text.Length - 1;
// 				if (text[last] == '%')
// 				{
// 					text = text.Substring(0, last);
// 					percent = true;
// 				}
// 			}
//
// 			try
// 			{
// 				result = (FP)text;
//
// 				if (percent)
// 					result /= FP.Hundred;
//
// 				return true;
// 			}
// 			catch (Exception)
// 			{ }
//
// 			return false;
// 		}
// 	}
//
// 	[ProtoContract]
// 	public partial struct TSVector2
// 	{
// 		[ProtoMember(1)]
// 		private FP SerializeX
// 		{
// 			get { return x; }
// 			set { x = value; }
// 		}
//
// 		[ProtoMember(2)]
// 		private FP SerializeY
// 		{
// 			get { return y; }
// 			set { y = value; }
// 		}
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static bool TryParse(string text, out TSVector2 result)
// 		{
// 			result = new TSVector2();
//
// 			if (string.IsNullOrEmpty(text))
// 				return false;
//
// 			var array = text.Split(FixMathSerialization.DefaultSeparators, 2);
// 			if (array == null || array.Length < 2)
// 				return false;
//
// 			result.Set((FP)array[0], (FP)array[1]);
// 			return true;
// 		}
// 	}
//
// 	[ProtoContract]
// 	public partial struct TSVector3
// 	{
// 		[ProtoMember(1)]
// 		private FP SerializeX
// 		{
// 			get { return x; }
// 			set { x = value; }
// 		}
//
// 		[ProtoMember(2)]
// 		private FP SerializeY
// 		{
// 			get { return y; }
// 			set { y = value; }
// 		}
//
// 		[ProtoMember(3)]
// 		private FP SerializeZ
// 		{
// 			get { return z; }
// 			set { z = value; }
// 		}
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static bool TryParse(string text, out TSVector3 result)
// 		{
// 			result = new TSVector3();
//
// 			if (string.IsNullOrEmpty(text))
// 				return false;
//
// 			var array = text.Split(FixMathSerialization.DefaultSeparators, 3);
// 			if (array == null || array.Length < 3)
// 				return false;
//
// 			result.Set((FP)array[0], (FP)array[1], (FP)array[2]);
// 			return true;
// 		}
// 	}
//
// 	[ProtoContract]
// 	public partial struct TSQuaternion2D
// 	{
// 		[ProtoMember(1)]
// 		private FP SerializeyawRad
// 		{
// 			get { return yawRad; }
// 			set { yawRad = value; }
// 		}
// 	}
//
// 	public static class FixMathSerialization
// 	{
// 		internal static readonly char[] DefaultSeparators = new char[] { ',' };
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static FP ReadFP(this BinaryReader reader) => new FP { _serializedValue = reader.ReadInt64() };
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static TSVector2 ReadTSVector2(this BinaryReader reader) => new TSVector2(reader.ReadFP(), reader.ReadFP());
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static TSVector3 ReadTSVector3(this BinaryReader reader) => new TSVector3(reader.ReadFP(), reader.ReadFP(), reader.ReadFP());
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static void WriteFP(this BinaryWriter writer, FP value)
// 		{
// 			writer.Write(value._serializedValue);
// 		}
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static void WriteTSVector2(this BinaryWriter writer, TSVector2 value)
// 		{
// 			writer.WriteFP(value.x);
// 			writer.WriteFP(value.y);
// 		}
//
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		public static void WriteTSVector3(this BinaryWriter writer, TSVector3 value)
// 		{
// 			writer.WriteFP(value.x);
// 			writer.WriteFP(value.y);
// 			writer.WriteFP(value.z);
// 		}
// 	}
// }
//
