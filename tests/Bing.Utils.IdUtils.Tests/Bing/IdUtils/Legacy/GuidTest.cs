namespace Bing.IdUtils.Legacy;
/// <summary>
/// GUID 生成测试
/// </summary>
[Trait("IdUtilsUT", "GuidProvider.Create")]
public class GuidTest
{
    /// <summary>
    /// 获取当前测试的输出助手。
    /// </summary>
    protected ITestOutputHelper Output { get; }
    /// <summary>
    /// 初始化一个 <see cref="GuidTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">当前测试的输出助手。</param>
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

    /// <summary>
    /// 验证 PostgreSQL COMB 的不重复模式消费入口使用 UTC 毫秒时间戳。
    /// </summary>
    /// <remarks>Unix COMB 只编码毫秒，因此同一毫秒内的连续时间戳可以相等。</remarks>
    [Fact]
    public void Test_Create_PostgreSqlNoRepeat_EmbedsUtcMillisecondTimestamps()
    {
        var firstId = GuidProvider.Create(GuidStyle.PostgreSqlTimeStampStyle, NoRepeatMode.On);
        var secondId = GuidProvider.Create(GuidStyle.PostgreSqlTimeStampStyle, NoRepeatMode.On);

        firstId.ShouldNotBe(Guid.Empty);
        secondId.ShouldNotBe(Guid.Empty);

        var decoder = global::Bing.IdUtils.CombImplements.InternalCombImplementProxy.PostgreSql;
        var firstTimestamp = decoder.GetTimeStamp(firstId);
        var secondTimestamp = decoder.GetTimeStamp(secondId);

        firstTimestamp.Kind.ShouldBe(DateTimeKind.Utc);
        secondTimestamp.Kind.ShouldBe(DateTimeKind.Utc);
        (firstTimestamp.Ticks % TimeSpan.TicksPerMillisecond).ShouldBe(0);
        (secondTimestamp.Ticks % TimeSpan.TicksPerMillisecond).ShouldBe(0);
        secondTimestamp.Ticks.ShouldBeGreaterThanOrEqualTo(firstTimestamp.Ticks);
    }

    /// <summary>
    /// 验证固定 UTC 时间经 NoRepeat 工厂和 PostgreSQL COMB 编解码后按毫秒量化。
    /// </summary>
    [Fact]
    public void Test_PostgreSqlNoRepeatFactory_EncodesFixedUtcTimestampsToMilliseconds()
    {
        var factory = new global::Bing.Date.NoRepeatTimeStampFactory();
        var fixedBase = new DateTime(2024, 1, 1, 12, 34, 56, 123, DateTimeKind.Utc).AddTicks(4567);
        var provider = new global::Bing.IdUtils.CombImplements.Providers.PostgreSqlCombProvider(
            new global::Bing.IdUtils.CombImplements.Strategies.UnixDateTimeStrategy(),
            () => factory.GetTimeStamp(fixedBase));
        var fixedGuid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");

        var firstId = provider.Create(fixedGuid);
        var secondId = provider.Create(fixedGuid);
        var firstTimestamp = provider.GetTimeStamp(firstId);
        var secondTimestamp = provider.GetTimeStamp(secondId);
        var firstExpectedTicks = new DateTime(2024, 1, 1, 12, 34, 56, 123, DateTimeKind.Utc).Ticks;
        var secondExpectedTicks = new DateTime(2024, 1, 1, 12, 34, 56, 127, DateTimeKind.Utc).Ticks;

        firstTimestamp.Ticks.ShouldBe(firstExpectedTicks);
        firstTimestamp.Kind.ShouldBe(DateTimeKind.Utc);
        secondTimestamp.Ticks.ShouldBe(secondExpectedTicks);
        secondTimestamp.Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// 验证 GUID 非空，并将结果写入测试输出。
    /// </summary>
    /// <param name="id">要验证并输出的 GUID。</param>
    private void Test_ShouldNotBeEmpty(Guid id)
    {
        id.ShouldNotBe(Guid.Empty);
        var result = id.ToString();
        result.ShouldNotBeEmpty();
        Output.WriteLine(result);
    }
}
