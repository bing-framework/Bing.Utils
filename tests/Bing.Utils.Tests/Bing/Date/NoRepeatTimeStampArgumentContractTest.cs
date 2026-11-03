namespace Bing.Date;

/// <summary>
/// 验证时间戳异常只报告公开参数，并保持失败原子性。
/// </summary>
public class NoRepeatTimeStampArgumentContractTest
{
    /// <summary>
    /// 验证指定基准入口在序列耗尽时报告 baseTime，且不推进状态。
    /// </summary>
    [Fact]
    public void BaseTime_ExhaustedSequence_ReportsBaseTimeWithoutChangingState()
    {
        var factory = new NoRepeatTimeStampFactory(0.0001);
        var maximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        factory.GetTimeStamp(maximum);
        var count = factory.GeneratedCount;
        Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamp(maximum)).ParamName.ShouldBe("baseTime");
        factory.GeneratedCount.ShouldBe(count);
    }

    /// <summary>
    /// 验证无时间参数的入口在序列耗尽时不暴露内部变量名。
    /// </summary>
    [Fact]
    public void ClockAndPeek_ExhaustedSequence_DoNotInventParameterNames()
    {
        var factory = new NoRepeatTimeStampFactory(0.0001);
        factory.GetTimeStamp(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
        var count = factory.GeneratedCount;
        Action[] calls =
        {
            () => factory.GetTimeStamp(), () => factory.GetUtcTimeStamp(),
            () => factory.GetTimeStampObject(), () => factory.GetUtcTimeStampObject(),
            () => factory.GetUnixTimeStampObject(), () => factory.GetUtcUnixTimeStampObject(),
            () => factory.PeekNextTimeStamp(), () => factory.PeekNextTimeStamp(true)
        };
        foreach (var call in calls)
        {
            Should.Throw<ArgumentOutOfRangeException>(call).ParamName.ShouldBeNull();
            factory.GeneratedCount.ShouldBe(count);
        }
    }

    /// <summary>
    /// 验证首项或批次尾部溢出均报告 count，不消耗已生成的序列。
    /// </summary>
    /// <param name="remainingTicks">尚可生成的 tick 数。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Batch_Overflow_ReportsCountWithoutChangingState(int remainingTicks)
    {
        var factory = new NoRepeatTimeStampFactory(0.0001);
        factory.GetTimeStamp(new DateTime(DateTime.MaxValue.Ticks - remainingTicks, DateTimeKind.Utc));
        var count = factory.GeneratedCount;
        Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamps(2, true)).ParamName.ShouldBe("count");
        Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStampObjects(2, true)).ParamName.ShouldBe("count");
        factory.GeneratedCount.ShouldBe(count);
    }

    /// <summary>
    /// 验证本地显示越界的异常标识公开入口，并在失败后仍可取出 UTC 候选值。
    /// </summary>
    [Fact]
    public void LocalOutput_AtUtcMaximum_ReportsPublicParameterWithoutChangingState()
    {
        var maximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        var factory = new NoRepeatTimeStampFactory(0.0001);
        factory.GetTimeStamp(maximum.AddTicks(-1));
        var count = factory.GeneratedCount;
        if (maximum.ToLocalTime().ToUniversalTime().Ticks == maximum.Ticks)
        {
            // UTC 或负偏移时区可以完整显示上界，仍验证预览不发布状态。
            factory.PeekNextTimeStamp().ToUniversalTime().Ticks.ShouldBe(maximum.Ticks);
            factory.GeneratedCount.ShouldBe(count);
        }
        else
        {
            // 在 Asia/Shanghai 等正偏移时区中，UTC 上界不能表示为本地时间。
            Should.Throw<ArgumentOutOfRangeException>(() => factory.PeekNextTimeStamp()).ParamName.ShouldBe("useUtc");
            Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamps(1)).ParamName.ShouldBe("useUtc");
            Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStampObjects(1)).ParamName.ShouldBe("useUtc");
            Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamp(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local))).ParamName.ShouldBe("baseTime");
            Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamp()).ParamName.ShouldBeNull();
            factory.GeneratedCount.ShouldBe(count);
        }
        factory.GetUtcTimeStamp().Ticks.ShouldBe(maximum.Ticks);
    }

    /// <summary>
    /// 验证本地基准转换超出 UTC 范围时以 baseTime 报告错误，不污染工厂。
    /// </summary>
    [Fact]
    public void BaseTime_OutsideUtcRange_ReportsBaseTimeWithoutChangingState()
    {
        var minimumLocal = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local);
        var maximumLocal = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
        DateTime? invalid = null;
        if (TimeZoneInfo.Local.GetUtcOffset(minimumLocal) > TimeSpan.Zero)
            invalid = minimumLocal;
        else if (TimeZoneInfo.Local.GetUtcOffset(maximumLocal) < TimeSpan.Zero)
            invalid = maximumLocal;

        var factory = new NoRepeatTimeStampFactory(0.0001);
        if (invalid.HasValue)
        {
            Should.Throw<ArgumentOutOfRangeException>(() => factory.GetTimeStamp(invalid.Value)).ParamName.ShouldBe("baseTime");
            factory.GeneratedCount.ShouldBe(0);
        }
        var origin = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        factory.GetTimeStamp(origin).Ticks.ShouldBe(origin.Ticks);
    }
}
