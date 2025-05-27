using Bing.Tests.Samples.Json;
using Bing.Tests.XUnitHelpers;
using Bing.Utils.Json;
using Bing.Utils.Json.Converters;
using Newtonsoft.Json;

namespace Bing.Utils.Tests.Json;

/// <summary>
/// Json测试
/// </summary>
public class JsonHelperTest : TestBase
{
    private static readonly DateTime DefaultDate = new(1900, 1, 1);

    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    public JsonHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试循环引用序列化
    /// </summary>
    [Fact]
    public void Test_Loop()
    {
        A a = new A { Name = "a" };
        B b = new B { Name = "b" };
        C c = new C { Name = "c" };
        a.B = b;
        b.C = c;
        c.A = a;
        AssertHelper.Throws<JsonSerializationException>(() => JsonHelper.ToJson(c));
    }

    /// <summary>
    /// 转成Json,验证空
    /// </summary>
    [Fact]
    public void Test_ToJson_Null()
    {
        Assert.Empty(JsonHelper.ToJson(null));
    }

    /// <summary>
    /// 测试转成Json
    /// </summary>
    [Fact]
    public void Test_ToJson()
    {
        var result = new StringBuilder();
        result.Append("{");
        result.Append("\"Name\":\"a\",");
        result.Append("\"nickname\":\"b\",");
        result.Append("\"Value\":null,");
        result.Append("\"Date\":\"2012/1/1 0:00:00\",");
        result.Append("\"Age\":1,");
        result.Append("\"isShow\":true");
        result.Append("}");
        var actualData = JsonTestSample.Create();
        actualData.Date = DateTime.Parse(actualData.Date).ToString("yyyy/M/d 0:00:00");
        Assert.Equal(result.ToString(), JsonHelper.ToJson(actualData));
    }

    /// <summary>
    /// 测试转成Json，将双引号转成单引号
    /// </summary>
    [Fact]
    public void Test_ToJson_ToSingleQuotes()
    {
        var result = new StringBuilder();
        result.Append("{");
        result.Append("'Name':'a',");
        result.Append("'nickname':'b',");
        result.Append("'Value':null,");
        result.Append("'Date':'2012/1/1 0:00:00',");
        result.Append("'Age':1,");
        result.Append("'isShow':true");
        result.Append("}");

        var actualData = JsonTestSample.Create();
        actualData.Date = DateTime.Parse(actualData.Date).ToString("yyyy/M/d 0:00:00");
        Assert.Equal(result.ToString(), JsonHelper.ToJson(actualData, true));
    }

    /// <summary>
    /// 测试转成对象
    /// </summary>
    [Fact]
    public void Test_ToObject()
    {
        var customer = JsonHelper.ToObject<JsonTestSample>("{\"Name\":\"a\"}");
        Assert.Equal("a", customer.Name);
    }

    /// <summary>
    /// 测试 - 确保 DateTime 等于 1900-01-01 时，序列化结果为 null
    /// </summary>
    [Fact]
    public void Serialize_1900DateTime_ShouldBeNull()
    {
        var sample = new JsonDateTimeSample
        {
            ConfirmTime = DefaultDate,
            OptionalTime = DefaultDate
        };

        var json = JsonConvert.SerializeObject(sample);
        Output.WriteLine(json);
        Assert.Contains("\"ConfirmTime\":null", json);
        Assert.Contains("\"OptionalTime\":null", json);
    }

    /// <summary>
    /// 测试 - 可空时间字段为 null 时，序列化结果应为 null；非 1900 日期应正常序列化
    /// </summary>
    [Fact]
    public void Serialize_NullOptionalTime_ShouldBeNull()
    {
        var sample = new JsonDateTimeSample
        {
            ConfirmTime = new DateTime(2025,1,1),
            OptionalTime = null
        };

        var json = JsonConvert.SerializeObject(sample);
        Output.WriteLine(json);
        Assert.Contains("\"OptionalTime\":null", json);
        Assert.Contains("\"ConfirmTime\":\"2025-01-01T00:00:00\"", json);
    }

    /// <summary>
    /// 测试 - JSON 中为 null 的时间字段，反序列化应还原为 1900-01-01
    /// </summary>
    [Fact]
    public void Deserialize_NullJson_ShouldReturnDefaultDate()
    {
        var json = "{\"ConfirmTime\":null,\"OptionalTime\":null}";
        var sample = JsonConvert.DeserializeObject<JsonDateTimeSample>(json);
        Output.WriteLine(json);
        Assert.Equal(DefaultDate, sample.ConfirmTime);
        Assert.Equal(DefaultDate, sample.OptionalTime);
    }

    /// <summary>
    /// 测试 - JSON 中为正常日期字符串时，能够正确反序列化为 DateTime 对象
    /// </summary>
    [Fact]
    public void Deserialize_NormalDate_ShouldParseCorrectly()
    {
        var json = "{\"ConfirmTime\":\"2023-12-12 10:30:00\",\"OptionalTime\":\"2023-12-12 10:30:00\"}";

        var dto = JsonConvert.DeserializeObject<JsonDateTimeSample>(json);

        var expected = new DateTime(2023, 12, 12, 10, 30, 0);
        Output.WriteLine(json);
        Assert.Equal(expected, dto.ConfirmTime);
        Assert.Equal(expected, dto.OptionalTime);
    }

    /// <summary>
    /// 测试 - JSON 中为正常日期字符串时，能够正确反序列化为 DateTime 对象
    /// </summary>
    [Fact]
    public void Deserialize_NormalDate_ShouldParseCorrectly_1()
    {
        var json = "{\"ConfirmTime\":\"2023-12-12T10:30:00\",\"OptionalTime\":\"2023-12-12T10:30:00\"}";

        var dto = JsonConvert.DeserializeObject<JsonDateTimeSample>(json);

        var expected = new DateTime(2023, 12, 12, 10, 30, 0);
        Output.WriteLine(json);
        Assert.Equal(expected, dto.ConfirmTime);
        Assert.Equal(expected, dto.OptionalTime);
    }

    /// <summary>
    /// 测试 - JSON 中为正常日期字符串时，能够正确反序列化为 DateTime 对象
    /// </summary>
    [Fact]
    public void Deserialize_NormalDate_ShouldParseCorrectly_2()
    {
        var json = "{\"ConfirmTime\":\"2023-12-12T10:30:00\",\"OptionalTime\":\"1900-01-01T00:00:00\"}";

        var dto = JsonConvert.DeserializeObject<JsonDateTimeSample>(json);

        var expected = new DateTime(2023, 12, 12, 10, 30, 0);
        Output.WriteLine(json);
        Assert.Equal(expected, dto.ConfirmTime);
        Assert.Equal(DefaultDate, dto.OptionalTime);
    }

    /// <summary>
    /// 测试 - JSON 中为正常日期字符串时，能够正确反序列化为 DateTime 对象
    /// </summary>
    [Fact]
    public void Deserialize_NormalDate_ShouldParseCorrectly_3()
    {
        var json = "{\"ConfirmTime\":\"2023-12-12T10:30:00\",\"OptionalTime\":null}";

        var dto = JsonConvert.DeserializeObject<JsonDateTimeSample>(json);

        var expected = new DateTime(2023, 12, 12, 10, 30, 0);
        Output.WriteLine(json);
        Assert.Equal(expected, dto.ConfirmTime);
        Assert.Equal(DefaultDate, dto.OptionalTime);
    }

    /// <summary>
    /// 测试 - 1900-01-01 的时间序列化为 null 后反序列化应还原为 1900-01-01
    /// </summary>
    [Fact]
    public void RoundTrip_Serialization_Deserialization_ShouldMaintainSemantics()
    {
        var original = new JsonDateTimeSample
        {
            ConfirmTime = DefaultDate,
            OptionalTime = null
        };

        var json = JsonConvert.SerializeObject(original);
        var deserialized = JsonConvert.DeserializeObject<JsonDateTimeSample>(json);
        Output.WriteLine(json);
        Assert.Equal(DefaultDate, deserialized.ConfirmTime);
        Assert.Equal(DefaultDate, deserialized.OptionalTime);
    }

    private class JsonDateTimeSample
    {
        [JsonConverter(typeof(DateTimeNullTo1900Converter))]
        public DateTime ConfirmTime { get; set; }

        [JsonConverter(typeof(DateTimeNullTo1900Converter))]
        public DateTime? OptionalTime { get; set; }
    }
}