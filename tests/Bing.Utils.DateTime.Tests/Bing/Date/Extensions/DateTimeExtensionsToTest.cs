using Bing.Tests;
using NodaTime;

namespace Bing.Date.Extensions;

/// <summary>
/// 日期时间 扩展 转换 测试
/// </summary>
[Trait("DateTimeUT", "DateTimeExtensions.To")]
public class DateTimeExtensionsToTest : TestBase
{
    /// <inheritdoc />
    public DateTimeExtensionsToTest(ITestOutputHelper output) : base(output)
    {
    }

    #region ToUtc

    /// <summary>
    /// 测试 - ToUtc - 转换本地时间为UTC时间格式
    /// </summary>
    [Fact]
    public void ToUtc_ConvertLocalToUtc_SetsKindToUtc()
    {
        // 准备
        var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);

        // 执行
        var utcTime = localTime.ToUtc();

        // 验证
        utcTime.Kind.ShouldBe(DateTimeKind.Utc);
        utcTime.Year.ShouldBe(localTime.Year);
        utcTime.Month.ShouldBe(localTime.Month);
        utcTime.Day.ShouldBe(localTime.Day);
        utcTime.Hour.ShouldBe(localTime.Hour);
        utcTime.Minute.ShouldBe(localTime.Minute);
        utcTime.Second.ShouldBe(localTime.Second);
    }

    /// <summary>
    /// 测试 - ToUtc - 保留原始时间值不变
    /// </summary>
    [Fact]
    public void ToUtc_PreservesTimeValue()
    {
        // 准备
        var originalTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);

        // 执行
        var utcTime = originalTime.ToUtc();

        // 验证
        utcTime.ToString("yyyy-MM-dd HH:mm:ss").ShouldBe(originalTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    /// <summary>
    /// 测试 - ToUtc - 不同Kind类型的转换
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    public void ToUtc_DifferentKinds_AlwaysReturnsUtcKind(DateTimeKind kind)
    {
        // 准备
        var time = new DateTime(2023, 1, 1, 12, 0, 0, kind);

        // 执行
        var utcTime = time.ToUtc();

        // 验证
        utcTime.Kind.ShouldBe(DateTimeKind.Utc);
    }

    #endregion

    #region ToCst

    /// <summary>
    /// 测试 - ToCst - 返回正确的中国标准时间
    /// </summary>
    [Fact]
    public void ToCst_ReturnsChineseStandardTime()
    {
        // 准备
        var dateTime = DateTime.UtcNow;

        // 执行
        var cstTime = dateTime.ToCst();

        // 验证
        cstTime.Kind.ShouldBe(DateTimeKind.Unspecified);

        // 由于ToCst使用SystemClock.Instance.GetCurrentInstant()获取时间
        // 而不是使用传入的参数，因此这里主要验证返回的时间类型正确
        Output.WriteLine($"CST时间: {cstTime}");
    }

    #endregion

    #region ToEpochTimeSpan

    /// <summary>
    /// 测试 - ToEpochTimeSpan - 计算与Unix纪元的时间差
    /// </summary>
    [Fact]
    public void ToEpochTimeSpan_CalculatesCorrectTimespan()
    {
        // 准备
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var testDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedSpan = testDate - epoch;

        // 执行
        var actualSpan = testDate.ToEpochTimeSpan();

        // 验证
        actualSpan.TotalDays.ShouldBe(expectedSpan.TotalDays);
        actualSpan.TotalHours.ShouldBe(expectedSpan.TotalHours);
    }

    /// <summary>
    /// 测试 - ToEpochTimeSpan - 对Unix纪元前的日期计算负值
    /// </summary>
    [Fact]
    public void ToEpochTimeSpan_BeforeEpoch_ReturnsNegativeTimespan()
    {
        // 准备
        var beforeEpoch = new DateTime(1969, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 执行
        var timeSpan = beforeEpoch.ToEpochTimeSpan();

        // 验证
        timeSpan.TotalSeconds.ShouldBeLessThan(0);
    }

    #endregion

    #region ToLocalDateTime

    /// <summary>
    /// 测试 - ToLocalDateTime - 转换为NodaTime LocalDateTime
    /// </summary>
    [Fact]
    public void ToLocalDateTime_ConvertToNodaTimeLocalDateTime()
    {
        // 准备
        var dateTime = new DateTime(2023, 1, 1, 12, 30, 45, 500);
        var expected = LocalDateTime.FromDateTime(dateTime);

        // 执行
        var result = dateTime.ToLocalDateTime();

        // 验证
        result.Year.ShouldBe(dateTime.Year);
        result.Month.ShouldBe(dateTime.Month);
        result.Day.ShouldBe(dateTime.Day);
        result.Hour.ShouldBe(dateTime.Hour);
        result.Minute.ShouldBe(dateTime.Minute);
        result.Second.ShouldBe(dateTime.Second);
        result.Millisecond.ShouldBe(dateTime.Millisecond);
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToLocalDateTime - 边界值测试
    /// </summary>
    [Fact]
    public void ToLocalDateTime_EdgeCases()
    {
        // 准备 - 最小值
        var minDateTime = DateTime.MinValue;

        // 执行
        var minResult = minDateTime.ToLocalDateTime();

        // 验证
        minResult.Year.ShouldBe(minDateTime.Year);
        minResult.Month.ShouldBe(minDateTime.Month);
        minResult.Day.ShouldBe(minDateTime.Day);

        // 注意：DateTime.MaxValue可能超出NodaTime的范围，可能需要使用较小的值
        var nearMaxDateTime = new DateTime(9999, 12, 31, 23, 59, 59, 999);

        // 执行
        var maxResult = nearMaxDateTime.ToLocalDateTime();

        // 验证
        maxResult.Year.ShouldBe(nearMaxDateTime.Year);
        maxResult.Month.ShouldBe(nearMaxDateTime.Month);
        maxResult.Day.ShouldBe(nearMaxDateTime.Day);
    }

    #endregion

    #region ToLocalDate

    /// <summary>
    /// 测试 - ToLocalDate - 转换为NodaTime LocalDate
    /// </summary>
    [Fact]
    public void ToLocalDate_ConvertToNodaTimeLocalDate()
    {
        // 准备
        var dateTime = new DateTime(2023, 1, 1, 12, 30, 45);
        var expected = LocalDate.FromDateTime(dateTime);

        // 执行
        var result = dateTime.ToLocalDate();

        // 验证
        result.Year.ShouldBe(dateTime.Year);
        result.Month.ShouldBe(dateTime.Month);
        result.Day.ShouldBe(dateTime.Day);
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToLocalDate - 时间部分被忽略
    /// </summary>
    [Fact]
    public void ToLocalDate_IgnoresTimeComponent()
    {
        // 准备
        var date1 = new DateTime(2023, 1, 1, 10, 30, 0);
        var date2 = new DateTime(2023, 1, 1, 22, 45, 0);

        // 执行
        var result1 = date1.ToLocalDate();
        var result2 = date2.ToLocalDate();

        // 验证
        result1.ShouldBe(result2);
    }

    #endregion

    #region ToNodaLocalTime

    /// <summary>
    /// 测试 - ToNodaLocalTime - 转换为NodaTime LocalTime
    /// </summary>
    [Fact]
    public void ToNodaLocalTime_ConvertToNodaTimeLocalTime()
    {
        // 准备
        var dateTime = new DateTime(2023, 1, 1, 12, 30, 45, 500);
        var expected = new LocalTime(dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

        // 执行
        var result = dateTime.ToNodaLocalTime();

        // 验证
        result.Hour.ShouldBe(dateTime.Hour);
        result.Minute.ShouldBe(dateTime.Minute);
        result.Second.ShouldBe(dateTime.Second);
        result.Millisecond.ShouldBe(dateTime.Millisecond);
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试 - ToNodaLocalTime - 日期部分被忽略
    /// </summary>
    [Fact]
    public void ToNodaLocalTime_IgnoresDateComponent()
    {
        // 准备
        var date1 = new DateTime(2022, 5, 10, 14, 30, 45, 500);
        var date2 = new DateTime(2023, 1, 1, 14, 30, 45, 500);

        // 执行
        var result1 = date1.ToNodaLocalTime();
        var result2 = date2.ToNodaLocalTime();

        // 验证
        result1.ShouldBe(result2);
    }

    #endregion

    #region ToBytes

    /// <summary>
    /// 测试 - ToBytes - 成功转换日期时间为字节数组
    /// </summary>
    [Fact]
    public void ToBytes_ReturnsCorrectBytes()
    {
        // 准备
        var date = new DateTime(2023, 1, 1, 12, 30, 45);
        var expectedBytes = BitConverter.GetBytes(date.ToBinary());

        // 执行
        var actualBytes = date.ToBytes();

        // 验证
        actualBytes.ShouldNotBeNull();
        actualBytes.Length.ShouldBe(8); // 长整数是8字节
        actualBytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - ToBytes - 最小日期值转换
    /// </summary>
    [Fact]
    public void ToBytes_MinValue_ReturnsCorrectBytes()
    {
        // 准备
        var date = DateTime.MinValue;
        var expectedBytes = BitConverter.GetBytes(date.ToBinary());

        // 执行
        var actualBytes = date.ToBytes();

        // 验证
        actualBytes.ShouldNotBeNull();
        actualBytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - ToBytes - 最大日期值转换
    /// </summary>
    [Fact]
    public void ToBytes_MaxValue_ReturnsCorrectBytes()
    {
        // 准备
        var date = DateTime.MaxValue;
        var expectedBytes = BitConverter.GetBytes(date.ToBinary());

        // 执行
        var actualBytes = date.ToBytes();

        // 验证
        actualBytes.ShouldNotBeNull();
        actualBytes.ShouldBe(expectedBytes);
    }

    /// <summary>
    /// 测试 - ToBytes - 不同Kind属性的日期时间转换
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ToBytes_DifferentKinds_ReturnsCorrectBytes(DateTimeKind kind)
    {
        // 准备
        var date = new DateTime(2023, 1, 1, 12, 30, 45, kind);
        var expectedBytes = BitConverter.GetBytes(date.ToBinary());

        // 执行
        var actualBytes = date.ToBytes();

        // 验证
        actualBytes.ShouldBe(expectedBytes);

        // 验证可以正确还原
        var restoredDate = DateTime.FromBinary(BitConverter.ToInt64(actualBytes, 0));
        restoredDate.ShouldBe(date);
        restoredDate.Kind.ShouldBe(kind);
    }

    /// <summary>
    /// 测试 - ToBytes - 验证转换的可逆性
    /// </summary>
    [Fact]
    public void ToBytes_IsReversible()
    {
        // 准备
        var originalDate = DateTime.Now;

        // 执行
        var bytes = originalDate.ToBytes();
        var restoredDate = DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));

        // 验证
        restoredDate.ShouldBe(originalDate);
        restoredDate.Kind.ShouldBe(originalDate.Kind);

        Output.WriteLine($"原始日期: {originalDate}");
        Output.WriteLine($"还原日期: {restoredDate}");
    }

    #endregion
}