using System;
using System.Runtime.CompilerServices;

namespace FixMath
{
    /// <summary>
    /// 大数定点数实现，专门用于大数值计算，使用Int128 (模拟)存储
    /// </summary>
    public struct BFP : IEquatable<BFP>, IComparable<BFP>
    {
        // 使用两个Int64模拟Int128
        private readonly long _high;
        private readonly ulong _low;

        // 小数部分的位数·
        private const int _FractionalBits = 12;

        // 常用常量
        public static readonly BFP Zero = new BFP(0, 0);
        public static readonly BFP One = new BFP(0, 1UL << _FractionalBits);
        public static readonly BFP Half = new BFP(0, 1UL << (_FractionalBits - 1));
        public static readonly BFP MaxValue = new BFP(long.MaxValue, ulong.MaxValue);
        public static readonly BFP MinValue = new BFP(long.MinValue, 0);
        public static readonly BFP Epsilon = new BFP(0, 1);

        // 构造函数
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BFP(long high, ulong low)
        {
            _high = high;
            _low = low;
        }

        // 获取原始值
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetHighValue()
        {
            return _high;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetLowValue()
        {
            return _low;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP FromDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentException("不能从NaN或Infinity创建BigFixedPoint");
            }

            // 处理0
            if (value == 0.0)
                return Zero;

            // 拆分符号和绝对值
            bool isNegative = value < 0;
            double abs = Math.Abs(value);

            // 拆分整数和小数部分
            double intPart = Math.Floor(abs);
            double fracPart = abs - intPart;

            // 处理整数部分
            ulong lowValue = 0;
            long highValue = 0;

            // 如果整数部分太大，会导致溢出
            if (intPart > long.MaxValue)
            {
                return isNegative ? MinValue : MaxValue;
            }

            // 转换整数部分
            lowValue = (ulong)intPart << _FractionalBits;

            // 转换小数部分
            lowValue |= (ulong)(fracPart * (1UL << _FractionalBits)) & ((1UL << _FractionalBits) - 1);

            // 处理符号
            if (isNegative)
            {
                // 对负数取补码
                lowValue = ~lowValue + 1;
                highValue = lowValue == 0 ? -1 : -1;
            }

            return new BFP(highValue, lowValue);
        }

        // 转换到基本类型
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ToLong()
        {
            // 简化处理，只考虑在long范围内的值
            if (_high == 0)
            {
                return (long)(_low >> _FractionalBits);
            }
            else if (_high == -1)
            {
                return -((long)(~_low >> _FractionalBits) + 1);
            }
            else
            {
                return _high >= 0 ? long.MaxValue : long.MinValue;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ToDouble()
        {
            if (_high == 0 && _low == 0)
                return 0.0;

            if (_high >= 0)
            {
                // 处理正数
                double result = (double)_low / (1 << _FractionalBits);
                if (_high > 0)
                {
                    result += (double)_high * (double)(1UL << (64 - _FractionalBits));
                }
                return result;
            }
            else
            {
                // 处理负数 (使用补码)
                if (_low == 0)
                {
                    return (double)_high * (double)(1UL << (64 - _FractionalBits));
                }
                else
                {
                    return -((double)(~_low + 1) / (1 << _FractionalBits));
                }
            }
        }

        // 加法操作
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP operator +(BFP a, BFP b)
        {
            ulong resultLow = a._low + b._low;
            long resultHigh = a._high + b._high;

            // 处理低位溢出
            if (resultLow < a._low) // 说明发生了溢出
            {
                resultHigh++;
            }

            return new BFP(resultHigh, resultLow);
        }

        // 减法操作
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP operator -(BFP a, BFP b)
        {
            // 在补码表示中，减法可以通过加上b的相反数来实现
            ulong lowNot = ~b._low;
            long highNot = ~b._high;

            // 如果低位部分不是全为1，那么高位部分不需要+1
            if (lowNot != ulong.MaxValue)
            {
                highNot = highNot;
            }
            else
            {
                highNot = highNot + 1;
            }

            // 现在我们有了b的补码形式，加上a即可得到a-b的结果
            ulong resultLow = a._low + lowNot + 1; // +1是补码的一部分
            long resultHigh = a._high + highNot;

            // 处理低位溢出
            if (resultLow < a._low)
            {
                resultHigh++;
            }

            return new BFP(resultHigh, resultLow);
        }

        // 一元负号操作
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP operator -(BFP a)
        {
            // 按位取反再加1
            ulong notLow = ~a._low;
            long notHigh = ~a._high;

            ulong resultLow = notLow + 1;
            long resultHigh = notHigh;

            // 处理低位溢出
            if (resultLow == 0)
            {
                resultHigh++;
            }

            return new BFP(resultHigh, resultLow);
        }

        // 除法操作
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP operator /(BFP a, BFP b)
        {
            // 检查除以零
            if (b._high == 0 && b._low == 0)
            {
                throw new DivideByZeroException("不能除以零");
            }

            // 特殊情况处理
            if (a._high == 0 && a._low == 0)
            {
                return Zero;
            }

            // 处理符号
            bool isNegative = (a._high < 0) != (b._high < 0);

            // 获取a和b的绝对值
            BFP absA = a._high < 0 ? -a : a;
            BFP absB = b._high < 0 ? -b : b;

            // 实现除法的长除法算法
            // 首先，将被除数左移FractionalBits位以保持精度
            BFP dividend = new BFP(
                (absA._high << _FractionalBits) | (long)(absA._low >> (64 - _FractionalBits)),
                absA._low << _FractionalBits
            );

            // 使用二分法查找结果
            BFP low = Zero;
            BFP high = new BFP(long.MaxValue, ulong.MaxValue);
            BFP mid = Zero;

            // 迭代次数控制精度
            const int maxIterations = 64;
            int iterations = 0;

            while (low < high && iterations < maxIterations)
            {
                // 计算中间值
                mid = new BFP(
                    (low._high >> 1) + (high._high >> 1) + ((low._high & 1) & (high._high & 1)),
                    (low._low >> 1) + (high._low >> 1) + ((low._low & 1) & (high._low & 1))
                );

                // 如果mid * absB <= dividend，低位边界上移
                BFP product = mid * absB;

                if (product <= dividend)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }

                iterations++;
            }

            // 应用符号
            return isNegative ? -low : low;
        }

        // 模运算
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP operator %(BFP a, BFP b)
        {
            // 检查除以零
            if (b._high == 0 && b._low == 0)
            {
                throw new DivideByZeroException("不能对零取模");
            }

            // 使用公式: a % b = a - (a / b) * b
            BFP quotient = a / b;
            BFP product = quotient * b;
            return a - product;
        }

        // 乘法操作 - 完整实现，适用于生产环境
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP operator *(BFP a, BFP b)
        {
            // 处理特殊情况
            if ((a._high == 0 && a._low == 0) || (b._high == 0 && b._low == 0))
                return Zero;

            // 使用完整的128位乘法算法

            // 将64位分解为32位块以避免溢出
            uint a0 = (uint)(a._low & 0xFFFFFFFF);
            uint a1 = (uint)(a._low >> 32);
            uint a2 = (uint)(a._high & 0xFFFFFFFF);
            uint a3 = (uint)(a._high >> 32);

            uint b0 = (uint)(b._low & 0xFFFFFFFF);
            uint b1 = (uint)(b._low >> 32);
            uint b2 = (uint)(b._high & 0xFFFFFFFF);
            uint b3 = (uint)(b._high >> 32);

            // 计算部分乘积（最多产生64位结果）
            ulong c0 = (ulong)a0 * b0;
            ulong c1 = (ulong)a0 * b1 + (ulong)a1 * b0;
            ulong c2 = (ulong)a0 * b2 + (ulong)a1 * b1 + (ulong)a2 * b0;
            ulong c3 = (ulong)a0 * b3 + (ulong)a1 * b2 + (ulong)a2 * b1 + (ulong)a3 * b0;
            ulong c4 = (ulong)a1 * b3 + (ulong)a2 * b2 + (ulong)a3 * b1;
            ulong c5 = (ulong)a2 * b3 + (ulong)a3 * b2;
            ulong c6 = (ulong)a3 * b3;

            // 组合结果，处理进位
            ulong r0 = c0 & 0xFFFFFFFF;
            ulong r1 = (c0 >> 32) + (c1 & 0xFFFFFFFF);
            ulong r2 = (c1 >> 32) + (c2 & 0xFFFFFFFF) + (r1 >> 32);
            ulong r3 = (c2 >> 32) + (c3 & 0xFFFFFFFF) + (r2 >> 32);
            ulong r4 = (c3 >> 32) + (c4 & 0xFFFFFFFF) + (r3 >> 32);
            ulong r5 = (c4 >> 32) + (c5 & 0xFFFFFFFF) + (r4 >> 32);
            ulong r6 = (c5 >> 32) + c6 + (r5 >> 32);

            // 组装最终的128位结果
            ulong resultLow = (r1 << 32) | r0;
            long resultHigh = (long)((r3 << 32) | r2);

            // 检查溢出情况 - 这是生产环境必须处理的
            if (r4 != 0 || r5 != 0 || r6 != 0 || (r3 >> 32) != 0)
            {
                // 在实际应用中，可以根据需要决定如何处理溢出
                // 例如，返回最大值或抛出异常
                if ((a._high < 0 && b._high >= 0) || (a._high >= 0 && b._high < 0))
                {
                    // 负数结果，返回最小值
                    return new BFP(long.MinValue, 0);
                }
                else
                {
                    // 正数结果，返回最大值
                    return new BFP(long.MaxValue, ulong.MaxValue);
                }
            }

            // 调整定点小数位
            // 由于使用了FractionalBits位表示小数部分，所以需要右移FractionalBits位

            // 创建最终结果，考虑小数点位置
            if (_FractionalBits == 0)
            {
                return new BFP(resultHigh, resultLow);
            }

            // 处理小数点移位
            ulong lowResult = (resultLow >> _FractionalBits) | (((ulong)resultHigh & ((1UL << _FractionalBits) - 1)) << (64 - _FractionalBits));
            long highResult = resultHigh >> _FractionalBits;

            // 处理符号位扩展
            if (resultHigh < 0 && (resultLow & ((1UL << _FractionalBits) - 1)) != 0)
            {
                highResult += 1; // 负数时需要考虑舍入
            }

            return new BFP(highResult, lowResult);
        }

        // 比较运算符
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BFP a, BFP b)
        {
            return a._high == b._high && a._low == b._low;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BFP a, BFP b)
        {
            return a._high != b._high || a._low != b._low;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(BFP a, BFP b)
        {
            // 符号不同时，高位决定大小
            if ((a._high < 0) != (b._high < 0))
                return a._high > b._high;

            // 符号相同时，比较数值大小
            if (a._high != b._high)
                return a._high > b._high;

            return a._low > b._low;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(BFP a, BFP b)
        {
            // 符号不同时，高位决定大小
            if ((a._high < 0) != (b._high < 0))
                return a._high < b._high;

            // 符号相同时，比较数值大小
            if (a._high != b._high)
                return a._high < b._high;

            return a._low < b._low;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(BFP a, BFP b)
        {
            return a > b || a == b;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(BFP a, BFP b)
        {
            return a < b || a == b;
        }

        // 接口实现
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(BFP other)
        {
            if (this > other)
                return 1;
            if (this < other)
                return -1;
            return 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BFP other)
        {
            return _high == other._high && _low == other._low;
        }
        public override bool Equals(object obj)
        {
            if (obj is BFP)
                return Equals((BFP)obj);
            return false;
        }
        public override int GetHashCode()
        {
            return _high.GetHashCode() ^ _low.GetHashCode();
        }

        // 数学函数
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP Abs(BFP value)
        {
            return value._high < 0 ? -value : value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP Min(BFP a, BFP b)
        {
            return a < b ? a : b;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP Max(BFP a, BFP b)
        {
            return a > b ? a : b;
        }

        // 转换为科学计数法表示字符串
        public override string ToString()
        {
            // 处理零的情况
            if (_high == 0 && _low == 0)
                return "0";

            double approxValue = ToDouble();

            // 如果值太大或太小，使用科学计数法
            if (Math.Abs(approxValue) >= 1e12 || (Math.Abs(approxValue) > 0 && Math.Abs(approxValue) < 0.001))
            {
                return approxValue.ToString("E6");
            }
            else
            {
                return approxValue.ToString("F6").TrimEnd('0').TrimEnd('.');
            }
        }

        // 平方根函数
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP Sqrt(BFP value)
        {
            if (value._high < 0)
            {
                throw new ArgumentOutOfRangeException("不能计算负数的平方根");
            }

            if (value._high == 0 && value._low == 0)
            {
                return Zero;
            }

            // 使用牛顿迭代法计算平方根
            // x_(n+1) = (x_n + value/x_n) / 2
            BFP result = value;
            BFP lastResult = Zero;

            // 迭代计算直到收敛
            for (int i = 0; i < 20; i++) // 限制迭代次数
            {
                lastResult = result;
                result = (result + value / result) * Half;

                // 检查是否已经足够接近
                if (Abs(result - lastResult) < Epsilon)
                {
                    break;
                }
            }

            return result;
        }

        // 幂函数 (针对整数幂)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFP Pow(BFP value, int power)
        {
            if (power == 0)
            {
                return One;
            }

            if (power < 0)
            {
                return One / Pow(value, -power);
            }

            BFP result = One;
            BFP multiplier = value;

            while (power > 0)
            {
                if ((power & 1) != 0)
                {
                    result *= multiplier;
                }

                multiplier *= multiplier;
                power >>= 1;
            }

            return result;
        }

        /// <summary>
        /// 从整型隐式转换到BFP
        /// </summary>
        public static implicit operator BFP(int value)
        {
            return new BFP(value >= 0 ? 0 : -1, (ulong)value << _FractionalBits);
        }

        /// <summary>
        /// 从长整型隐式转换到BFP
        /// </summary>
        public static implicit operator BFP(long value)
        {
            return new BFP(value >= 0 ? 0 : -1, (ulong)value << _FractionalBits);
        }

        /// <summary>
        /// 从单精度浮点数隐式转换到BFP
        /// </summary>
        public static implicit operator BFP(float value)
        {
            return FromDouble(value);
        }

        /// <summary>
        /// 从双精度浮点数隐式转换到BFP
        /// </summary>
        public static implicit operator BFP(double value)
        {
            return FromDouble(value);
        }

        /// <summary>
        /// 从FP隐式转换到BFP
        /// </summary>
        public static implicit operator BFP(FP value)
        {
            long rawValue = value.RawValue;
            // FP的小数位是32位，BFP的小数位是12位，需要进行调整
            int shift = 32 - _FractionalBits;

            if (rawValue >= 0)
            {
                // 正数情况，直接右移调整小数位
                ulong low = (ulong)rawValue >> shift;
                return new BFP(0, low);
            }
            else
            {
                // 负数情况，需要保持符号扩展
                ulong low = (ulong)rawValue >> shift;
                // 对于负数，高位填充-1以保持符号
                return new BFP(-1, low);
            }
        }
    }
}
