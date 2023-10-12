using System.Collections.Generic;
using Bing.Helpers;
using Bing.Tests.Samples;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Helpers;

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
    /// 测试 - 转换为布尔型
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="result">结果</param>
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1A", false)]
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
    public void Test_ToBool(object input, bool result)
    {
        Assert.Equal(result, Conv.ToBool(input));
    }

    #endregion

    #region ToBoolOrNull

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
        Assert.True(Conv.To<bool>("true"));
        Assert.Equal(new DateTime(2000, 1, 1), Conv.To<DateTime>("2000-1-1"));
        Assert.Equal(new DateTime(2000, 1, 1), Conv.To<DateTime?>("2000-1-1"));
        var guid = Guid.NewGuid();
        Assert.Equal(guid.ToString(), Conv.To<string>(guid));
        Assert.Equal(EnumSample.C, Conv.To<EnumSample>("c"));
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
        var json = Bing.Helpers.Json.ToJson(dic);
        dic = Bing.Helpers.Json.ToObject<Dictionary<string, object>>(json);

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
            Test3 = new Sample3 { StringValue = "a" },
            TestList = new List<Sample3> { new() { StringValue = "a" }, new() { StringValue = "b" } }
        };
        var result = Conv.ToDictionary(sample);
        Assert.Equal(9, result.Count);
        Assert.Equal("Display", result["Display"]);
        Assert.Equal(2, result["IntValue"]);
    }

    /// <summary>
    /// 测试 - 对象转换为字典(属性名-属性值) - 传入字典
    /// </summary>
    [Fact]
    public void Test_ToDictionary_2()
    {
        var content = new Dictionary<string, object>();
        content.Add("Code", "a");
        content.Add("Price", 0);
        var result = Conv.ToDictionary(content);
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result["Code"]);
        Assert.Equal(0, result["Price"]);
    }

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
}