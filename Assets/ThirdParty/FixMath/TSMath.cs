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
using System.Collections.Generic;

namespace FixMath {

    /// <summary>
    /// Contains common math operations.
    /// </summary>
    public sealed class TSMath {
		public static FP SquareRootTwo = TSMath.Sqrt(2);
		public static FP PI = FP.Pi;
        /// <summary>
        /// PI constant.
        /// </summary>
        public static FP Pi = FP.Pi;

        /**
        *  @brief PI over 2 constant.
        **/
        public static FP PiOver2 = FP.PiOver2;

        /// <summary>
        /// A small value often used to decide if numeric
        /// results are zero.
        /// </summary>
		public static FP Epsilon = FP.Epsilon;

        /**
        *  @brief Degree to radians constant.
        **/
        public static FP Deg2Rad = FP.Deg2Rad;

        /**
        *  @brief Radians to degree constant.
        **/
        public static FP Rad2Deg = FP.Rad2Deg;

        /// <summary>
        /// Gets the square root.
        /// </summary>
        /// <param name="number">The number to get the square root from.</param>
        /// <returns></returns>
        #region public static FP Sqrt(FP number)
        public static FP Sqrt(FP number) {
            return FP.Sqrt(number);
        }
        #endregion

        /// <summary>
        /// Gets the maximum number of two values.
        /// </summary>
        /// <param name="val1">The first value.</param>
        /// <param name="val2">The second value.</param>
        /// <returns>Returns the largest value.</returns>
        #region public static FP Max(FP val1, FP val2)
        public static FP Max(FP val1, FP val2) {
            return (val1 > val2) ? val1 : val2;
        }
        #endregion

        /// <summary>
        /// Gets the minimum number of two values.
        /// </summary>
        /// <param name="val1">The first value.</param>
        /// <param name="val2">The second value.</param>
        /// <returns>Returns the smallest value.</returns>
        #region public static FP Min(FP val1, FP val2)
        public static FP Min(FP val1, FP val2) {
            return (val1 < val2) ? val1 : val2;
        }
        #endregion

        /// <summary>
        /// Gets the maximum number of three values.
        /// </summary>
        /// <param name="val1">The first value.</param>
        /// <param name="val2">The second value.</param>
        /// <param name="val3">The third value.</param>
        /// <returns>Returns the largest value.</returns>
        #region public static FP Max(FP val1, FP val2,FP val3)
        public static FP Max(FP val1, FP val2, FP val3) {
            FP max12 = (val1 > val2) ? val1 : val2;
            return (max12 > val3) ? max12 : val3;
        }
        #endregion

        /// <summary>
        /// Returns a number which is within [min,max]
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        #region public static FP Clamp(FP value, FP min, FP max)
        public static FP Clamp(FP value, FP min, FP max) {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }
        #endregion

        /// <summary>
        /// Returns a number which is within [min,max]
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        #region public static FP Clamp01(FP value)
        public static FP Clamp01(FP value) {
            if(value < FP.Zero)
                return FP.Zero;
            return value > FP.One ? FP.One : value;
        }
        #endregion

        /// <summary>
        /// Changes every sign of the matrix entry to '+'
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="result">The absolute matrix.</param>
        #region public static void Absolute(ref JMatrix matrix,out JMatrix result)
        public static void Absolute(ref TSMatrix matrix, out TSMatrix result) {
            result.M11 = FP.Abs(matrix.M11);
            result.M12 = FP.Abs(matrix.M12);
            result.M13 = FP.Abs(matrix.M13);
            result.M21 = FP.Abs(matrix.M21);
            result.M22 = FP.Abs(matrix.M22);
            result.M23 = FP.Abs(matrix.M23);
            result.M31 = FP.Abs(matrix.M31);
            result.M32 = FP.Abs(matrix.M32);
            result.M33 = FP.Abs(matrix.M33);
        }
        #endregion

        /// <summary>
        /// Returns the sine of value.
        /// </summary>
        public static FP Sin(FP value) {
            return FP.Sin(value);
        }

        /// <summary>
        /// Returns the cosine of value.
        /// </summary>
        public static FP Cos(FP value) {
            return FP.Cos(value);
        }

        /// <summary>
        /// Returns the tan of value.
        /// </summary>
        public static FP Tan(FP value) {
            return FP.Tan(value);
        }

        /// <summary>
        /// Returns the arc sine of value.
        /// </summary>
        public static FP Asin(FP value) {
            return FP.Asin(value);
        }

        /// <summary>
        /// Returns the arc cosine of value.
        /// </summary>
        public static FP Acos(FP value) {
            return FP.Acos(value);
        }

        /// <summary>
        /// Returns the arc tan of value.
        /// </summary>
        public static FP Atan(FP value) {
            return FP.Atan(value);
        }

        /// <summary>
        /// Returns the arc tan of coordinates x-y.
        /// </summary>
        public static FP Atan2(FP y, FP x) {
            return FP.Atan2(y, x);
        }

        /// <summary>
        /// Returns the largest integer less than or equal to the specified number.
        /// </summary>
        public static FP Floor(FP value) {
            return FP.Floor(value);
        }

        /// <summary>
        /// Returns the smallest integral value that is greater than or equal to the specified number.
        /// </summary>
        public static FP Ceiling(FP value) {
            return FP.Ceiling(value);
        }

        /// <summary>
        /// Rounds a value to the nearest integral value.
        /// If the value is halfway between an even and an uneven value, returns the even value.
        /// </summary>
        public static FP Round(FP value) {
            return FP.Round(value);
        }

        /// <summary>
        /// Returns a number indicating the sign of a Fix64 number.
        /// Returns 1 if the value is positive, 0 if is 0, and -1 if it is negative.
        /// </summary>
        public static int Sign(FP value) {
            return FP.Sign(value);
        }

        /// <summary>
        /// Returns a specified number raised to the specified power.
        /// Provides about 5 digits of accuracy for the result.
        /// </summary>
        public static FP Pow(FP f, FP p)
        {
            return FP.Pow(f, p);
        }

        /// <summary>
        /// Returns 2 raised to the specified power.
        /// Provides at least 6 decimals of accuracy.
        /// </summary>
        public static FP Pow2(FP f)
        {
            return FP.Pow2(f);
        }

        /// <summary>
        /// Returns the base-2 logarithm of a specified number.
        /// Provides at least 9 decimals of accuracy.
        /// </summary>
        public static FP Log(FP f)
        {
            return FP.Log(f);
        }

        /// <summary>
        /// Returns the natural logarithm of a specified number.
        /// Provides at least 7 decimals of accuracy.
        /// </summary>
        public static FP Ln(FP value) {
            return FP.Ln(value);
        }

        public static FP Abs(FP value) {
            return FP.Abs(value);
        }

		public static int Abs(int value)
		{
			return System.Math.Abs(value);
		}

		public static FP Barycentric(FP value1, FP value2, FP value3, FP amount1, FP amount2) {
            return value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2;
        }

        public static FP CatmullRom(FP value1, FP value2, FP value3, FP value4, FP amount) {
            // Using formula from http://www.mvps.org/directx/articles/catmull/
            // Internally using FPs not to lose precission
            FP amountSquared = amount * amount;
            FP amountCubed = amountSquared * amount;
            return (FP)(0.5 * (2.0 * value2 +
                                 (value3 - value1) * amount +
                                 (2.0 * value1 - 5.0 * value2 + 4.0 * value3 - value4) * amountSquared +
                                 (3.0 * value2 - value1 - 3.0 * value3 + value4) * amountCubed));
        }

        public static FP Distance(FP value1, FP value2) {
            return FP.Abs(value1 - value2);
        }

        public static FP Hermite(FP value1, FP tangent1, FP value2, FP tangent2, FP amount) {
            // All transformed to FP not to lose precission
            // Otherwise, for high numbers of param:amount the result is NaN instead of Infinity
            FP v1 = value1, v2 = value2, t1 = tangent1, t2 = tangent2, s = amount, result;
            FP sCubed = s * s * s;
            FP sSquared = s * s;

            if (amount == 0f)
                result = value1;
            else if (amount == 1f)
                result = value2;
            else
                result = (2 * v1 - 2 * v2 + t2 + t1) * sCubed +
                         (3 * v2 - 3 * v1 - 2 * t1 - t2) * sSquared +
                         t1 * s +
                         v1;
            return (FP)result;
        }

        public static FP Lerp(FP value1, FP value2, FP amount) {
            return value1 + (value2 - value1) * amount;
        }

        public static FP SmoothStep(FP value1, FP value2, FP amount) {
            // It is expected that 0 < amount < 1
            // If amount < 0, return value1
            // If amount > 1, return value2
            FP result = Clamp(amount, 0, 1);
            result = Hermite(value1, 0, value2, 0, result);
            return result;
        }

        public static int CeilToInt(FP fp) {
            return Ceiling(fp).AsInt();
        }

        public static int RoundToInt(FP fp) {
            return Round(fp).AsInt();
        }

        public static int FloorToInt(FP fp)
        {
            return Floor(fp).AsInt();
        }

        public static FP Round(FP fp,int p) {
            return Round(fp);
        }

        public static FP Infinity {
            get { return FP.PositiveInfinity; }
        }

        public static FP Min(FP a,FP b ,FP c ,FP d)
        {
            return Min(Min(a,b), Min(c,d));
        }

		public static int Clamp(int value, int min, int max)
		{
			if (value <= min)
			{
				return min;
			}
			else if (value >= max) {
				return max;
			}
			return value;
        }

		public static FP MoveTowards(FP from, FP to, FP maxDelta)
		{
			if (Abs(to - from) <= maxDelta)
			{
				return to;
			}
			return from + Sign(to - from) * maxDelta;
		}

		public static int Max(params int[] values) {
			if (values == null)
				throw new System.Exception("values == null");
			int val = int.MinValue;
			for (int i = 0; i < values.Length;i++) {
				val = val > values[i] ? val : values[i];
			}
			return val;
        }

		public static FP Max(params FP[] values)
		{
			if (values == null)
				throw new System.Exception("values == null");
			FP val = FP.MinValue;
			for (int i = 0; i < values.Length; i++)
			{
				val = val > values[i] ? val : values[i];
			}
			return val;
		}

		public static int Min(params int[] values)
        {
			if (values == null)
				throw new System.Exception("values == null");
			int val = int.MaxValue;
			for (int i = 0; i < values.Length; i++)
			{
				val = val < values[i] ? val : values[i];
			}
			return val;
		}

        //标准差
		public static FP StdDeviation(List<FP> values)
		{
			return TSMath.Sqrt(Deviation(values));
		}

		//方差
		public static FP Deviation(List<FP> values)
		{
			if (values.Count <= 0)
				return 0;

			FP avg = 0;
			foreach (var v in values)
			{
				avg += v;
			}
			avg = avg / values.Count;
			FP sum = 0;
			foreach (var v in values)
			{
				sum += (v - avg)* (v - avg);
			}
			return sum / values.Count;
		}

		//方差
		public static FP Average(List<FP> values)
		{
			if (values.Count <= 0)
				return 0;

			FP avg = 0;
			foreach (var v in values)
			{
				avg += v;
			}
			avg = avg / values.Count;
			return avg;
		}

		// [-PI,PI)
		public static FP NormalizeYaw(FP yaw)
		{
			while (yaw <= -FP.Pi)
				yaw += FP.PiTimes2;
			while (yaw > FP.Pi)
				yaw -= FP.PiTimes2;

			return yaw;
		}
	}
}
