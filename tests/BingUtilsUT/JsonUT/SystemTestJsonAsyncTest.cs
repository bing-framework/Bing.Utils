using Bing.Helpers;

namespace BingUtilsUT.JsonUT;

#if NET6_0_OR_GREATER

/// <summary>
/// System.Text.Json测试
/// </summary>
[Trait("JsonUT", "System.Text.Json")]
public class SystemTestJsonAsyncTest
{
    /// <summary>
    /// 测试 - 转换成Json字符串
    /// </summary>
    [Fact]
    public async Task Test_ToJsonAsync()
    {
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
        var json = await Json.ToJsonAsync(sample);
        Assert.Equal(result.ToString(), json);
    }

    /// <summary>
    /// 测试 - 转换为对象
    /// </summary>
    [Fact]
    public async Task Test_ToObjectAsync()
    {
        var sample = await Json.ToObjectAsync<JsonTestSample>("{\"Name\":\"a\"}");
        Assert.Equal("a", sample.Name);
    }
}

#endif
