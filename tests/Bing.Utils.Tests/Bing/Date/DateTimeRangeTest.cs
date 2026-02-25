namespace Bing.Date;
/// <summary>
/// 时间范围 测试
/// </summary>
public class DateTimeRangeTest
{
    /// <summary>
    /// 当前时间
    /// </summary>
    private static readonly DateTime _now = new(2024, 6, 25, 14, 37, 56, 78);
    /// <summary>
    /// 测试 - 构造函数 - 默认
    /// </summary>
    [Fact]
    public void Test_Ctor_Default()
    {
        var range = new DateTimeRange();
        range.StartTime.ShouldBe(DateTime.MinValue);
        range.EndTime.ShouldBe(DateTime.MaxValue);
        var now = new DateTime(2024, 1, 15, 13, 41, 22);
        range = new DateTimeRange(now.Date.AddDays(-1), now.Date.AddDays(2));
        range.StartTime.Day.ShouldBe(14);
        range.EndTime.Day.ShouldBe(17);
    }
    /// <summary>
    /// 测试 - 构造函数 - 指定开始时间和结束时间
    /// </summary>
    [Fact]
    public void Test_Ctor_ShouldOrderStartAndEnd()
    {
        var start = new DateTime(2025, 1, 2);
        var end = new DateTime(2025, 1, 1);
        var range = new DateTimeRange(start, end);
        Assert.Equal(end, range.StartTime);
        Assert.Equal(start, range.EndTime);
    }
    /// <summary>
    /// 测试 - 构造函数 - 指定开始时间和结束时间
    /// </summary>
    [Fact]
    public void Test_Ctor_DateTimeOffset_ShouldOrderStartAndEnd()
    {
        var start = new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var range = new DateTimeRange(start, end);
        Assert.Equal(end.DateTime, range.StartTime);
        Assert.Equal(start.DateTime, range.EndTime);
    }
    /// <summary>
    /// 测试 - 构造函数 - 使用 IDateTimeRange 接口
    /// </summary>
    [Fact]
    public void Test_Ctor_IDateTimeRange()
    {
        var originalRange = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var newRange = new DateTimeRange(originalRange);
        newRange.StartTime.ShouldBe(originalRange.StartTime);
        newRange.EndTime.ShouldBe(originalRange.EndTime);
    }
    /// <summary>
    /// 测试 - 构造函数 - 使用起始时间和持续时间
    /// </summary>
    [Fact]
    public void Test_Ctor_WithDuration()
    {
        var start = new DateTime(2024, 1, 1);
        var duration = TimeSpan.FromDays(5);
        var range = new DateTimeRange(start, duration);
        range.StartTime.ShouldBe(start);
        range.EndTime.ShouldBe(new DateTime(2024, 1, 6));
        range.TotalDays.ShouldBe(5);
    }
    /// <summary>
    /// 测试 - 构造函数 - 使用持续时间和结束时间
    /// </summary>
    [Fact]
    public void Test_Ctor_WithDurationAndEnd()
    {
        var end = new DateTime(2024, 1, 10);
        var duration = TimeSpan.FromDays(3);
        var range = new DateTimeRange(duration, end);
        range.EndTime.ShouldBe(end);
        range.StartTime.ShouldBe(new DateTime(2024, 1, 7));
        range.TotalDays.ShouldBe(3);
    }
    /// <summary>
    /// 测试 - 构造函数 - 负持续时间应抛出异常
    /// </summary>
    [Fact]
    public void Test_Ctor_NegativeDuration_ShouldThrowException()
    {
        var start = new DateTime(2024, 1, 1);
        var negativeDuration = TimeSpan.FromDays(-1);
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DateTimeRange(start, negativeDuration));
        exception.Message.ShouldContain("持续时间不能为负值");
        var end = new DateTime(2024, 1, 10);
        exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DateTimeRange(negativeDuration, end));
        exception.Message.ShouldContain("持续时间不能为负值");
    }
    /// <summary>
    /// 测试 - UTC 时间转换
    /// </summary>
    [Fact]
    public void Test_UtcTimeConversion()
    {
        var localStart = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var localEnd = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Local);
        var range = new DateTimeRange(localStart, localEnd);
        // 本地时间和 UTC 时间应该有时区差异
        range.StartTime.ShouldBe(localStart);
        range.UtcStartTime.ShouldNotBe(localStart);
        // 但 MinValue 和 MaxValue 应该保持原样
        var minMaxRange = new DateTimeRange();
        minMaxRange.StartTime.ShouldBe(DateTime.MinValue);
        minMaxRange.UtcStartTime.ShouldBe(DateTime.MinValue);
        minMaxRange.EndTime.ShouldBe(DateTime.MaxValue);
        minMaxRange.UtcEndTime.ShouldBe(DateTime.MaxValue);
    }
    /// <summary>
    /// 测试 - 属性
    /// </summary>
    [Fact]
    public void Test_Properties()
    {
        DateTimeRange.Today.StartTime.ShouldBeGreaterThan(DateTimeRange.Yesterday.EndTime);
        DateTimeRange.Today.EndTime.ShouldBeLessThan(DateTimeRange.Tomorrow.StartTime);
        DateTimeRange.ThisMonth.StartTime.Day.ShouldBe(1);
        DateTimeRange.LastMonth.StartTime.Day.ShouldBe(1);
        DateTimeRange.NextMonth.StartTime.Day.ShouldBe(1);
        DateTimeRange.ThisMonth.StartTime.ShouldBeGreaterThan(DateTimeRange.LastMonth.EndTime);
        DateTimeRange.ThisMonth.EndTime.ShouldBeLessThan(DateTimeRange.NextMonth.StartTime);
        DateTimeRange.ThisYear.StartTime.Month.ShouldBe(1);
        DateTimeRange.ThisYear.StartTime.Day.ShouldBe(1);
        DateTimeRange.ThisYear.StartTime.ShouldBeGreaterThan(DateTimeRange.LastYear.EndTime);
        DateTimeRange.ThisYear.EndTime.ShouldBeLessThan(DateTimeRange.NextYear.StartTime);
        DateTimeRange.Last7DaysExceptToday.EndTime.ShouldBeLessThan(DateTimeRange.Today.StartTime);
        DateTimeRange.Last30DaysExceptToday.EndTime.ShouldBeLessThan(DateTimeRange.Today.StartTime);
    }
    /// <summary>
    /// 测试 - 获取相差天数
    /// </summary>
    [Fact]
    public void Test_GetDays()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 4));
        range.GetDays().ShouldBe(3);
    }
    /// <summary>
    /// 测试 - 获取相差小时数
    /// </summary>
    [Fact]
    public void Test_GetHours()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 0, 0, 0), new DateTime(2024, 1, 1, 12, 0, 0));
        range.GetHours().ShouldBe(12);
    }
    /// <summary>
    /// 测试 - 获取相差分钟数
    /// </summary>
    [Fact]
    public void Test_GetMinutes()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 0, 0, 0), new DateTime(2024, 1, 1, 0, 45, 0));
        range.GetMinutes().ShouldBe(45);
    }
    /// <summary>
    /// 测试 - 获取相差秒数
    /// </summary>
    [Fact]
    public void Test_GetSeconds()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 0, 0, 0), new DateTime(2024, 1, 1, 0, 0, 45));
        range.GetSeconds().ShouldBe(45);
    }
    /// <summary>
    /// 测试 - 获取相差毫秒数
    /// </summary>
    [Fact]
    public void Test_GetMilliseconds()
    {
        var start = new DateTime(2024, 1, 1, 12, 0, 0, 0);
        var end = new DateTime(2024, 1, 1, 12, 0, 0, 500);
        var range = new DateTimeRange(start, end);
        range.GetMilliseconds().ShouldBe(500);
    }
    /// <summary>
    /// 测试 - 是否在指定范围内
    /// </summary>
    [Fact]
    public void Test_In()
    {
        var innerRange = new DateTimeRange(new DateTime(2024, 1, 3), new DateTime(2024, 1, 8));
        var outerRange = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        // 内部范围应该在外部范围内
        innerRange.In(outerRange).ShouldBeTrue();
        // 外部范围不应该在内部范围内
        outerRange.In(innerRange).ShouldBeFalse();
        // 部分重叠的范围不应该互相包含
        var partialRange = new DateTimeRange(new DateTime(2024, 1, 5), new DateTime(2024, 1, 15));
        innerRange.In(partialRange).ShouldBeFalse();
        partialRange.In(innerRange).ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - 使用起止时间参数的In方法
    /// </summary>
    [Fact]
    public void Test_In_WithStartAndEnd()
    {
        var innerRange = new DateTimeRange(new DateTime(2024, 1, 3), new DateTime(2024, 1, 8));
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 10);
        innerRange.In(start, end).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - 是否包含指定时间
    /// </summary>
    [Fact]
    public void Test_Contains_ShouldWork()
    {
        var range = new DateTimeRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
        Assert.True(range.Contains(new DateTime(2025, 1, 5)));
        Assert.False(range.Contains(new DateTime(2024, 12, 31)));
    }
    /// <summary>
    /// 测试 - 包含指定时间范围
    /// </summary>
    [Fact]
    public void Test_Contains_Range()
    {
        var outerRange = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var innerRange = new DateTimeRange(new DateTime(2024, 1, 3), new DateTime(2024, 1, 8));
        // 外部范围应该包含内部范围
        outerRange.Contains(innerRange).ShouldBeTrue();
        // 内部范围不应该包含外部范围
        innerRange.Contains(outerRange).ShouldBeFalse();
        // 部分重叠的范围不应该互相包含
        var partialRange = new DateTimeRange(new DateTime(2024, 1, 5), new DateTime(2024, 1, 15));
        outerRange.Contains(partialRange).ShouldBeFalse();
        partialRange.Contains(outerRange).ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - 使用起止时间参数的Contains方法
    /// </summary>
    [Fact]
    public void Test_Contains_WithStartAndEnd()
    {
        var outerRange = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var start = new DateTime(2024, 1, 3);
        var end = new DateTime(2024, 1, 8);
        outerRange.Contains(start, end).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - 是否与指定时间范围相交
    /// </summary>
    [Fact]
    public void Test_HasIntersect_ShouldWork()
    {
        var r1 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var r2 = new DateTimeRange(new DateTime(2024, 1, 5), new DateTime(2024, 1, 15));
        Assert.True(r1.HasIntersect(r2));
        var r3 = new DateTimeRange(new DateTime(2024, 1, 11), new DateTime(2024, 1, 12));
        Assert.False(r1.HasIntersect(r3));
    }
    /// <summary>
    /// 测试 - 获取交集范围
    /// </summary>
    [Fact]
    public void Test_Intersect()
    {
        // 有交集的情况
        var r1 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var r2 = new DateTimeRange(new DateTime(2024, 1, 5), new DateTime(2024, 1, 15));
        var (intersected, range) = r1.Intersect(r2);
        intersected.ShouldBeTrue();
        range.ShouldNotBeNull();
        range.StartTime.ShouldBe(new DateTime(2024, 1, 5));
        range.EndTime.ShouldBe(new DateTime(2024, 1, 10));
        // 无交集的情况
        var r3 = new DateTimeRange(new DateTime(2024, 1, 11), new DateTime(2024, 1, 15));
        var (intersected2, range2) = r1.Intersect(r3);
        intersected2.ShouldBeFalse();
        range2.ShouldBeNull();
        // 边界相等的情况
        var r4 = new DateTimeRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 15));
        var (intersected3, range3) = r1.Intersect(r4);
        intersected3.ShouldBeTrue();
        range3.StartTime.ShouldBe(new DateTime(2024, 1, 10));
        range3.EndTime.ShouldBe(new DateTime(2024, 1, 10));
    }
    /// <summary>
    /// 测试 - 使用起止时间参数的交集方法
    /// </summary>
    [Fact]
    public void Test_Intersect_WithStartAndEnd()
    {
        var r1 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var start = new DateTime(2024, 1, 5);
        var end = new DateTime(2024, 1, 15);
        var (intersected, range) = r1.Intersect(start, end);
        intersected.ShouldBeTrue();
        range.ShouldNotBeNull();
        range.StartTime.ShouldBe(new DateTime(2024, 1, 5));
        range.EndTime.ShouldBe(new DateTime(2024, 1, 10));
    }
    /// <summary>
    /// 测试 - 合并时间范围
    /// </summary>
    [Fact]
    public void Test_Union()
    {
        // 有交集的情况
        var r1 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var r2 = new DateTimeRange(new DateTime(2024, 1, 5), new DateTime(2024, 1, 15));
        var result = r1.Union(r2);
        result.StartTime.ShouldBe(new DateTime(2024, 1, 1));
        result.EndTime.ShouldBe(new DateTime(2024, 1, 15));
        // 边界相等的情况
        var r3 = new DateTimeRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 15));
        result = r1.Union(r3);
        result.StartTime.ShouldBe(new DateTime(2024, 1, 1));
        result.EndTime.ShouldBe(new DateTime(2024, 1, 15));
    }
    /// <summary>
    /// 测试 - 合并不相交时间范围应抛出异常
    /// </summary>
    [Fact]
    public void Test_Union_NonIntersecting_ShouldThrowException()
    {
        var r1 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));
        var r2 = new DateTimeRange(new DateTime(2024, 1, 6), new DateTime(2024, 1, 10));
        var exception = Assert.Throws<ArgumentException>(() => r1.Union(r2));
        exception.Message.ShouldContain("不相交的时间段无法合并");
    }
    /// <summary>
    /// 测试 - 使用起止时间参数的合并方法
    /// </summary>
    [Fact]
    public void Test_Union_WithStartAndEnd()
    {
        var r1 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var start = new DateTime(2024, 1, 5);
        var end = new DateTime(2024, 1, 15);
        var result = r1.Union(start, end);
        result.StartTime.ShouldBe(new DateTime(2024, 1, 1));
        result.EndTime.ShouldBe(new DateTime(2024, 1, 15));
    }
    /// <summary>
    /// 测试 - 匹配相等
    /// </summary>
    [Fact]
    public void Test_Equals()
    {
        var range1 = new DateTimeRange(_now, _now.AddMinutes(1));
        var range2 = new DateTimeRange(_now, _now.AddMinutes(1));
        Assert.True(range1==range2);
    }
    /// <summary>
    /// 测试 - 输出字符串
    /// </summary>
    [Fact]
    public void Test_ToString()
    {
        var range = new DateTimeRange(_now, _now.AddMinutes(1));
        range.ToString().ShouldBe("2024-06-25 14:37:56 - 2024-06-25 14:38:56");
        range.ToString("yyyy-MM-dd HH:mm").ShouldBe("2024-06-25 14:37 - 2024-06-25 14:38");
        range.ToString("yyyy-MM-dd HH:mm", " ~ ").ShouldBe("2024-06-25 14:37 ~ 2024-06-25 14:38");
    }
    /// <summary>
    /// 测试 - 比较方法
    /// </summary>
    [Fact]
    public void Test_CompareTo()
    {
        var earlier = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));
        var later = new DateTimeRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 15));
        earlier.CompareTo(later).ShouldBeLessThan(0); // 早期应该小于后期
        later.CompareTo(earlier).ShouldBeGreaterThan(0); // 后期应该大于早期
        earlier.CompareTo(earlier).ShouldBe(0); // 相同应该等于0
        earlier.CompareTo(null).ShouldBe(1); // 与null比较应该返回1
    }
    /// <summary>
    /// 测试 - HashCode 唯一性
    /// </summary>
    [Fact]
    public void Test_GetHashCode()
    {
        var r1 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var r2 = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 10));
        var r3 = new DateTimeRange(new DateTime(2024, 1, 2), new DateTime(2024, 1, 10));
        // 相同范围的HashCode应该一致
        r1.GetHashCode().ShouldBe(r2.GetHashCode());
        // 不同范围的HashCode应该不同
        r1.GetHashCode().ShouldNotBe(r3.GetHashCode());
    }
    /// <summary>
    /// 测试 - DateTime.In扩展方法
    /// </summary>
    [Fact]
    public void Test_DateTime_In()
    {
        var date = new DateTime(2024, 1, 5);
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 10);
        // 测试在范围内的日期
        date.In(start, end).ShouldBeTrue();
        // 测试边界值
        start.In(start, end).ShouldBeTrue();
        end.In(start, end).ShouldBeTrue();
        // 测试范围外的日期
        new DateTime(2023, 12, 31).In(start, end).ShouldBeFalse();
        new DateTime(2024, 1, 11).In(start, end).ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - 不同 DateTimeKind 的处理
    /// </summary>
    [Fact]
    public void Test_DateTimeKind_Handling()
    {
        var utcStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var utcEnd = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var range = new DateTimeRange(utcStart, utcEnd);
        // UTC时间应该使用UTC比较
        var utcTime = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        range.Contains(utcTime).ShouldBeTrue();
        // 本地时间应该使用本地比较
        var localTime = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Local);
        range.Contains(localTime).ShouldBeTrue();
        // 测试UTC和本地时间的转换
        range.StartTime.ShouldNotBe(range.UtcStartTime); // 除非测试机器时区是UTC
        range.EndTime.ShouldNotBe(range.UtcEndTime);
    }
}
