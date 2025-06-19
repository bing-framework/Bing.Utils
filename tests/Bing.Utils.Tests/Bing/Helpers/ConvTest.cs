using Bing.Tests.Samples;
using Bing.Utils.Tests;
using System.Globalization;

namespace Bing.Helpers;

/// <summary>
/// 类型转换操作测试
/// </summary>
public class ConvTest : TestBase
{
    /// <summary>
    /// 初始化一个<see cref="ConvTest"/>类型的实例
    /// </summary>
    public ConvTest(ITestOutputHelper output) : base(output)
    {
    }

    #region ToByte

    /// <summary>
    /// 测试 - 转换为8位整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("1A", 0)]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("12.3", 12)]
    [InlineData("12.335556", 12)]
    public void Test_ToByte(object input, byte result)
    {
        Assert.Equal(result, Conv.ToByte(input));
    }

    #endregion

    #region ToByteOrNull

    /// <summary>
    /// 测试 - 转换为8位可空整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1A", null)]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("12.3", 12)]
    [InlineData("12.335556", 12)]
    public void Test_ToByteOrNull(object input, int? result)
    {
        Assert.Equal(result, Conv.ToByteOrNull(input));
    }

    #endregion

    #region ToChar

    /// <summary>
    /// 测试 - 转换为字符
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, default(char))]
    [InlineData("", default(char))]
    [InlineData("1", '1')]
    [InlineData("A", 'A')]
    public void Test_ToChar(object input, char result)
    {
        Assert.Equal(result, Conv.ToChar(input));
    }

    #endregion

    #region ToCharOrNull

    /// <summary>
    /// 测试 - 转换为可空字符
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1", '1')]
    [InlineData("A", 'A')]
    public void Test_ToCharOrNull(object input, char? result)
    {
        Assert.Equal(result, Conv.ToCharOrNull(input));
    }

    #endregion

    #region ToShort

    /// <summary>
    /// 测试 - 转换为16位整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("1A", 0)]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("12.3", 12)]
    [InlineData("12.335556", 12)]
    public void Test_ToShort(object input, short result)
    {
        Assert.Equal(result, Conv.ToShort(input));
    }

    #endregion

    #region ToShortOrNull

    /// <summary>
    /// 测试 - 转换为16位可空整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1A", null)]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("12.3", 12)]
    [InlineData("12.335556", 12)]
    public void Test_ToShortOrNull(object input, int? result)
    {
        Assert.Equal(result, Conv.ToShortOrNull(input));
    }

    #endregion

    #region ToInt

    /// <summary>
    /// 测试 - 转换为32位整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("1A", 0)]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("1778019.78", 1778020)]
    [InlineData("1778019.7801684", 1778020)]
    public void Test_ToInt(object input, int result)
    {
        Assert.Equal(result, Conv.ToInt(input));
    }

    #endregion

    #region ToIntOrNull

    /// <summary>
    /// 测试 - 转换为32位可空整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1A", null)]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("1778019.78", 1778020)]
    [InlineData("1778019.7801684", 1778020)]
    public void Test_ToIntOrNull(object input, int? result)
    {
        Assert.Equal(result, Conv.ToIntOrNull(input));
    }

    #endregion

    #region ToLong

    /// <summary>
    /// 测试 - 转换为64位整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("1A", 0)]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("1778019.7801684", 1778020)]
    [InlineData("177801978016841234", 177801978016841234)]
    public void Test_ToLong(object input, long result)
    {
        Assert.Equal(result, Conv.ToLong(input));
    }

    #endregion

    #region ToLongOrNull

    /// <summary>
    /// 测试 - 转换为64位可空整型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1A", null)]
    [InlineData("0", 0L)]
    [InlineData("1", 1L)]
    [InlineData("1778019.7801684", 1778020L)]
    [InlineData("177801978016841234", 177801978016841234L)]
    public void Test_ToLongOrNull(object input, long? result)
    {
        Assert.Equal(result, Conv.ToLongOrNull(input));
    }

    #endregion

    #region ToFloat

    /// <summary>
    /// 测试 - 转换为32位浮点型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    /// <param name="digits">小数位数</param>
    [Theory]
    [InlineData(null, 0, null)]
    [InlineData("", 0, null)]
    [InlineData("1A", 0, null)]
    [InlineData("0", 0, null)]
    [InlineData("1", 1, null)]
    [InlineData("1.2", 1.2, null)]
    [InlineData("12.346", 12.35, 2)]
    public void Test_ToFloat(object input, float result, int? digits)
    {
        Assert.Equal(result, Conv.ToFloat(input, digits));
    }

    #endregion

    #region ToFloatOrNull

    /// <summary>
    /// 测试 - 转换为32位可空浮点型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    /// <param name="digits">小数位数</param>
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", null, null)]
    [InlineData("1A", null, null)]
    [InlineData("0", 0f, null)]
    [InlineData("1", 1f, null)]
    [InlineData("1.2", 1.2f, null)]
    [InlineData("12.346", 12.35f, 2)]
    public void Test_ToFloatOrNull(object input, float? result, int? digits)
    {
        Assert.Equal(result, Conv.ToFloatOrNull(input, digits));
    }

    #endregion

    #region ToDouble

    /// <summary>
    /// 测试 - 转换为64位浮点型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    /// <param name="digits">小数位数</param>
    [Theory]
    [InlineData(null, 0, null)]
    [InlineData("", 0, null)]
    [InlineData("1A", 0, null)]
    [InlineData("0", 0, null)]
    [InlineData("1", 1, null)]
    [InlineData("1.2", 1.2, null)]
    [InlineData("12.235", 12.24, 2)]
    [InlineData("12.345", 12.35, 2)]
    [InlineData("12.3451", 12.35, 2)]
    [InlineData("12.346", 12.35, 2)]
    public void Test_ToDouble(object input, double result, int? digits)
    {
        Assert.Equal(result, Conv.ToDouble(input, digits));
    }

    #endregion

    #region ToDoubleOrNull

    /// <summary>
    /// 测试 - 转换为64位可空浮点型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    /// <param name="digits">小数位数</param>
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", null, null)]
    [InlineData("1A", null, null)]
    [InlineData("0", 0d, null)]
    [InlineData("1", 1d, null)]
    [InlineData("1.2", 1.2, null)]
    [InlineData("12.355", 12.36, 2)]
    public void Test_ToDoubleOrNull(object input, double? result, int? digits)
    {
        Assert.Equal(result, Conv.ToDoubleOrNull(input, digits));
    }

    #endregion

    #region ToDecimal

    /// <summary>
    /// 测试 - 转换为128位浮点型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    /// <param name="digits">小数位数</param>
    [Theory]
    [InlineData(null, 0, null)]
    [InlineData("", 0, null)]
    [InlineData("1A", 0, null)]
    [InlineData("0", 0, null)]
    [InlineData("1", 1, null)]
    [InlineData("1.2", 1.2, null)]
    [InlineData("12.235", 12.24, 2)]
    [InlineData("12.345", 12.35, 2)]
    [InlineData("12.3451", 12.35, 2)]
    [InlineData("12.346", 12.35, 2)]
    public void Test_ToDecimal(object input, decimal result, int? digits)
    {
        Assert.Equal(result, Conv.ToDecimal(input, digits));
    }

    /// <summary>
    /// 测试 - 转换为128位浮点型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    /// <param name="digits">小数位数</param>
    [Theory]
    [InlineData(null, 0, null)]
    [InlineData("", 0, null)]
    [InlineData("1A", 0, null)]
    [InlineData("0", 0, null)]
    [InlineData("1", 1, null)]
    [InlineData("1.2", 1.2, null)]
    [InlineData("12.235", 12.23, 2)]
    [InlineData("12.345", 12.34, 2)]
    [InlineData("12.3451", 12.34, 2)]
    [InlineData("12.346", 12.34, 2)]
    public void Test_ToDecimal_ToZero(object input, decimal result, int? digits)
    {
#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal(result, Conv.ToDecimal(input, 0, digits, MidpointRounding.ToZero));
#endif
    }

    #endregion

    #region ToDecimalOrNull

    /// <summary>
    /// 测试 - 转换为128位可空浮点型，验证
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    /// <param name="digits">小数位数</param>
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", null, null)]
    [InlineData("1A", null, null)]
    [InlineData("1A", null, 2)]
    public void Test_ToDecimalOrNull_Validate(object input, decimal? result, int? digits)
    {
        Assert.Equal(result, Conv.ToDecimalOrNull(input, digits));
    }

    /// <summary>
    /// 测试 - 转换为128位可空浮点型，输入值为"0"
    /// </summary>
    [Fact]
    public void Test_ToDecimalOrNull()
    {
        Assert.Equal(0M, Conv.ToDecimalOrNull("0"));
        Assert.Equal(1.2M, Conv.ToDecimalOrNull("1.2"));
        Assert.Equal(23.46M, Conv.ToDecimalOrNull("23.456", 2));
    }

    #endregion

    #region ToBool

    /// <summary>
    /// 测试 - 转换为布尔型（字符串输入场景）
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">期望结果</param>
    [Theory]
    [InlineData("true", true)]      // 标准布尔字符串
    [InlineData("false", false)]
    [InlineData("1", true)]         // 数字字符串
    [InlineData("0", false)]
    [InlineData("是", true)]        // 中文肯定
    [InlineData("否", false)]       // 中文否定
    [InlineData("ok", true)]        // 英文肯定
    [InlineData("fail", false)]     // 英文否定
    [InlineData("yes", true)]
    [InlineData("no", false)]
    [InlineData("on", true)]        // 开启/关闭
    [InlineData("off", false)]
    [InlineData("enable", true)]
    [InlineData("disable", false)]
    [InlineData("enabled", true)]
    [InlineData("disabled", false)]
    [InlineData("y", true)]         // 简写
    [InlineData("n", false)]
    [InlineData("t", true)]
    [InlineData("f", false)]
    [InlineData("", false)]         // 空字符串
    [InlineData(null, false)]       // null
    [InlineData("abc", false)]      // 无效字符串
    public void Test_ToBool_StringInput_ReturnsExpected(object input, bool result)
    {
        Assert.Equal(result, Conv.ToBool(input));
    }

    /// <summary>
    /// 测试 - 转换为布尔型（数值输入场景）
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">期望结果</param>
    [Theory]
    [InlineData(1, true)]           // 非零整数
    [InlineData(0, false)]          // 零
    [InlineData(-1, true)]          // 负数
    [InlineData(123.45, true)]      // 非零浮点数
    [InlineData(0.0, false)]        // 零浮点数
    public void Test_ToBool_NumberInput_ReturnsExpected(object input, bool result)
    {
        // 验证数值类型输入的布尔转换
        Assert.Equal(result, Conv.ToBool(input));
    }

    /// <summary>
    /// 测试 - 转换为布尔型（bool类型输入）
    /// </summary>
    [Fact]
    public void Test_ToBool_BoolInput_ReturnsSelf()
    {
        // 直接传入bool类型，结果应与输入一致
        Assert.True(Conv.ToBool(true));
        Assert.False(Conv.ToBool(false));
    }

    /// <summary>
    /// 测试 - 转换为布尔型（枚举类型输入）
    /// </summary>
    private enum TestEnum { None = 0, Yes = 1 }

    [Fact]
    public void Test_ToBool_EnumInput_ReturnsExpected()
    {
        // 枚举值为0应为false，非0为true
        Assert.False(Conv.ToBool(TestEnum.None));
        Assert.True(Conv.ToBool(TestEnum.Yes));
    }

    /// <summary>
    /// 测试 - ToBool 边界和特殊情况
    /// </summary>
    [Theory]
    [InlineData("   ", false)]                // 仅空白字符串
    [InlineData("\t\n", false)]               // 制表符和换行
    [InlineData("TrUe", true)]                // 大小写混合
    [InlineData("FaLsE", false)]
    [InlineData("YeS", true)]
    [InlineData("nO", false)]
    [InlineData("null", false)]               // 常见非布尔表达
    [InlineData("none", false)]
    [InlineData("undefined", false)]
    [InlineData("empty", false)]
    [InlineData("2", false)]                   // 非标准数字字符串
    [InlineData("-1", false)]
    [InlineData("0.0", false)]
    [InlineData("1.0", false)]
    [InlineData(double.NaN, true)]           // NaN
    [InlineData(double.PositiveInfinity, true)] // 正无穷
    [InlineData(double.NegativeInfinity, true)] // 负无穷
    [InlineData(int.MaxValue, true)]          // 极大值
    [InlineData(int.MinValue, true)]          // 极小值
    [InlineData(new int[] { 1 }, false)]      // 数组
    public void Test_ToBool_BoundaryCases(object input, bool expected)
    {
        // 验证 ToBool 方法在各种边界和特殊输入下的健壮性
        Assert.Equal(expected, Conv.ToBool(input));
    }

    /// <summary>
    /// 测试 - ToBool 复杂类型输入（如对象、数组）
    /// </summary>
    [Fact]
    public void Test_ToBool_ObjectAndArrayInput()
    {
        // 数据库空值
        Assert.False(Conv.ToBool(DBNull.Value));

        // 普通对象
        object obj = new object();
        Assert.False(Conv.ToBool(obj));

        // 数组
        int[] arr = new int[] { 1 };
        Assert.False(Conv.ToBool(arr));
    }

    /// <summary>
    /// 测试 - 转换为可空布尔型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1A", null)]
    [InlineData("0", false)]
    [InlineData("否", false)]
    [InlineData("不", false)]
    [InlineData("no", false)]
    [InlineData("No", false)]
    [InlineData("false", false)]
    [InlineData("fail", false)]
    [InlineData("1", true)]
    [InlineData("是", true)]
    [InlineData("yes", true)]
    [InlineData("true", true)]
    [InlineData("ok", true)]
    public void Test_ToBoolOrNull(object input, bool? result)
    {
        Assert.Equal(result, Conv.ToBoolOrNull(input));
    }

    #endregion

    #region ToDate

    /// <summary>
    /// 测试 - 转换为日期，验证
    /// </summary>
    [Fact]
    public void Test_ToDate_Validate()
    {
        Assert.Equal(DateTime.MinValue, Conv.ToDate(null));
        Assert.Equal(DateTime.MinValue, Conv.ToDate(""));
        Assert.Equal(DateTime.MinValue, Conv.ToDate("1A"));
    }

    /// <summary>
    /// 测试 - 转换为日期
    /// </summary>
    [Fact]
    public void Test_ToDate()
    {
        Assert.Equal(new DateTime(2000, 1, 1), Conv.ToDate("2000-1-1"));
    }

    #endregion

    #region ToDateOrNull

    /// <summary>
    /// 测试 - 转换为可空日期，验证
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1A", null)]
    public void Test_ToDateOrNull_Validate(object input, DateTime? result)
    {
        Assert.Equal(result, Conv.ToDateOrNull(input));
    }

    /// <summary>
    /// 测试 - 转换为可空日期
    /// </summary>
    [Fact]
    public void Test_ToDateOrNull()
    {
        Assert.Equal(new DateTime(2000, 1, 1), Conv.ToDateOrNull("2000-1-1"));
    }

    #endregion

    #region ToGuid

    /// <summary>
    /// 测试 - 转换为Guid，验证
    /// </summary>
    [Fact]
    public void Test_ToGuid_Validate()
    {
        Assert.Equal(Guid.Empty, Conv.ToGuid(null));
        Assert.Equal(Guid.Empty, Conv.ToGuid(""));
        Assert.Equal(Guid.Empty, Conv.ToGuid("1A"));
    }

    /// <summary>
    /// 测试 - 转换为Guid
    /// </summary>
    [Fact]
    public void Test_ToGuid()
    {
        Assert.Equal(new Guid("B9EB56E9-B720-40B4-9425-00483D311DDC"), Conv.ToGuid("B9EB56E9-B720-40B4-9425-00483D311DDC"));
    }

    #endregion

    #region ToGuidOrNull

    /// <summary>
    /// 测试 - 转换为可空Guid，验证
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("1A", null)]
    public void Test_ToGuidOrNull_Validate(object input, Guid? result)
    {
        Assert.Equal(result, Conv.ToGuidOrNull(input));
    }

    /// <summary>
    /// 测试 - 转换为可空Guid
    /// </summary>
    [Fact]
    public void Test_ToGuidOrNull()
    {
        Assert.Equal(new Guid("B9EB56E9-B720-40B4-9425-00483D311DDC"), Conv.ToGuidOrNull("B9EB56E9-B720-40B4-9425-00483D311DDC"));
    }

    #endregion

    #region ToGuidList

    /// <summary>
    /// 测试 - 转换为Guid集合
    /// </summary>
    [Fact]
    public void Test_ToGuidList()
    {
        Assert.Empty(Conv.ToGuidList(null));
        Assert.Empty(Conv.ToGuidList(""));

        const string guid = "83B0233C-A24F-49FD-8083-1337209EBC9A";
        Assert.Single(Conv.ToGuidList(guid));
        Assert.Equal(new Guid(guid), Conv.ToGuidList(guid)[0]);

        const string guid2 = "83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A";
        Assert.Equal(2, Conv.ToGuidList(guid2).Count);
        Assert.Equal(new Guid("83B0233C-A24F-49FD-8083-1337209EBC9A"), Conv.ToGuidList(guid2)[0]);
        Assert.Equal(new Guid("EAB523C6-2FE7-47BE-89D5-C6D440C3033A"), Conv.ToGuidList(guid2)[1]);
    }

    /// <summary>
    /// 测试 - 转换为Guid集合
    /// </summary>
    [Fact]
    public void Test_ToGuidList_2()
    {
        const string guid = "83B0233C-A24F-49FD-8083-1337209EBC9A,,EAB523C6-2FE7-47BE-89D5-C6D440C3033A,";
        Assert.Equal(2, Conv.ToGuidList(guid).Count);
        Assert.Equal(new Guid("83B0233C-A24F-49FD-8083-1337209EBC9A"), Conv.ToGuidList(guid)[0]);
        Assert.Equal(new Guid("EAB523C6-2FE7-47BE-89D5-C6D440C3033A"), Conv.ToGuidList(guid)[1]);
    }

    #endregion

    #region ToBytes

    /// <summary>
    /// 测试 - 转换为字节数组 - 默认UTF8编码
    /// </summary>
    [Fact]
    public void Test_ToBytes_DefaultEncoding()
    {
        // 测试空字符串
        var emptyResult = Conv.ToBytes(string.Empty);
        Assert.Empty(emptyResult);

        // 测试null
        var nullResult = Conv.ToBytes(null);
        Assert.Empty(nullResult);

        // 测试普通字符串
        var input = "测试字符串";
        var expected = Encoding.UTF8.GetBytes(input);
        var result = Conv.ToBytes(input);

        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 转换为字节数组 - 指定编码
    /// </summary>
    [Fact]
    public void Test_ToBytes_SpecificEncoding()
    {
        var input = "测试字符串";

        // 测试UTF8编码
        var utf8Result = Conv.ToBytes(input, Encoding.UTF8);
        Assert.Equal(Encoding.UTF8.GetBytes(input), utf8Result);

        // 测试ASCII编码
        var asciiResult = Conv.ToBytes(input, Encoding.ASCII);
        Assert.Equal(Encoding.ASCII.GetBytes(input), asciiResult);

        // 测试Unicode编码
        var unicodeResult = Conv.ToBytes(input, Encoding.Unicode);
        Assert.Equal(Encoding.Unicode.GetBytes(input), unicodeResult);
    }

    #endregion

    #region ToBase64

    /// <summary>
    /// 测试 - 转换为Base64字符串
    /// </summary>
    [Fact]
    public void Test_ToBase64()
    {
        // 测试空字符串
        Assert.Null(Conv.ToBase64(string.Empty));

        // 测试null
        Assert.Null(Conv.ToBase64(null));

        // 测试普通字符串
        var input = "Hello World";
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        var result = Conv.ToBase64(input);

        Assert.Equal(expected, result);

        // 测试包含中文的字符串
        input = "你好，世界";
        expected = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        result = Conv.ToBase64(input);

        Assert.Equal(expected, result);
    }

    #endregion

    #region ToList

    /// <summary>
    /// 测试 - 泛型集合转换
    /// </summary>
    [Fact]
    public void Test_ToList()
    {
        Assert.Empty(Conv.ToList<string>(null));
        Assert.Single(Conv.ToList<string>("1"));
        Assert.Equal(2, Conv.ToList<string>("1,2").Count);
        Assert.Equal(2, Conv.ToList<int>("1,2")[1]);
    }

    #endregion

    #region To

    /// <summary>
    /// 测试 - 通用泛型转换
    /// </summary>
    [Fact]
    public void Test_To()
    {
        Assert.Null(Conv.To<string>(""));
        Assert.Equal("1A", Conv.To<string>("1A"));
        Assert.Equal(0, Conv.To<int>(null));
        Assert.Equal(0, Conv.To<int>(""));
        Assert.Equal(0, Conv.To<int>("2A"));
        Assert.Equal(1, Conv.To<int>("1"));
        Assert.Null(Conv.To<int?>(null));
        Assert.Null(Conv.To<int?>(""));
        Assert.Null(Conv.To<int?>("3A"));
        Assert.Equal(Guid.Empty, Conv.To<Guid>(""));
        Assert.Equal(Guid.Empty, Conv.To<Guid>("4A"));
        Assert.Equal(new Guid("B9EB56E9-B720-40B4-9425-00483D311DDC"), Conv.To<Guid>("B9EB56E9-B720-40B4-9425-00483D311DDC"));
        Assert.Equal(new Guid("B9EB56E9-B720-40B4-9425-00483D311DDC"), Conv.To<Guid?>("B9EB56E9-B720-40B4-9425-00483D311DDC"));
        Assert.Equal(12.5, Conv.To<double>("12.5"));
        Assert.Equal(12.5, Conv.To<double?>("12.5"));
        Assert.Equal(12.5M, Conv.To<decimal>("12.5"));

        Conv.To<double>("12.5").ShouldBe(12.5);
        Conv.To<double>("0.2").ShouldBe(0.2);
        Conv.To<int>(2.0).ShouldBe(2);

        Conv.To<bool>("false").ShouldBe(false);
        Conv.To<bool>("True").ShouldBe(true);
        Conv.To<bool>("False").ShouldBe(false);
        Conv.To<bool>("TrUE").ShouldBe(true);

        Assert.Equal(new DateTime(2000, 1, 1), Conv.To<DateTime>("2000-1-1"));
        Assert.Equal(new DateTime(2000, 1, 1), Conv.To<DateTime?>("2000-1-1"));
        var guid = Guid.NewGuid();
        Assert.Equal(guid.ToString(), Conv.To<string>(guid));
        Assert.Equal(EnumSample.C, Conv.To<EnumSample>("c"));
    }

    /// <summary>
    /// 测试 - 通用泛型转换 - 转换为整数
    /// </summary>
    /// <param name="input"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("123", 123)]
    [InlineData("123.45", 0)]
    [InlineData("abc", 0)]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(double.NegativeInfinity, 0)]
    public void Test_To_Int(object input, int expected)
    {
        Assert.Equal(expected, Conv.To<int>(input));
    }

    /// <summary>
    /// 测试 - 通用泛型转换 - 转换对象副本
    /// </summary>
    [Fact]
    public void Test_To_2()
    {
        Sample4 sample = new Sample4 { StringValue = "a" };
        var result = Conv.To<Sample4>(sample.GetClone());
        Assert.Equal("a", result?.StringValue);
    }

    /// <summary>
    /// 测试 - 通用泛型转换 - 转换Json元素
    /// </summary>
    [Fact]
    public void Test_To_3()
    {
        //序列化再反序列化字典
        var dic = new Dictionary<string, object> {
            {"a", new Sample3 {StringValue = "a"}},
            {"b", new Sample3 {StringValue = "b"}}
        };
        var json = Json.ToJson(dic);
        dic = Json.ToObject<Dictionary<string, object>>(json);

        //从字典中获取元素并转换
        var element = dic["b"];
        var result = Conv.To<Sample3>(element);

        //验证
        Assert.Equal("b", result?.StringValue);
    }

    #endregion

    #region ToDictionary

    /// <summary>
    /// 测试 - 对象转换为字典(属性名-属性值)
    /// </summary>
    [Fact]
    public void Test_ToDictionary_1()
    {
        var sample = new Sample2
        {
            BoolValue = true,
            Description = "Description",
            StringValue = "StringValue",
            IntValue = 2,
            Display = "Display",
            NullableBoolValue = true,
            DisplayName = "DisplayName",
            DisplayName2 = "DisplayName2",
            Test3 = new Sample3 { StringValue = "a" },
            TestList = new List<Sample3> { new() { StringValue = "a" }, new() { StringValue = "b" } }
        };
        var result = Conv.ToDictionary(sample);
        Assert.Equal(10, result.Count);
        Assert.Equal("Description", result["Description"]);
        Assert.Equal("Display", result["Display"]);
        Assert.Equal("DisplayName", result["DisplayName"]);
        Assert.Equal("DisplayName2", result["DisplayName2"]);
        Assert.Equal(2, result["IntValue"]);
    }

    /// <summary>
    /// 测试 - 对象转换为字典(属性名-属性值)
    /// </summary>
    [Fact]
    public void Test_ToDictionary_2()
    {
        var sample = new Sample2
        {
            BoolValue = true,
            Description = "Description",
            StringValue = "StringValue",
            IntValue = 2,
            Display = "Display",
            NullableBoolValue = true,
            DisplayName = "DisplayName",
            DisplayName2 = "DisplayName2",
            Test3 = new Sample3 { StringValue = "a" },
            TestList = new List<Sample3> { new() { StringValue = "a" }, new() { StringValue = "b" } }
        };
        var result = Conv.ToDictionary(sample, true);
        Assert.Equal(10, result.Count);
        Assert.Equal("Description", result["描述"]);
        Assert.Equal("Display", result["Display"]);
        Assert.Equal("DisplayName", result["显示名"]);
        Assert.Equal("DisplayName2", result["DisplayName2"]);
        Assert.Equal(2, result["IntValue"]);
    }

    /// <summary>
    /// 测试 - 对象转换为字典(属性名-属性值) - 传入字典
    /// </summary>
    [Fact]
    public void Test_ToDictionary_3()
    {
        var content = new Dictionary<string, object>
        {
            { "Code", "a" },
            { "Price", 0 }
        };
        var result = Conv.ToDictionary(content);
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result["Code"]);
        Assert.Equal(0, result["Price"]);
    }

    #endregion

    #region ToStringOrDefault

    #region 无格式参数的ToStringOrDefault测试

    /// <summary>
    /// 测试 - ToStringOrDefault - 正常值
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithValue()
    {
        // Arrange
        int? nullableInt = 42;
        decimal? nullableDecimal = 123.45m;
        double? nullableDouble = 9876.54;
        DateTime? nullableDateTime = new DateTime(2025, 6, 18, 15, 30, 0);

        // Act
        string intResult = Conv.ToStringOrDefault(nullableInt, "默认整数");
        string decimalResult = Conv.ToStringOrDefault(nullableDecimal, "默认小数");
        string doubleResult = Conv.ToStringOrDefault(nullableDouble, "默认双精度");
        string dateResult = Conv.ToStringOrDefault(nullableDateTime, "默认日期");

        // Assert
        Assert.Equal("42", intResult);
        Assert.Equal("123.45", decimalResult);
        Assert.Equal("9876.54", doubleResult);
        Assert.Equal(new DateTime(2025, 6, 18, 15, 30, 0).ToString(), dateResult);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - null值返回默认值
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithNull()
    {
        // Arrange
        int? nullableInt = null;
        decimal? nullableDecimal = null;
        double? nullableDouble = null;
        DateTime? nullableDateTime = null;

        // Act
        string intResult = Conv.ToStringOrDefault(nullableInt, "默认整数");
        string decimalResult = Conv.ToStringOrDefault(nullableDecimal, "默认小数");
        string doubleResult = Conv.ToStringOrDefault(nullableDouble, "默认双精度");
        string dateResult = Conv.ToStringOrDefault(nullableDateTime, "默认日期");

        // Assert
        Assert.Equal("默认整数", intResult);
        Assert.Equal("默认小数", decimalResult);
        Assert.Equal("默认双精度", doubleResult);
        Assert.Equal("默认日期", dateResult);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 默认值为空字符串
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_EmptyDefault()
    {
        // Arrange
        int? nullableInt = null;

        // Act
        string result = Conv.ToStringOrDefault(nullableInt, string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 默认值为null
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_NullDefault()
    {
        // Arrange
        int? nullableInt = null;

        // Act
        string result = Conv.ToStringOrDefault(nullableInt, null);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region 带格式参数的ToStringOrDefault测试

    /// <summary>
    /// 测试 - ToStringOrDefault - 带格式 - 数字格式
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithFormat_Number()
    {
        // Arrange
        decimal? price = 1234.56m;

        // Act
        string result = Conv.ToStringOrDefault(price, "N2", "未定价");

        // Assert
        Assert.Equal(1234.56m.ToString("N2", CultureInfo.CurrentCulture), result);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 带格式 - 货币格式
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithFormat_Currency()
    {
        // Arrange
        decimal? price = 1234.56m;

        // Act
        string result = Conv.ToStringOrDefault(price, "C", "未定价");

        // Assert
        Assert.Equal(1234.56m.ToString("C", CultureInfo.CurrentCulture), result);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 带格式 - 日期格式
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithFormat_Date()
    {
        // Arrange
        DateTime? date = new DateTime(2025, 6, 18, 15, 30, 0);

        // Act
        string result = Conv.ToStringOrDefault(date, "yyyy-MM-dd", "未设置日期");

        // Assert
        Assert.Equal("2025-06-18", result);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 带格式 - 自定义格式
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithFormat_Custom()
    {
        // Arrange
        int? number = 42;

        // Act
        string result = Conv.ToStringOrDefault(number, "D5", "未设置编号");

        // Assert
        Assert.Equal("00042", result);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 带格式 - null值返回默认值
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithFormat_Null()
    {
        // Arrange
        decimal? price = null;
        DateTime? date = null;

        // Act
        string priceResult = Conv.ToStringOrDefault(price, "C2", "未定价");
        string dateResult = Conv.ToStringOrDefault(date, "yyyy-MM-dd", "未设置日期");

        // Assert
        Assert.Equal("未定价", priceResult);
        Assert.Equal("未设置日期", dateResult);
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 带格式 - 空格式字符串
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithEmptyFormat()
    {
        // Arrange
        decimal? price = 1234.56m;

        // Act
        string result = Conv.ToStringOrDefault(price, "", "未定价");

        // Assert
        Assert.Equal("1234.56", result);  // 使用默认格式
    }

    /// <summary>
    /// 测试 - 在不同区域文化设置下的格式化结果
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithDifferentCultures()
    {
        // 保存当前区域设置
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            // Arrange
            decimal? price = 1234.56m;

            // 设置为美国区域
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            string usResult = Conv.ToStringOrDefault(price, "C", "未定价");

            // 设置为德国区域
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            string deResult = Conv.ToStringOrDefault(price, "C", "未定价");

            // Assert
            Assert.Contains("$", usResult);      // 美元符号
            Assert.Contains("€", deResult);      // 欧元符号
            Assert.NotEqual(usResult, deResult); // 结果应该不同
        }
        finally
        {
            // 恢复原始区域设置
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试 - ToStringOrDefault - 带格式 - 无效格式字符串
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithInvalidFormat()
    {
        // Arrange
        decimal? price = 1234.56m;

        // Act & Assert
        // 无效的格式字符串应该抛出异常
        Assert.Throws<FormatException>(() => Conv.ToStringOrDefault(price, "Z", "未定价"));
    }

    /// <summary>
    /// 测试 - ToStringOrDefault - 极值情况
    /// </summary>
    [Fact]
    public void Test_ToStringOrDefault_WithExtremeValues()
    {
        // Arrange
        int? maxInt = int.MaxValue;
        int? minInt = int.MinValue;
        double? maxDouble = double.MaxValue;
        double? minDouble = double.MinValue;

        // Act
        string maxIntResult = Conv.ToStringOrDefault(maxInt, "默认值");
        string minIntResult = Conv.ToStringOrDefault(minInt, "默认值");
        string maxDoubleResult = Conv.ToStringOrDefault(maxDouble, "默认值");
        string minDoubleResult = Conv.ToStringOrDefault(minDouble, "默认值");

        // Assert
        Assert.Equal(int.MaxValue.ToString(), maxIntResult);
        Assert.Equal(int.MinValue.ToString(), minIntResult);
        Assert.Equal(double.MaxValue.ToString(), maxDoubleResult);
        Assert.Equal(double.MinValue.ToString(), minDoubleResult);
    }

    #endregion

    #endregion

    #region ToRMB

    /// <summary>
    /// 测试 - 转换为人民币大写金额
    /// </summary>
    [Theory]
    [InlineData(null, default)]
    [InlineData(1, "壹元")]
    [InlineData(1.2, "壹元贰角")]
    [InlineData(1.23, "壹元贰角叁分")]
    [InlineData(1.234, "壹元贰角叁分")]
    [InlineData(1.05, "壹元零伍分")]
    [InlineData(2, "贰元")]
    [InlineData(3, "叁元")]
    [InlineData(4, "肆元")]
    [InlineData(5, "伍元")]
    [InlineData(6, "陆元")]
    [InlineData(7, "柒元")]
    [InlineData(8, "捌元")]
    [InlineData(9, "玖元")]
    [InlineData("10", "壹拾元")]
    [InlineData("10.2", "壹拾元贰角")]
    [InlineData("10.23", "壹拾元贰角叁分")]
    [InlineData("10.234", "壹拾元贰角叁分")]
    [InlineData("100", "壹佰元")]
    [InlineData("1000", "壹仟元")]
    [InlineData("10000", "壹万元")]
    [InlineData(100000, "壹拾万元")]
    [InlineData(1000000, "壹佰万元")]
    [InlineData(10000000, "壹仟万元")]
    [InlineData(100000000, "壹亿元")]
    public void Test_ToRMB(object input, string result)
    {
        Assert.Equal(result, Conv.ToRMB(input));
    }

    #endregion

    #region 测试特殊情况和边界条件

    /// <summary>
    /// 测试 - 特殊字符串转换
    /// </summary>
    [Fact]
    public void Test_SpecialStringConversion()
    {
        // 测试空格字符串
        var spaceStr = "   ";
        Assert.Null(Conv.To<string>(spaceStr));
        Assert.Equal(0, Conv.ToInt(spaceStr));
        Assert.Null(Conv.ToIntOrNull(spaceStr));

        // 测试超大数字
        var largeNumber = "99999999999999999999999999"; // 超出Int64范围
        Assert.Equal(0, Conv.ToInt(largeNumber));
        Assert.Null(Conv.ToIntOrNull(largeNumber));

        // 尝试解析超出范围的数字到decimal
        var decimalResult = Conv.ToDecimalOrNull(largeNumber);
        Assert.NotNull(decimalResult);

        // 测试非法格式 - 日期时间
        var invalidDate = "2023-13-32"; // 不存在的月份和日期
        Assert.Equal(DateTime.MinValue, Conv.ToDate(invalidDate));
        Assert.Null(Conv.ToDateOrNull(invalidDate));
    }

    /// <summary>
    /// 测试 - 跨类型转换
    /// </summary>
    [Fact]
    public void Test_CrossTypeConversion()
    {
        // 数值到字符串
        int intValue = 100;
        Assert.Equal("100", Conv.To<string>(intValue));

        // 字符串到数值
        string strValue = "100";
        Assert.Equal(100, Conv.To<int>(strValue));
        Assert.Equal(100.0, Conv.To<double>(strValue));

        // 浮点数到整数
        double doubleValue = 123.45;
        Assert.Equal(123, Conv.To<int>(doubleValue));

        // 字符串到日期
        string dateStr = "2023-01-15";
        Assert.Equal(new DateTime(2023, 1, 15), Conv.To<DateTime>(dateStr));

        // 数字到布尔值
        Assert.True(Conv.ToBool(1));
        Assert.False(Conv.ToBool(0));
    }

    /// <summary>
    /// 测试 - 区域相关转换
    /// </summary>
    [Fact]
    public void Test_CultureSpecificConversion()
    {
        // 保存当前文化
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            // 设置为美国文化
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            // 美国文化下的货币格式
            decimal money = 1234.56m;
            var formattedMoney = money.ToString("C", CultureInfo.CurrentCulture); // $1,234.56
            Assert.Contains("$", formattedMoney);

            // 转换带逗号的数字字符串
            var numWithCommas = "1,234.56";
            var decimalResult = Conv.ToDecimalOrNull(numWithCommas);
            Assert.Equal(1234.56m, decimalResult);

            // 设置为德国文化
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            // 德国文化下，逗号是小数点，句点是千位分隔符
            var germanFormat = "1.234,56";
            var germanDecimal = Conv.ToDecimalOrNull(germanFormat);
            Assert.Equal(1234.56m, germanDecimal);
        }
        finally
        {
            // 恢复原始文化
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region 测试性能和健壮性

    /// <summary>
    /// 测试 - 大量数据处理
    /// </summary>
    [Fact]
    public void Test_BulkDataProcessing()
    {
        // 创建大量的GUID字符串
        var largeGuidList = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            largeGuidList.Add(Guid.NewGuid().ToString());
        }

        // 将列表合并为逗号分隔的字符串
        var guidString = string.Join(",", largeGuidList);

        // 测试转换为GUID列表
        var resultList = Conv.ToGuidList(guidString);

        // 验证结果
        Assert.Equal(1000, resultList.Count);
        for (int i = 0; i < resultList.Count; i++)
        {
            Assert.Equal(Guid.Parse(largeGuidList[i]), resultList[i]);
        }
    }

    /// <summary>
    /// 测试 - 异常处理和容错性
    /// </summary>
    [Fact]
    public void Test_ExceptionHandlingAndFaultTolerance()
    {
        // 测试格式转换异常处理
        var invalidInput = "abc";

        // 这些调用不应抛出异常，而是返回默认值
        Assert.Equal(0, Conv.ToInt(invalidInput));
        Assert.Null(Conv.ToIntOrNull(invalidInput));
        Assert.Equal(default, Conv.ToFloat(invalidInput));
        Assert.Null(Conv.ToFloatOrNull(invalidInput));
        Assert.Equal(DateTime.MinValue, Conv.ToDate(invalidInput));
        Assert.Null(Conv.ToDateOrNull(invalidInput));

        // 测试类型转换异常处理
        var nonConvertibleObject = new object();
        Assert.Equal(default(int), Conv.To<int>(nonConvertibleObject));
        Assert.Null(Conv.To<int?>(nonConvertibleObject));
    }

    #endregion

    #region 测试与其他方法互操作的场景

    /// <summary>
    /// 测试 - 复杂对象和集合转换
    /// </summary>
    [Fact]
    public void Test_ComplexObjectAndCollectionConversion()
    {
        // 测试复杂对象转字典
        var complexObject = new
        {
            Id = 1,
            Name = "Test",
            Attributes = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } }
        };

        var dict = Conv.ToDictionary(complexObject);
        Assert.Equal(3, dict.Count);
        Assert.Equal(1, dict["Id"]);
        Assert.Equal("Test", dict["Name"]);
        Assert.IsType<Dictionary<string, string>>(dict["Attributes"]);

        // 测试集合转换 - 字符串到集合
        var numString = "1,2,3,4,5";
        var numList = Conv.ToList<int>(numString);
        Assert.Equal(5, numList.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, numList);
    }

    /// <summary>
    /// 测试 - 链式转换调用
    /// </summary>
    [Fact]
    public void Test_ChainedConversionCalls()
    {
        // 测试从字符串开始的转换链
        var input = "123.45";
        var doubleValue = Conv.ToDouble(input);
        var roundedInt = Conv.ToInt(doubleValue);
        Assert.Equal(123, roundedInt);

        // 测试从对象到字符串再到其他类型
        var obj = new { Value = 42 };
        var dict = Conv.ToDictionary(obj);
        var value = dict["Value"];
        var strValue = value.ToString();
        var intValue = Conv.ToInt(strValue);
        Assert.Equal(42, intValue);
    }

    #endregion
}