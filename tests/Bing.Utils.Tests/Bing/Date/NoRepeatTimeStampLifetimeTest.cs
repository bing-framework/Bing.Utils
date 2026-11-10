namespace Bing.Date;

/// <summary>
/// 验证时间戳唯一性的实例和重置生命周期边界。
/// </summary>
public class NoRepeatTimeStampLifetimeTest
{
    /// <summary>
    /// 使用固定基准证明跨实例及 Reset 后可以重复，禁止误认为分布式唯一。
    /// </summary>
    [Fact]
    public void FixedOrigin_NewInstanceAndReset_StartIndependentSequences()
    {
        var origin = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstFactory = new NoRepeatTimeStampFactory(0.0001);
        var secondFactory = new NoRepeatTimeStampFactory(0.0001);
        var first = firstFactory.GetTimeStamp(origin);
        secondFactory.GetTimeStamp(origin).ShouldBe(first);
        firstFactory.GetTimeStamp(origin).Ticks.ShouldBe(first.Ticks + 1);
        firstFactory.Reset();
        firstFactory.GetTimeStamp(origin).Ticks.ShouldBe(first.Ticks);
    }
}
