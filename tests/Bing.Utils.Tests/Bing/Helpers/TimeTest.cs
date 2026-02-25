namespace Bing.Helpers;
/// <summary>
/// 时间操作测试
/// </summary>
[Trait("Bing.Helpers", "Time")]
public class TimeTest : TestBase, IDisposable
{
    #region 测试初始化
    /// <summary>
    /// 日期格式
    /// </summary>
    private static readonly string _dateFormat = "yyyy-MM-dd HH:mm:ss";
    /// <summary>
    /// 日期时间字符串,"2012-12-12 12:12:12"
    /// </summary>
    private static readonly string _testDateString = "2012-12-12 12:12:12";
    /// <summary>
    /// 日期时间,2012-12-12 12:12:12
    /// </summary>
    private static readonly DateTime _testDate = DateTime.Parse(_testDateString);
    /// <summary>
    /// 本地日期时间,2012-12-12 12:12:12
    /// </summary>
    private static readonly DateTime _localDate = new(2012, 12, 12, 12, 12, 12, DateTimeKind.Local);
    /// <summary>
    /// 本地日期时间,2012-12-12 20:12:12
    /// </summary>
    private static readonly DateTime _localDate2 = new(2012, 12, 12, 20, 12, 12, DateTimeKind.Local);
    /// <summary>
    /// utc日期时间,2012-12-12 4:12:12
    /// </summary>
    private static readonly DateTime _utcDate = new(2012, 12, 12, 4, 12, 12, DateTimeKind.Utc);
    /// <summary>
    /// utc日期时间,2012-12-12 12:12:12
    /// </summary>
    private static readonly DateTime _utcDate2 = new(2012, 12, 12, 12, 12, 12, DateTimeKind.Utc);
    /// <summary>
    /// 未指定日期时间,2012-12-12 12:12:12
    /// </summary>
    private static readonly DateTime _unspecifiedDate = new(2012, 12, 12, 12, 12, 12, DateTimeKind.Unspecified);
    /// <summary>
    /// 测试初始化
    /// </summary>
    public TimeTest(ITestOutputHelper output) : base(output)
    {
    }
    #endregion
    #region 测试清理
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose() => Time.Reset();
    #endregion
    #region 辅助方法
    /// <summary>
    /// 断言日期时间相等
    /// </summary>
    /// <param name="expected">期望的日期字符串</param>
    /// <param name="actual">实际的日期时间</param>
    private void AssertDateTimeEqual(string expected, DateTime actual)
    {
        actual.ToString(_dateFormat).ShouldBe(expected);
    }
    /// <summary>
    /// 断言日期时间不相等
    /// </summary>
    /// <param name="expected">期望的日期字符串</param>
    /// <param name="actual">实际的日期时间</param>
    private void AssertDateTimeNotEqual(string expected, DateTime actual)
    {
        actual.ToString(_dateFormat).ShouldNotBe(expected);
    }
    #endregion
    #region SetTime 测试
    /// <summary>
    /// 测试 - SetTime - 设置和获取模拟时间
    /// </summary>
    [Fact]
    public void SetTime_ValidDateTime_SetsAndReturnsCorrectTime()
    {
        // Act
        Time.SetTime(_testDate);
        // Assert
        AssertDateTimeEqual(_testDateString, Time.Now);
    }
    /// <summary>
    /// 测试 - SetTime - 设置null清除模拟时间
    /// </summary>
    [Fact]
    public void SetTime_NullDateTime_ClearsMockTime()
    {
        // Arrange
        Time.SetTime(_testDate);
        // Act
        Time.SetTime((DateTime?)null);
        // Assert
        AssertDateTimeNotEqual(_testDateString, Time.Now);
    }
    /// <summary>
    /// 测试 - SetTime - 通过字符串设置时间
    /// </summary>
    [Theory]
    [InlineData("2023-01-01 12:00:00")]
    [InlineData("2023/1/1 12:00")]
    [InlineData("Jan 1, 2023 12:00 PM")]
    public void SetTime_ValidDateString_SetsTimeCorrectly(string dateString)
    {
        // Act
        Time.SetTime(dateString);
        // Assert
        Time.Now.ShouldNotBe(default(DateTime));
        // 验证时间确实被设置了（不等于默认值且是固定的）
        var time1 = Time.Now;
        var time2 = Time.Now;
        time1.ShouldBe(time2); // 模拟时间应该是固定的
    }
    /// <summary>
    /// 测试 - SetTime - 无效日期字符串抛出异常
    /// </summary>
    [Theory]
    [InlineData("invalid date")]
    [InlineData("2023-13-45")]
    [InlineData("abc")]
    public void SetTime_InvalidDateString_ThrowsArgumentException(string invalidDateString)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Time.SetTime(invalidDateString));
    }
    /// <summary>
    /// 测试 - SetTime - 空字符串清除模拟时间
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetTime_NullOrEmptyString_ClearsMockTime(string dateString)
    {
        // Arrange
        Time.SetTime(_testDate);
        var beforeClear = Time.Now;
        // Act
        Time.SetTime(dateString);
        // Assert
        beforeClear.ShouldBe(_testDate);
        AssertDateTimeNotEqual(_testDateString, Time.Now);
    }
    #endregion
    #region UseUtc 测试
    /// <summary>
    /// 测试 - UseUtc - 启用UTC时间
    /// </summary>
    [Fact]
    public void UseUtc_EnableUtc_ReturnsUtcTime()
    {
        // Act
        Time.UseUtc(true);
        // Assert
        var now = Time.Now;
        var utcNow = DateTime.UtcNow;
        // 允许1秒的误差范围
        Math.Abs((now - utcNow).TotalSeconds).ShouldBeLessThan(1);
    }
    /// <summary>
    /// 测试 - UseUtc - 禁用UTC时间
    /// </summary>
    [Fact]
    public void UseUtc_DisableUtc_ReturnsLocalTime()
    {
        // Act
        Time.UseUtc(false);
        // Assert
        var now = Time.Now;
        var localNow = DateTime.Now;
        // 允许1秒的误差范围
        Math.Abs((now - localNow).TotalSeconds).ShouldBeLessThan(1);
    }
    /// <summary>
    /// 测试 - UseUtc - 默认参数启用UTC
    /// </summary>
    [Fact]
    public void UseUtc_DefaultParameter_EnablesUtc()
    {
        // Act
        Time.UseUtc();
        // Assert
        var now = Time.Now;
        var utcNow = DateTime.UtcNow;
        // 允许1秒的误差范围
        Math.Abs((now - utcNow).TotalSeconds).ShouldBeLessThan(1);
    }
    #endregion
    #region Reset 测试
    /// <summary>
    /// 测试 - Reset - 重置所有配置
    /// </summary>
    [Fact]
    public void Reset_AfterSettingTimeAndUtc_ClearsAllSettings()
    {
        // Arrange
        Time.SetTime(_testDate);
        Time.UseUtc(true);
        // Act
        Time.Reset();
        // Assert
        AssertDateTimeNotEqual(_testDateString, Time.Now);
        // 重置后应该使用默认的时间配置
        Time.Now.ShouldNotBe(_testDate);
    }
    #endregion
    #region Normalize 测试
    /// <summary>
    /// 测试 - Normalize - 可空参数为null返回null
    /// </summary>
    [Fact]
    public void Normalize_NullDateTime_ReturnsNull()
    {
        // Act
        var result = Time.Normalize((DateTime?)null);
        // Assert
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试 - Normalize - 本地时间配置下转换本地时间
    /// </summary>
    [Fact]
    public void Normalize_LocalConfigWithLocalTime_ReturnsLocalTime()
    {
        // Arrange
        Time.UseUtc(false);
        // Act
        var result = Time.Normalize(_localDate);
        // Assert
        AssertDateTimeEqual(_testDateString, result);
        result.Kind.ShouldBe(DateTimeKind.Local);
    }
    /// <summary>
    /// 测试 - Normalize - 本地时间配置下转换UTC时间
    /// </summary>
    [Fact]
    public void Normalize_LocalConfigWithUtcTime_ConvertsToLocal()
    {
        // Arrange
        Time.UseUtc(false);
        // Act
        var result = Time.Normalize(_utcDate);
        // Assert
        AssertDateTimeEqual(_testDateString, result);
        result.Kind.ShouldBe(DateTimeKind.Local);
    }
    /// <summary>
    /// 测试 - Normalize - UTC配置下转换本地时间
    /// </summary>
    [Fact]
    public void Normalize_UtcConfigWithLocalTime_ConvertsToUtc()
    {
        // Arrange
        Time.UseUtc(true);
        // Act
        var result = Time.Normalize(_localDate2);
        // Assert
        AssertDateTimeEqual(_testDateString, result);
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }
    /// <summary>
    /// 测试 - Normalize - UTC配置下转换UTC时间
    /// </summary>
    [Fact]
    public void Normalize_UtcConfigWithUtcTime_ReturnsUtcTime()
    {
        // Arrange
        Time.UseUtc(true);
        // Act
        var result = Time.Normalize(_utcDate2);
        // Assert
        AssertDateTimeEqual(_testDateString, result);
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }
    /// <summary>
    /// 测试 - Normalize - 本地配置下处理未指定时区
    /// </summary>
    [Fact]
    public void Normalize_LocalConfigWithUnspecified_TreatsAsLocal()
    {
        // Arrange
        Time.UseUtc(false);
        // Act
        var result = Time.Normalize(_unspecifiedDate);
        // Assert
        AssertDateTimeEqual(_testDateString, result);
        result.Kind.ShouldBe(DateTimeKind.Local);
    }
    /// <summary>
    /// 测试 - Normalize - UTC配置下处理未指定时区
    /// </summary>
    [Fact]
    public void Normalize_UtcConfigWithUnspecified_ConvertsToUtc()
    {
        // Arrange
        Time.UseUtc(true);
        // Act
        var result = Time.Normalize(_unspecifiedDate);
        // Assert
        result.ShouldBe(_utcDate);
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }
    #endregion
    #region ToUniversalTime 测试
    /// <summary>
    /// 测试 - ToUniversalTime - DateTime.MinValue特殊处理
    /// </summary>
    [Fact]
    public void ToUniversalTime_MinValue_ReturnsMinValue()
    {
        // Act
        var result = Time.ToUniversalTime(DateTime.MinValue);
        // Assert
        result.ShouldBe(DateTime.MinValue);
    }
    /// <summary>
    /// 测试 - ToUniversalTime - 本地时间转UTC
    /// </summary>
    [Fact]
    public void ToUniversalTime_LocalTime_ConvertsToUtc()
    {
        // Act
        var result = Time.ToUniversalTime(_localDate);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Utc);
        result.ShouldBe(_localDate.ToUniversalTime());
    }
    /// <summary>
    /// 测试 - ToUniversalTime - 未指定时区假定为本地时间
    /// </summary>
    [Fact]
    public void ToUniversalTime_UnspecifiedTime_TreatsAsLocalAndConverts()
    {
        // Act
        var result = Time.ToUniversalTime(_unspecifiedDate);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Utc);
        var expected = DateTime.SpecifyKind(_unspecifiedDate, DateTimeKind.Local).ToUniversalTime();
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - ToUniversalTime - UTC时间直接返回
    /// </summary>
    [Fact]
    public void ToUniversalTime_UtcTime_ReturnsOriginal()
    {
        // Act
        var result = Time.ToUniversalTime(_utcDate);
        // Assert
        result.ShouldBe(_utcDate);
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }
    #endregion
    #region ToLocalTime 测试
    /// <summary>
    /// 测试 - ToLocalTime - DateTime.MinValue特殊处理
    /// </summary>
    [Fact]
    public void ToLocalTime_MinValue_ReturnsMinValue()
    {
        // Act
        var result = Time.ToLocalTime(DateTime.MinValue);
        // Assert
        result.ShouldBe(DateTime.MinValue);
    }
    /// <summary>
    /// 测试 - ToLocalTime - UTC时间转本地
    /// </summary>
    [Fact]
    public void ToLocalTime_UtcTime_ConvertsToLocal()
    {
        // Act
        var result = Time.ToLocalTime(_utcDate);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Local);
        result.ShouldBe(_utcDate.ToLocalTime());
    }
    /// <summary>
    /// 测试 - ToLocalTime - 未指定时区设置为本地
    /// </summary>
    [Fact]
    public void ToLocalTime_UnspecifiedTime_SetsToLocal()
    {
        // Act
        var result = Time.ToLocalTime(_unspecifiedDate);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Local);
        result.ShouldBe(DateTime.SpecifyKind(_unspecifiedDate, DateTimeKind.Local));
    }
    /// <summary>
    /// 测试 - ToLocalTime - 本地时间直接返回
    /// </summary>
    [Fact]
    public void ToLocalTime_LocalTime_ReturnsOriginal()
    {
        // Act
        var result = Time.ToLocalTime(_localDate);
        // Assert
        result.ShouldBe(_localDate);
        result.Kind.ShouldBe(DateTimeKind.Local);
    }
    #endregion
    #region UtcToLocalTime 测试
    /// <summary>
    /// 测试 - UtcToLocalTime - DateTime.MinValue特殊处理
    /// </summary>
    [Fact]
    public void UtcToLocalTime_MinValue_ReturnsMinValue()
    {
        // Act
        var result = Time.UtcToLocalTime(DateTime.MinValue);
        // Assert
        result.ShouldBe(DateTime.MinValue);
    }
    /// <summary>
    /// 测试 - UtcToLocalTime - UTC时间转换
    /// </summary>
    [Fact]
    public void UtcToLocalTime_UtcTime_ConvertsToLocal()
    {
        // Act
        var result = Time.UtcToLocalTime(_utcDate);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Local);
        result.ShouldBe(_utcDate.ToLocalTime());
    }
    /// <summary>
    /// 测试 - UtcToLocalTime - 本地时间直接返回
    /// </summary>
    [Fact]
    public void UtcToLocalTime_LocalTime_ReturnsOriginal()
    {
        // Act
        var result = Time.UtcToLocalTime(_localDate);
        // Assert
        result.ShouldBe(_localDate);
        result.Kind.ShouldBe(DateTimeKind.Local);
    }
    /// <summary>
    /// 测试 - UtcToLocalTime - 未指定时区在UTC配置下转换
    /// </summary>
    [Fact]
    public void UtcToLocalTime_UnspecifiedWithUtcConfig_ConvertsAsUtc()
    {
        // Arrange
        Time.UseUtc(true);
        // Act
        var result = Time.UtcToLocalTime(_unspecifiedDate);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Local);
        var expected = DateTime.SpecifyKind(_unspecifiedDate, DateTimeKind.Utc).ToLocalTime();
        result.ShouldBe(expected);
    }
    /// <summary>
    /// 测试 - UtcToLocalTime - 未指定时区在本地配置下直接返回
    /// </summary>
    [Fact]
    public void UtcToLocalTime_UnspecifiedWithLocalConfig_ReturnsOriginal()
    {
        // Arrange
        Time.UseUtc(false);
        // Act
        var result = Time.UtcToLocalTime(_unspecifiedDate);
        // Assert
        result.ShouldBe(_unspecifiedDate);
    }
    #endregion
    #region GetDateTime 和 GetDate 测试
    /// <summary>
    /// 测试 - GetDateTime - 兼容性方法功能
    /// </summary>
    [Fact]
    public void GetDateTime_WithMockTime_ReturnsMockTime()
    {
        // Arrange
        Time.SetTime(_testDate);
        // Act
#pragma warning disable CS0618 // 忽略过时警告
        var result = Time.GetDateTime();
#pragma warning restore CS0618
        // Assert
        result.ShouldBe(_testDate);
    }
    /// <summary>
    /// 测试 - GetDate - 获取日期部分
    /// </summary>
    [Fact]
    public void GetDate_WithMockTime_ReturnsDatePart()
    {
        // Arrange
        Time.SetTime(_testDate);
        // Act
        var result = Time.GetDate();
        // Assert
        result.ShouldBe(_testDate.Date);
        result.TimeOfDay.ShouldBe(TimeSpan.Zero);
    }
    #endregion
    #region Unix时间戳测试
    /// <summary>
    /// 测试 - GetUnixTimestamp - 当前时间戳
    /// </summary>
    [Fact]
    public void GetUnixTimestamp_CurrentTime_ReturnsValidTimestamp()
    {
        // Act
        var timestamp = Time.GetUnixTimestamp();
        // Assert
        timestamp.ShouldBeGreaterThan(0);
        // 验证时间戳合理性（应该接近当前时间）
        var expectedMin = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();
        var expectedMax = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
        timestamp.ShouldBeInRange(expectedMin, expectedMax);
    }
    /// <summary>
    /// 测试 - GetUnixTimestamp - 指定时间的时间戳
    /// </summary>
    [Theory]
    [InlineData("1970-01-01 12:12:12", 15132)]
    [InlineData("2000-12-12 12:12:12", 976594332)]
    [InlineData("2014-02-18 04:24:59", 1392668699)]
    public void GetUnixTimestamp_SpecificDateTime_ReturnsCorrectTimestamp(string dateTimeString, long expectedTimestamp)
    {
        // Arrange
        var dateTime = DateTime.Parse(dateTimeString);
        // Act
        var result = Time.GetUnixTimestamp(dateTime);
        // Assert
        result.ShouldBe(expectedTimestamp);
    }
    /// <summary>
    /// 测试 - GetTimeFromUnixTimestamp - 从时间戳获取时间
    /// </summary>
    [Theory]
    [InlineData(15132, "1970-01-01 12:12:12")]
    [InlineData(976594332, "2000-12-12 12:12:12")]
    [InlineData(1392668699, "2014-02-18 04:24:59")]
    public void GetTimeFromUnixTimestamp_ValidTimestamp_ReturnsCorrectDateTime(long timestamp, string expectedDateTimeString)
    {
        // Arrange
        var expectedDateTime = DateTime.Parse(expectedDateTimeString);
        // Act
        var result = Time.GetTimeFromUnixTimestamp(timestamp);
        // Assert
        result.ShouldBe(expectedDateTime);
    }
    /// <summary>
    /// 测试 - OfEpochSecond - Unix时间戳秒转换
    /// </summary>
    [Fact]
    public void OfEpochSecond_ValidTimestamp_ReturnsCorrectLocalTime()
    {
        // Arrange
        const long timestamp = 1640995200; // 2022-01-01 00:00:00 UTC
        // Act
        var result = Time.OfEpochSecond(timestamp);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Local);
        // 验证转换正确性 - 转回时间戳应该相等
        Time.ToEpochSecond(result).ShouldBe(timestamp);
    }
    /// <summary>
    /// 测试 - OfEpochSecond - 超出范围的时间戳
    /// </summary>
    [Theory]
    [InlineData(253402300800)] // 超出DateTime.MaxValue对应的时间戳
    [InlineData(-62135596801)] // 小于DateTime.MinValue对应的时间戳
    public void OfEpochSecond_OutOfRangeTimestamp_ThrowsArgumentOutOfRangeException(long invalidTimestamp)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Time.OfEpochSecond(invalidTimestamp));
    }
    /// <summary>
    /// 测试 - OfEpochMilli - Unix时间戳毫秒转换
    /// </summary>
    [Fact]
    public void OfEpochMilli_ValidTimestamp_ReturnsCorrectLocalTime()
    {
        // Arrange
        const long timestamp = 1640995200000; // 2022-01-01 00:00:00.000 UTC
        // Act
        var result = Time.OfEpochMilli(timestamp);
        // Assert
        result.Kind.ShouldBe(DateTimeKind.Local);
        // 验证转换正确性 - 转回时间戳应该相等
        Time.ToEpochMilli(result).ShouldBe(timestamp);
    }
    /// <summary>
    /// 测试 - OfEpochMilli - 超出范围的时间戳
    /// </summary>
    [Theory]
    [InlineData(253402300800000)] // 超出DateTime.MaxValue对应的时间戳（毫秒）
    [InlineData(-62135596800001)] // 小于DateTime.MinValue对应的时间戳（毫秒）
    public void OfEpochMilli_OutOfRangeTimestamp_ThrowsArgumentOutOfRangeException(long invalidTimestamp)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => Time.OfEpochMilli(invalidTimestamp));
    }
    /// <summary>
    /// 测试 - ToEpochSecond - 日期转Unix时间戳秒
    /// </summary>
    [Fact]
    public void ToEpochSecond_ValidDateTime_ReturnsCorrectTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // Act
        var result = Time.ToEpochSecond(dateTime);
        // Assert
        result.ShouldBe(1640995200);
    }
    /// <summary>
    /// 测试 - ToEpochMilli - 日期转Unix时间戳毫秒
    /// </summary>
    [Fact]
    public void ToEpochMilli_ValidDateTime_ReturnsCorrectTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2022, 1, 1, 0, 0, 0, 500, DateTimeKind.Utc);
        // Act
        var result = Time.ToEpochMilli(dateTime);
        // Assert
        result.ShouldBe(1640995200500);
    }
    /// <summary>
    /// 测试 - Unix时间戳转换 - 往返测试
    /// </summary>
    [Theory]
    [InlineData(0)]  // Unix epoch
    [InlineData(1640995200)]  // 2022-01-01
    [InlineData(-62135596800)] // DateTime.MinValue 对应的时间戳
    public void UnixTimestampRoundTrip_ValidTimestamps_MaintainsAccuracy(long originalTimestamp)
    {
        // Act
        var dateTime = Time.OfEpochSecond(originalTimestamp);
        var resultTimestamp = Time.ToEpochSecond(dateTime);
        // Assert
        resultTimestamp.ShouldBe(originalTimestamp);
    }
    #endregion
    #region 边界条件和异常测试
    /// <summary>
    /// 测试 - 时区转换 - 处理极值
    /// </summary>
    [Theory]
    [InlineData("0001-01-01 00:00:00")]
    [InlineData("9999-12-31 23:59:59")]
    public void TimeZoneConversion_ExtremeValues_HandlesCorrectly(string dateTimeString)
    {
        // Arrange
        var dateTime = DateTime.Parse(dateTimeString);
        // Act & Assert - 这些操作不应该抛出异常
        Should.NotThrow(() =>
        {
            var utc = Time.ToUniversalTime(dateTime);
            var local = Time.ToLocalTime(utc);
            var normalized = Time.Normalize(dateTime);
        });
    }
    /// <summary>
    /// 测试 - 多线程安全性 - AsyncLocal隔离
    /// </summary>
    [Fact]
    public async Task AsyncLocal_MultipleThreads_AreIsolated()
    {
        // Arrange
        var date1 = new DateTime(2023, 1, 1);
        var date2 = new DateTime(2023, 2, 1);
        var results = new DateTime[2];
        // Act
        var task1 = Task.Run(() =>
        {
            Time.SetTime(date1);
            Thread.Sleep(100); // 确保两个任务有重叠
            results[0] = Time.Now;
        });
        var task2 = Task.Run(() =>
        {
            Time.SetTime(date2);
            Thread.Sleep(100); // 确保两个任务有重叠
            results[1] = Time.Now;
        });
        await Task.WhenAll(task1, task2);
        // Assert
        results[0].ShouldBe(date1);
        results[1].ShouldBe(date2);
    }
    /// <summary>
    /// 测试 - 性能测试 - Unix时间戳转换
    /// </summary>
    [Fact]
    public void UnixTimestampConversion_PerformanceTest_ExecutesWithinReasonableTime()
    {
        // Arrange
        const int iterations = 10000;
        var dateTime = DateTime.UtcNow;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var timestamp = Time.ToEpochSecond(dateTime);
                var converted = Time.OfEpochSecond(timestamp);
            }
        }, TimeSpan.FromSeconds(1)); // 应该在1秒内完成10000次转换
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 完整工作流 - 模拟时间设置和时区转换
    /// </summary>
    [Fact]
    public void CompleteWorkflow_MockTimeAndTimezoneConversion_WorksCorrectly()
    {
        // Arrange
        var testDateTime = new DateTime(2023, 6, 15, 14, 30, 0, DateTimeKind.Local);
        // Act & Assert
        // 1. 设置模拟时间
        Time.SetTime(testDateTime);
        Time.Now.ShouldBe(testDateTime);
        // 2. 测试时区转换
        Time.UseUtc(true);
        var normalizedUtc = Time.Normalize(testDateTime);
        normalizedUtc.Kind.ShouldBe(DateTimeKind.Utc);
        // 3. 测试Unix时间戳转换
        var timestamp = Time.ToEpochSecond(testDateTime);
        var fromTimestamp = Time.OfEpochSecond(timestamp);
        // 允许小的精度差异（秒级转换可能丢失毫秒精度）
        Math.Abs((fromTimestamp - testDateTime).TotalSeconds).ShouldBeLessThan(1);
        // 4. 重置并验证
        Time.Reset();
        Time.Now.ShouldNotBe(testDateTime);
    }
    #endregion
}
