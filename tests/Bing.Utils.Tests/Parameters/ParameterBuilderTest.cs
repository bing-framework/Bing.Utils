using Bing.Tests.Samples;
using Bing.Utils.Parameters;
namespace Bing.Utils.Tests.Parameters;
/// <summary>
/// 验证参数生成器对不同值类型的格式化行为。
/// </summary>
public class ParameterBuilderTest
{
    /// <summary>
    /// 保存当前测试使用的参数生成器实例。
    /// </summary>
    private readonly ParameterBuilder _builder;
    /// <summary>
    /// 初始化一个 <see cref="ParameterBuilderTest"/> 类型的实例。
    /// </summary>
    public ParameterBuilderTest() => _builder = new ParameterBuilder();
    /// <summary>
    /// 验证键为空时不生成参数。
    /// </summary>
    /// <param name="key">待添加的参数键。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Test_Add_KeyIsEmpty(string key)
    {
        Assert.Empty(_builder.Add(key, "b").Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证值为空时不生成参数。
    /// </summary>
    /// <param name="value">待添加的参数值。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Test_Add_ValueIsEmpty(string value)
    {
        Assert.Empty(_builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证格式化前会去除键两端的空格。
    /// </summary>
    [Fact]
    public void Test_Add_TrimKey()
    {
        Assert.Equal("a:1", _builder.Add(" a ", "1").Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证格式化前会去除值两端的空格。
    /// </summary>
    [Fact]
    public void Test_Add_TrimValue()
    {
        Assert.Equal("a:1", _builder.Add("a", " 1 ").Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证字符串参数的格式化结果。
    /// </summary>
    [Fact]
    public void Test_Add_String()
    {
        Assert.Equal("a:1", _builder.Add("a", "1").Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证多个字符串参数按格式化器规则连接。
    /// </summary>
    [Fact]
    public void Test_Add_String_2()
    {
        Assert.Equal("a:1|b:2", _builder.Add("a", " 1").Add("b ", "2 ").Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证日期时间参数按格式化器规则输出。
    /// </summary>
    [Fact]
    public void Test_Add_DateTime()
    {
        DateTime value = new DateTime(2000, 10, 10, 10, 10, 10);
        Assert.Equal("a:2000-10-10 10:10:10", _builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证有值的可空日期时间参数正常输出。
    /// </summary>
    [Fact]
    public void Test_Add_DateTime_Nullable()
    {
        DateTime? value = new DateTime(2000, 10, 10, 10, 10, 10);
        Assert.Equal("a:2000-10-10 10:10:10", _builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证空可空日期时间参数不生成内容。
    /// </summary>
    [Fact]
    public void Test_Add_DateTime_Nullable_Null()
    {
        DateTime? value = null;
        Assert.Empty(_builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证整数参数按格式化器规则输出。
    /// </summary>
    [Fact]
    public void Test_Add_Int()
    {
        int value = 1;
        Assert.Equal("a:1", _builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证有值的可空整数参数正常输出。
    /// </summary>
    [Fact]
    public void Test_Add_Int_Nullable()
    {
        int? value = 1;
        Assert.Equal("a:1", _builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证空可空整数参数不生成内容。
    /// </summary>
    [Fact]
    public void Test_Add_Int_Nullable_Null()
    {
        int? value = null;
        Assert.Empty(_builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证布尔参数按格式化器规则输出。
    /// </summary>
    [Fact]
    public void Test_Add_Bool()
    {
        bool value = true;
        Assert.Equal("a:true", _builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证有值的可空布尔参数正常输出。
    /// </summary>
    [Fact]
    public void Test_Add_Bool_Nullable()
    {
        bool? value = false;
        Assert.Equal("a:false", _builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证空可空布尔参数不生成内容。
    /// </summary>
    [Fact]
    public void Test_Add_Bool_Nullable_Null()
    {
        bool? value = null;
        Assert.Empty(_builder.Add("a", value).Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证重复键会使用后添加的值。
    /// </summary>
    [Fact]
    public void Test_Add_Exist()
    {
        Assert.Equal("a:2", _builder.Add("a", "1").Add("a", "2").Result(new ParameterFormatterSample()));
    }
    /// <summary>
    /// 验证参数集合可转换为 JSON 对象。
    /// </summary>
    [Fact]
    public void Test_ToJson()
    {
        Assert.Equal("{\"a\":\"true\",\"b\":\"2000-10-10 10:10:10\"}", _builder.Add("a", true).Add("b", new DateTime(2000, 10, 10, 10, 10, 10)).ToJson());
    }
}
