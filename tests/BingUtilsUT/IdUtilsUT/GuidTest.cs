using Bing.IdUtils;

namespace BingUtilsUT.IdUtilsUT;

/// <summary>
/// GUID 生成测试
/// </summary>
[Trait("IdUtilsUT", "GuidProvider.Create")]
public class GuidTest
{
    /// <summary>
    /// 测试输出帮助类
    /// </summary>
    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// 测试初始化
    /// </summary>
    public GuidTest(ITestOutputHelper output)
    {
        Output = output;
    }

    /// <summary>
    /// 测试 - 随机GUID
    /// </summary>
    [Fact]
    public void Test_Random()
    {
        Test_ShouldNotBeEmpty(GuidProvider.CreateRandom());
    }

    /// <summary>
    /// 测试 - 创建GUID
    /// </summary>
    [Fact]
    public void Test_Create()
    {
        Test_ShouldNotBeEmpty(GuidProvider.Create());
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.NormalStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.UnixStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.SqlStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.LegacySqlStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.PostgreSqlStyle));

        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.BasicStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.TimeStampStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.UnixTimeStampStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SqlTimeStampStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.LegacySqlTimeStampStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.PostgreSqlTimeStampStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.CombStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsStringStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsBinaryStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsEndStyle));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.EquifaxStyle)); 
    }

    /// <summary>
    /// 测试 - 创建GUID - 不重复模式
    /// </summary>
    [Fact]
    public void Test_Create_With_RepeatMode()
    {
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.NormalStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.UnixStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.SqlStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.LegacySqlStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.PostgreSqlStyle, NoRepeatMode.On));

        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.BasicStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.TimeStampStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.UnixTimeStampStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SqlTimeStampStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.LegacySqlTimeStampStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.PostgreSqlTimeStampStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.CombStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsStringStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsBinaryStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsEndStyle, NoRepeatMode.On));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.EquifaxStyle, NoRepeatMode.On));

        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.NormalStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.UnixStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.SqlStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.LegacySqlStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(CombStyle.PostgreSqlStyle, NoRepeatMode.Off));

        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.BasicStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.TimeStampStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.UnixTimeStampStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SqlTimeStampStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.LegacySqlTimeStampStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.PostgreSqlTimeStampStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.CombStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsStringStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsBinaryStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.SequentialAsEndStyle, NoRepeatMode.Off));
        Test_ShouldNotBeEmpty(GuidProvider.Create(GuidStyle.EquifaxStyle, NoRepeatMode.Off));
    }

    private void Test_ShouldNotBeEmpty(Guid id)
    {
        var result = id.ToString();
        result.ShouldNotBeEmpty();
        Output.WriteLine(result);
    }
}