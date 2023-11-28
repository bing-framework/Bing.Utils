using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using Bing.Helpers;
using Bing.Serialization.SystemTextJson;
using BingUtilsUT.Samples;

namespace BingUtilsUT.JsonUT;

/// <summary>
/// System.Text.Json测试
/// </summary>
[Trait("JsonUT", "System.Text.Json")]
public class SystemTestJsonTest
{
    /// <summary>
    /// 测试 - 转换成Json字符串 - 转换为本地日期
    /// </summary>
    [Fact]
    public void Test_ToJson_Local()
    {
        Time.UseUtc(false);
        var result = new StringBuilder();
        result.Append("{");
        result.Append("\"Name\":\"a\",");
        result.Append("\"nickname\":\"b\",");
        result.Append("\"firstName\":\"c\",");
        result.Append("\"Date\":\"2012-12-12 20:12:12\",");
        result.Append("\"UtcDate\":\"2012-12-12 20:12:12\",");
        result.Append("\"Age\":1,");
        result.Append("\"IsShow\":true,");
        result.Append("\"Enum\":0");
        result.Append("}");
        var sample = JsonTestSample.Create();
        Assert.Equal(result.ToString(), Json.ToJson(sample));
    }

    /// <summary>
    /// 测试 - 转换成Json字符串 - 移除双引号
    /// </summary>
    [Fact]
    public void Test_ToJson_RemoveQuotationMarks()
    {
        var result = new StringBuilder();
        result.Append("{");
        result.Append("Name:a,");
        result.Append("nickname:b,");
        result.Append("firstName:c,");
        result.Append("Date:2012-12-12 20:12:12,");
        result.Append("UtcDate:2012-12-12 20:12:12,");
        result.Append("Age:1,");
        result.Append("IsShow:true,");
        result.Append("Enum:0");
        result.Append("}");
        var sample = JsonTestSample.Create();
        Assert.Equal(result.ToString(), Json.ToJson(sample, removeQuotationMarks: true));
    }

    /// <summary>
    /// 测试 - 转换成Json字符串 - 将双引号转换成单引号
    /// </summary>
    [Fact]
    public void Test_ToJson_ToSingleQuotes()
    {
        var result = new StringBuilder();
        result.Append("{");
        result.Append("'Name':'a',");
        result.Append("'nickname':'b',");
        result.Append("'firstName':'c',");
        result.Append("'Date':'2012-12-12 20:12:12',");
        result.Append("'UtcDate':'2012-12-12 20:12:12',");
        result.Append("'Age':1,");
        result.Append("'IsShow':true,");
        result.Append("'Enum':0");
        result.Append("}");
        var sample = JsonTestSample.Create();
        Assert.Equal(result.ToString(), Json.ToJson(sample, toSingleQuotes: true));
    }

    /// <summary>
    /// 测试 - 转换成Json字符串 - 序列化接口
    /// </summary>
    [Fact]
    public void Test_ToJson_Interface()
    {
        Time.UseUtc(false);
        var result = new StringBuilder();
        result.Append("{");
        result.Append("\"Name\":\"a\",");
        result.Append("\"nickname\":\"b\",");
        result.Append("\"firstName\":\"c\",");
        result.Append("\"Date\":\"2012-12-12 20:12:12\",");
        result.Append("\"UtcDate\":\"2012-12-12 20:12:12\",");
        result.Append("\"Age\":1,");
        result.Append("\"IsShow\":true,");
        result.Append("\"Enum\":0");
        result.Append("}");
        var sample = JsonTestSample.CreateToInterface();
        Assert.Equal(result.ToString(), Json.ToJson(sample));
    }

    /// <summary>
    /// 测试 - 转换为对象
    /// </summary>
    [Fact]
    public void Test_ToObject()
    {
        var sample = Json.ToObject<JsonTestSample>("{\"Name\":\"a\"}");
        Assert.Equal("a", sample.Name);
    }

    /// <summary>
    /// 测试 - 转换成Json字符串 - 不转义中文字符
    /// </summary>
    [Fact]
    public void Test_ToJson_Encoder()
    {
        var result = new StringBuilder();
        result.Append("{");
        result.Append("\"Name\":\"哈哈\",");
        result.Append("\"nickname\":\"b\",");
        result.Append("\"firstName\":\"c\",");
        result.Append("\"Date\":\"2012-12-12 12:12:12\",");
        result.Append("\"Age\":1,");
        result.Append("\"IsShow\":true,");
        result.Append("\"Enum\":0");
        result.Append("}");
        var sample = new JsonTestSample
        {
            Name = "哈哈",
            nickname = "b",
            FirstName = "c",
            Value = null,
            Date = Conv.ToDate("2012-12-12 12:12:12"),
            Age = 1,
            IsShow = true
        };
        Assert.Equal(result.ToString(), Json.ToJson(sample, new JsonSerializerOptions
        {
#if NET5_0_OR_GREATER
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#else
            IgnoreNullValues = true,
#endif
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new NullableDateTimeJsonConverter() }
        }));
    }

    /// <summary>
    /// 测试 - 转换成Json字符串 - 枚举转换器
    /// </summary>
    [Fact]
    public void Test_ToJson_Enum()
    {
        var sample = new JsonTestSample
        {
            Enum = TestEnum.Test2,
            NullableEnum = TestEnum.Test2
        };
        var json = Json.ToJson(sample,
            new JsonSerializerOptions
            {
#if NET5_0_OR_GREATER
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#else
                IgnoreNullValues = true,
#endif
                Converters = { new EnumJsonConverterFactory() }
            });

        var result = Json.ToObject<JsonTestSample>(json, new JsonSerializerOptions
        {
            Converters = { new EnumJsonConverterFactory() }
        });
        Assert.True(result.Enum == TestEnum.Test2);
        Assert.True(result.NullableEnum == TestEnum.Test2);
    }

}