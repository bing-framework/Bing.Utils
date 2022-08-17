using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bing.Numeric
{
    /// <summary>
    /// 数值可选项
    /// </summary>
    public enum NumericMayOptions
    {
        /// <summary>
        /// 默认
        /// </summary>
        Default = 0,

        /// <summary>
        /// 忽略可空
        /// </summary>
        IgnoreNullable = 1
    }

    /// <summary>
    /// 数值操作
    /// </summary>
    public static partial class Numbers
    {
        #region GetMembersBetween

        /// <summary>
        /// 获取最小值和最大值之间的成员(包括最小值和最大值)
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        public static int[] GetMembersBetween(int min, int max)
        {
            if (min == max)
                return new[] { min };

            if (min > max)
                (min, max) = (max, min);

            var count = max - min + 1;
            var results = new int[count];
            var pointer = min;
            var index = 0;

            while (pointer <= max && index < count)
            {
                results[index] = pointer;
                ++index;
                ++pointer;
            }

            return results;
        }

        /// <summary>
        /// 获取最小值和最大值之间的成员(包括最小值和最大值)
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        public static long[] GetMembersBetween(long min, long max)
        {
            if (min == max)
                return new[] { min };

            if (min > max)
                (min, max) = (max, min);

            var count = max - min + 1;
            var results = new long[count];
            var pointer = min;
            var index = 0L;

            while (pointer <= max && index < count)
            {
                results[index] = pointer;
                ++index;
                ++pointer;
            }

            return results;
        }

        #endregion

        /// <summary>
        /// 修正零。如果将双公差浮点值视为零（在ε公差内），则返回真零的快捷方式。
        /// </summary>
        /// <param name="value">值</param>
        public static double FixZero(double value) => !value.Equals(0) && IsZeroValue(value) ? 0 : value;

        /// <summary>
        /// 如果有限小数则返回零
        /// </summary>
        /// <param name="value">值</param>
        private static double ReturnZeroIfFinite(float value)
        {
            if (float.IsNegativeInfinity(value))
                return double.NegativeInfinity;
            if (float.IsPositiveInfinity(value))
                return double.PositiveInfinity;
            return float.IsNaN(value) ? double.NaN : 0D;
        }

        /// <summary>
        /// 返回最后零位之前的小数位数
        /// </summary>
        /// <param name="value">值</param>
        public static int GetDecimalPlaces(double value)
        {
            if (IsNaN(value))
                return 0;
            var valueString = value.ToString(CultureInfo.InvariantCulture);
            var index = valueString.IndexOf('.');
            return index == -1 ? 0 : valueString.Length - index - 1;
        }

        /// <summary>
        /// 精确求和。通过消除意外的不准确性来确保附加公差
        /// </summary>
        /// <param name="source">源值</param>
        /// <param name="value">值</param>
        public static double GetSumAccurate(double source, double value)
        {
            var result = source + value;
            var vp = GetDecimalPlaces(source);
            if (vp > 15)
                return result;
            var ap = GetDecimalPlaces(value);
            if (ap > 15)
                return result;
            var digits = Math.Max(vp, ap);
            return Math.Round(result, digits);
        }

        /// <summary>
        /// 精确乘积。通过消除意外的不准确性来确保附加公差
        /// </summary>
        /// <param name="source">源值</param>
        /// <param name="value">值</param>
        public static double GetProductAccurate(double source, double value)
        {
            var result = source * value;
            var vp = GetDecimalPlaces(source);
            if (vp > 15)
                return result;
            var ap = GetDecimalPlaces(value);
            if (ap > 15)
                return result;
            var digits = Math.Max(vp, ap);
            return Math.Round(result, digits);
        }

        /// <summary>
        /// 求和。通过使用整数来确保加法公差
        /// </summary>
        /// <param name="source">源值</param>
        /// <param name="value">值</param>
        public static double GetSumUsingIntegers(double source, double value)
        {
            var x = Math.Pow(10, Math.Max(GetDecimalPlaces(source), GetDecimalPlaces(value)));
            var v = (long)(source * x);
            var a = (long)(value * x);
            var result = v + a;
            return result / x;
        }
    }

    /// <summary>
    /// 数值操作扩展
    /// </summary>
    public static partial class NumberExtensions
    {
        /// <summary>
        /// 修正零。如果将双公差浮点值视为零（在ε公差内），则返回真零的快捷方式。
        /// </summary>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FixZero(this double value) => Numbers.FixZero(value);

        /// <summary>
        /// 返回最后零位之前的小数位数
        /// </summary>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DecimalPlaces(this double value) => Numbers.GetDecimalPlaces(value);

        /// <summary>
        /// 精确求和。通过消除意外的不准确性来确保附加公差
        /// </summary>
        /// <param name="source">源值</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SumAccurate(this double source, double value) => Numbers.GetSumAccurate(source, value);

        /// <summary>
        /// 精确乘积。通过消除意外的不准确性来确保附加公差
        /// </summary>
        /// <param name="source">源值</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ProductAccurate(this double source, double value) => Numbers.GetProductAccurate(source, value);

        /// <summary>
        /// 求和。通过使用整数来确保加法公差
        /// </summary>
        /// <param name="source">源值</param>
        /// <param name="value">值</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SumUsingIntegers(this double source, double value) => Numbers.GetSumUsingIntegers(source, value);
    }
}
