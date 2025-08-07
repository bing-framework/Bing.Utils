using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 金额单位转换工具类，提供人民币"分"和"元"之间的转换以及金额格式化功能。
/// </summary>
public static partial class AmountUnitConv
{
    /// <summary>
    /// 将"分"转换为"元"
    /// </summary>
    /// <param name="fen">分金额，整数值</param>
    /// <returns>转换后的元金额，保留两位小数</returns>
    public static decimal ToYuan(int fen) => Conv.ToDecimal((decimal)fen / 100, 2);

    /// <summary>
    /// 将"分"转换为"元"（可空类型重载）
    /// </summary>
    /// <param name="fen">分金额，可空整数值</param>
    /// <returns>
    /// 转换后的元金额，保留两位小数。
    /// 如果输入为 null，则返回 0.00
    /// </returns>
    public static decimal ToYuan(int? fen) => fen == null ? 0 : Conv.ToDecimal((decimal)fen / 100, 2);

    /// <summary>
    /// 将"分"转换为"元"（支持长整型，用于处理大金额）
    /// </summary>
    /// <param name="fen">分金额，长整数值</param>
    /// <returns>转换后的元金额，保留两位小数</returns>
    public static decimal ToYuan(long fen) => Conv.ToDecimal((decimal)fen / 100, 2);

    /// <summary>
    /// 将"分"转换为"元"（可空长整型重载）
    /// </summary>
    /// <param name="fen">分金额，可空长整数值</param>
    /// <returns>
    /// 转换后的元金额，保留两位小数。
    /// 如果输入为 null，则返回 0.00
    /// </returns>
    public static decimal ToYuan(long? fen) => fen == null ? 0 : Conv.ToDecimal((decimal)fen / 100, 2);

    /// <summary>
    /// 将"元"转换为"分"
    /// </summary>
    /// <param name="yuan">元金额</param>
    /// <returns>转换后的分金额，整数值</returns>
    public static int ToFen(decimal yuan) => Conv.ToInt(CutDecimalWithN(yuan, 2) * 100, 0);

    /// <summary>
    /// 将"元"转换为"分"（可空类型重载）
    /// </summary>
    /// <param name="yuan">元金额，可空值</param>
    /// <returns>
    /// 转换后的分金额，整数值。
    /// 如果输入为 null，则返回 0
    /// </returns>
    public static int ToFen(decimal? yuan) => yuan == null ? 0 : Conv.ToInt(CutDecimalWithN(yuan.Value, 2) * 100, 0);

    /// <summary>
    /// 将"元"转换为"分"（返回长整型，用于处理大金额）
    /// </summary>
    /// <param name="yuan">元金额</param>
    /// <returns>转换后的分金额，长整数值</returns>
    public static long ToFenLong(decimal yuan) => Conv.ToLong(CutDecimalWithN(yuan, 2) * 100, 0);

    /// <summary>
    /// 将"元"转换为"分"（可空类型，返回长整型）
    /// </summary>
    /// <param name="yuan">元金额，可空值</param>
    /// <returns>
    /// 转换后的分金额，长整数值。
    /// 如果输入为 null，则返回 0
    /// </returns>
    public static long ToFenLong(decimal? yuan) => yuan == null ? 0 : Conv.ToLong(CutDecimalWithN(yuan.Value, 2) * 100, 0);

    /// <summary>
    /// 截取保留N位小数且不进行四舍五入
    /// </summary>
    /// <param name="input">输入的十进制数值</param>
    /// <param name="digits">要保留的小数位数</param>
    /// <returns>截取后的十进制数值</returns>
    /// <remarks>
    /// 此方法采用截取而非四舍五入的方式处理小数，确保转换结果的一致性和可预测性。<br />
    /// 
    /// 处理逻辑：<br />
    /// 1. 如果数值没有小数点或小数位数不足，则使用字符串格式化补齐<br />
    /// 2. 如果数值有足够的小数位，则直接截取到指定位数<br />
    /// 3. 使用不变区域文化进行字符串转换，避免本地化影响<br />
    /// 
    /// 这种处理方式在金融计算中很重要，因为它避免了四舍五入可能带来的累积误差。
    /// </remarks>
    private static decimal CutDecimalWithN(decimal input, int digits)
    {
        if (digits < 0)
            throw new ArgumentOutOfRangeException(nameof(digits), "小数位数不能为负数");
        try
        {
            var decimalStr = input.ToString(CultureInfo.InvariantCulture);
            var index = decimalStr.IndexOf(".", StringComparison.Ordinal);
            if (index == -1 || decimalStr.Length < index + digits + 1)
            {
                // 没有小数点或小数位数不足，使用格式化补齐
                decimalStr = input.ToString("F" + digits, CultureInfo.InvariantCulture);
            }
            else
            {
                // 有足够的小数位，直接截取
                var length = index;
                if (digits != 0)
                    length = index + digits + 1;
                decimalStr = decimalStr.Substring(0, length);
            }

            return decimal.Parse(decimalStr, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
        {
            return input;
        }
    }

    /// <summary>
    /// 格式化金额为带千位分隔符的两位小数字符串
    /// </summary>
    /// <param name="input">输入的金额数值</param>
    /// <returns>格式化后的金额字符串，格式为 "1,234.56"</returns>
    /// <remarks>
    /// 此方法使用 N2 格式化标准，会自动添加千位分隔符并保留两位小数。
    /// 格式化结果依赖于当前系统的区域设置。
    /// 
    /// 在中文环境下，通常显示为：1,234.56
    /// 在其他某些区域，可能显示为：1.234,56（欧洲格式）
    /// </remarks>
    /// <example>
    /// <code>
    /// string money1 = AmountUnitConv.ToN2String(1234.56m);    // 返回 "1,234.56"
    /// string money2 = AmountUnitConv.ToN2String(1000000m);    // 返回 "1,000,000.00"
    /// string money3 = AmountUnitConv.ToN2String(0m);          // 返回 "0.00"
    /// string money4 = AmountUnitConv.ToN2String(-1234.56m);   // 返回 "-1,234.56"
    /// string money5 = AmountUnitConv.ToN2String(123m);        // 返回 "123.00"
    /// </code>
    /// </example>
    public static string ToN2String(decimal input) => $"{input:N2}";

    /// <summary>
    /// 格式化金额为指定区域文化的带千位分隔符的两位小数字符串
    /// </summary>
    /// <param name="input">输入的金额数值</param>
    /// <param name="culture">指定的区域文化，如果为 null 则使用当前系统设置</param>
    /// <returns>按指定区域文化格式化的金额字符串</returns>
    /// <remarks>
    /// 此方法允许指定具体的区域文化来控制数字格式化的结果。
    /// 不同的区域文化会有不同的千位分隔符和小数点符号。
    /// </remarks>
    /// <example>
    /// <code>
    /// var usCulture = new CultureInfo("en-US");
    /// var germanCulture = new CultureInfo("de-DE");
    /// 
    /// string usFormat = AmountUnitConv.ToN2String(1234.56m, usCulture);      // 返回 "1,234.56"
    /// string germanFormat = AmountUnitConv.ToN2String(1234.56m, germanCulture); // 返回 "1.234,56"
    /// string defaultFormat = AmountUnitConv.ToN2String(1234.56m, null);      // 使用当前系统设置
    /// </code>
    /// </example>
    public static string ToN2String(decimal input, CultureInfo culture)
    {
        culture ??= CultureInfo.CurrentCulture;
        return input.ToString("N2", culture);
    }

    /// <summary>
    /// 获取金额的元和分的分离值
    /// </summary>
    /// <param name="yuan">元金额</param>
    /// <returns>包含元部分和分部分的元组，元部分为整数，分部分为0-99的整数</returns>
    /// <remarks>
    /// 此方法将一个元金额分解为整数元部分和分部分，方便某些业务场景的处理。
    /// 例如：123.45元 分解为 (123元, 45分)
    /// </remarks>
    /// <example>
    /// <code>
    /// var (yuanPart, fenPart) = AmountUnitConv.SeparateYuanAndFen(123.45m);
    /// // yuanPart = 123, fenPart = 45
    /// 
    /// var (yuan2, fen2) = AmountUnitConv.SeparateYuanAndFen(100.00m);
    /// // yuan2 = 100, fen2 = 0
    /// 
    /// var (yuan3, fen3) = AmountUnitConv.SeparateYuanAndFen(0.99m);
    /// // yuan3 = 0, fen3 = 99
    /// </code>
    /// </example>
    public static (int Yuan, int Fen) SeparateYuanAndFen(decimal yuan)
    {
        var cutValue = CutDecimalWithN(yuan, 2);
        var yuanPart = (int)Math.Truncate(cutValue);
        var fenPart = (int)((cutValue - yuanPart) * 100);
        return (yuanPart, fenPart);
    }

    /// <summary>
    /// 验证金额是否在有效范围内
    /// </summary>
    /// <param name="yuan">要验证的元金额</param>
    /// <param name="minValue">最小允许值，默认为0</param>
    /// <param name="maxValue">最大允许值，默认为decimal的最大值</param>
    /// <returns>如果金额在有效范围内返回 true，否则返回 false</returns>
    /// <remarks>
    /// 此方法用于验证金额是否在业务允许的范围内。
    /// 可以自定义最小值和最大值来适应不同的业务场景。
    /// </remarks>
    /// <example>
    /// <code>
    /// bool valid1 = AmountUnitConv.IsValidAmount(100.00m);           // 返回 true
    /// bool valid2 = AmountUnitConv.IsValidAmount(-10.00m);           // 返回 false（默认最小值为0）
    /// bool valid3 = AmountUnitConv.IsValidAmount(50.00m, 0, 100);    // 返回 true
    /// bool valid4 = AmountUnitConv.IsValidAmount(150.00m, 0, 100);   // 返回 false（超过最大值）
    /// </code>
    /// </example>
    public static bool IsValidAmount(decimal yuan, decimal minValue = 0, decimal maxValue = decimal.MaxValue)
    {
        return yuan >= minValue && yuan <= maxValue;
    }

    /// <summary>
    /// 计算多个金额的总和（分单位）
    /// </summary>
    /// <param name="fenAmounts">分金额数组</param>
    /// <returns>总和（分单位）</returns>
    /// <remarks>
    /// 此方法用于计算多个分金额的总和，避免了多次单位转换可能带来的精度损失。
    /// 如果数组为空或null，返回0。
    /// </remarks>
    /// <example>
    /// <code>
    /// int[] amounts = { 12345, 67890, 11111 }; // 分金额数组
    /// long total = AmountUnitConv.SumFenAmounts(amounts); // 返回 91346
    /// </code>
    /// </example>
    public static long SumFenAmounts(params int[] fenAmounts)
    {
        if (fenAmounts == null || fenAmounts.Length == 0)
            return 0;

        long sum = 0;
        foreach (var amount in fenAmounts) 
            sum += amount;
        return sum;
    }

    /// <summary>
    /// 计算多个金额的总和（元单位）
    /// </summary>
    /// <param name="yuanAmounts">元金额数组</param>
    /// <returns>总和（元单位），保留两位小数</returns>
    /// <remarks>
    /// 此方法计算多个元金额的总和。
    /// 为了保证精度，内部会先转换为分进行计算，最后再转换回元。
    /// </remarks>
    /// <example>
    /// <code>
    /// decimal[] amounts = { 123.45m, 678.90m, 111.11m }; // 元金额数组
    /// decimal total = AmountUnitConv.SumYuanAmounts(amounts); // 返回 913.46
    /// </code>
    /// </example>
    public static decimal SumYuanAmounts(params decimal[] yuanAmounts)
    {
        if (yuanAmounts == null || yuanAmounts.Length == 0)
            return 0;

        long totalFen = 0;
        foreach (var amount in yuanAmounts) 
            totalFen += ToFenLong(amount);
        return ToYuan(totalFen);
    }
}