using System.Globalization;
namespace Bing.Date;
/// <summary>
/// 时间戳类测试
/// </summary>
[Trait("Bing.Date", "TimeStamp")]
public class TimeStampTest
{
    #region 构造函数测试
    /// <summary>
    /// 测试 - 默认构造函数 - 使用当前时间
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesWithCurrentTime()
    {
        // Arrange
        var beforeCreation = DateTime.Now;
        // Act
        var timeStamp = new TimeStamp();
        // Assert
        var afterCreation = DateTime.Now;
        var actualTime = timeStamp.ToDateTime();
        actualTime.ShouldBeGreaterThanOrEqualTo(beforeCreation.AddMilliseconds(-100));
        actualTime.ShouldBeLessThanOrEqualTo(afterCreation.AddMilliseconds(100));
        timeStamp.ToTimestamp().ShouldBe(actualTime.Ticks);
    }
    /// <summary>
    /// 测试 - 时间戳构造函数 - 有效时间戳
    /// </summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(621355968000000000L)] // Unix epoch in .NET ticks
    [InlineData(638000000000000000L)] // Some future date
    public void Constructor_WithValidTimestamp_CreatesCorrectly(long timestamp)
    {
        // Act
        var timeStamp = new TimeStamp(timestamp);
        // Assert
        timeStamp.ToTimestamp().ShouldBe(timestamp);
        timeStamp.ToDateTime().Ticks.ShouldBe(timestamp);
    }
    /// <summary>
    /// 测试 - 时间戳构造函数 - 无效时间戳
    /// </summary>
    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void Constructor_WithInvalidTimestamp_ThrowsArgumentOutOfRangeException(long invalidTimestamp)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new TimeStamp(invalidTimestamp))
            .ParamName.ShouldBe("timestamp");
    }
    /// <summary>
    /// 测试 - DateTime构造函数 - 各种时间值
    /// </summary>
    [Theory]
    [InlineData("2024-01-01 00:00:00")]
    [InlineData("2024-12-31 23:59:59")]
    [InlineData("1900-01-01 00:00:00")]
    [InlineData("9999-12-31 23:59:59")]
    public void Constructor_WithDateTime_CreatesCorrectly(string dateTimeString)
    {
        // Arrange
        var dateTime = DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture);
        // Act
        var timeStamp = new TimeStamp(dateTime);
        // Assert
        timeStamp.ToDateTime().ShouldBe(dateTime);
        timeStamp.ToTimestamp().ShouldBe(dateTime.Ticks);
    }
    #endregion
    #region 转换方法测试
    /// <summary>
    /// 测试 - ToDateTime - 返回正确的时间
    /// </summary>
    [Fact]
    public void ToDateTime_Always_ReturnsCorrectDateTime()
    {
        // Arrange
        var expectedDateTime = new DateTime(2024, 6, 15, 14, 30, 45, 123);
        var timeStamp = new TimeStamp(expectedDateTime);
        // Act
        var actualDateTime = timeStamp.ToDateTime();
        // Assert
        actualDateTime.ShouldBe(expectedDateTime);
    }
    /// <summary>
    /// 测试 - ToTimestamp - 返回正确的时间戳
    /// </summary>
    [Fact]
    public void ToTimestamp_Always_ReturnsCorrectTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45, 123);
        var expectedTimestamp = dateTime.Ticks;
        var timeStamp = new TimeStamp(dateTime);
        // Act
        var actualTimestamp = timeStamp.ToTimestamp();
        // Assert
        actualTimestamp.ShouldBe(expectedTimestamp);
    }
    /// <summary>
    /// 测试 - 双向转换一致性
    /// </summary>
    [Fact]
    public void BidirectionalConversion_Always_MaintainsConsistency()
    {
        // Arrange
        var originalDateTime = new DateTime(2024, 3, 15, 10, 20, 30, 456);
        var originalTimestamp = originalDateTime.Ticks;
        // Act - DateTime -> TimeStamp -> DateTime
        var timeStampFromDateTime = new TimeStamp(originalDateTime);
        var convertedDateTime = timeStampFromDateTime.ToDateTime();
        // Act - Timestamp -> TimeStamp -> Timestamp
        var timeStampFromTimestamp = new TimeStamp(originalTimestamp);
        var convertedTimestamp = timeStampFromTimestamp.ToTimestamp();
        // Assert
        convertedDateTime.ShouldBe(originalDateTime);
        convertedTimestamp.ShouldBe(originalTimestamp);
        timeStampFromDateTime.ToTimestamp().ShouldBe(timeStampFromTimestamp.ToTimestamp());
    }
    #endregion
    #region 静态方法测试
    /// <summary>
    /// 测试 - Now - 返回当前时间戳
    /// </summary>
    [Fact]
    public void Now_Always_ReturnsCurrentTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.Now.Ticks;
        // Act
        var nowTimestamp = TimeStamp.Now();
        // Assert
        var afterCall = DateTime.Now.Ticks;
        nowTimestamp.ShouldBeGreaterThanOrEqualTo(beforeCall);
        nowTimestamp.ShouldBeLessThanOrEqualTo(afterCall);
    }
    /// <summary>
    /// 测试 - UtcNow - 返回当前UTC时间戳
    /// </summary>
    [Fact]
    public void UtcNow_Always_ReturnsCurrentUtcTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow.Ticks;
        // Act
        var utcNowTimestamp = TimeStamp.UtcNow();
        // Assert
        var afterCall = DateTime.UtcNow.Ticks;
        utcNowTimestamp.ShouldBeGreaterThanOrEqualTo(beforeCall);
        utcNowTimestamp.ShouldBeLessThanOrEqualTo(afterCall);
    }
    /// <summary>
    /// 测试 - CreateNow - 创建当前时间的时间戳对象
    /// </summary>
    [Fact]
    public void CreateNow_Always_ReturnsCurrentTimeStampObject()
    {
        // Arrange
        var beforeCall = DateTime.Now;
        // Act
        var timeStamp = TimeStamp.CreateNow();
        // Assert
        var afterCall = DateTime.Now;
        var actualTime = timeStamp.ToDateTime();
        actualTime.ShouldBeGreaterThanOrEqualTo(beforeCall.AddMilliseconds(-100));
        actualTime.ShouldBeLessThanOrEqualTo(afterCall.AddMilliseconds(100));
    }
    /// <summary>
    /// 测试 - CreateUtcNow - 创建当前UTC时间的时间戳对象
    /// </summary>
    [Fact]
    public void CreateUtcNow_Always_ReturnsCurrentUtcTimeStampObject()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;
        // Act
        var timeStamp = TimeStamp.CreateUtcNow();
        // Assert
        var afterCall = DateTime.UtcNow;
        var actualTime = timeStamp.ToDateTime();
        actualTime.ShouldBeGreaterThanOrEqualTo(beforeCall.AddMilliseconds(-100));
        actualTime.ShouldBeLessThanOrEqualTo(afterCall.AddMilliseconds(100));
    }
    #endregion
    #region 实用工具方法测试
    /// <summary>
    /// 测试 - GetDifference - 计算时间差
    /// </summary>
    [Fact]
    public void GetDifference_WithValidTimeStamps_ReturnsCorrectDifference()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp1 = new TimeStamp(baseTime);
        var timeStamp2 = new TimeStamp(baseTime.AddHours(1));
        // Act
        var difference = timeStamp2.GetDifference(timeStamp1);
        // Assert
        difference.ShouldBe(TimeSpan.FromHours(1).Ticks);
    }
    /// <summary>
    /// 测试 - GetDifference - Null参数验证
    /// </summary>
    [Fact]
    public void GetDifference_WithNullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        var timeStamp = new TimeStamp();
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => timeStamp.GetDifference(null))
            .ParamName.ShouldBe("other");
    }
    /// <summary>
    /// 测试 - GetTimeSpan - 计算时间间隔
    /// </summary>
    [Fact]
    public void GetTimeSpan_WithValidTimeStamps_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp1 = new TimeStamp(baseTime);
        var timeStamp2 = new TimeStamp(baseTime.AddMinutes(30));
        // Act
        var timeSpan = timeStamp1.GetTimeSpan(timeStamp2);
        // Assert
        timeSpan.ShouldBe(TimeSpan.FromMinutes(30));
    }
    /// <summary>
    /// 测试 - Add - 添加时间间隔
    /// </summary>
    [Theory]
    [InlineData(1, 0, 0, 0)] // 1 day
    [InlineData(0, 2, 0, 0)] // 2 hours
    [InlineData(0, 0, 30, 0)] // 30 minutes
    [InlineData(0, 0, 0, 500)] // 500 milliseconds
    public void Add_WithTimeSpan_ReturnsCorrectResult(int days, int hours, int minutes, int milliseconds)
    {
        // Arrange
        var originalTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp = new TimeStamp(originalTime);
        var timeSpan = new TimeSpan(days, hours, minutes, 0, milliseconds);
        // Act
        var newTimeStamp = timeStamp.Add(timeSpan);
        // Assert
        var expectedTime = originalTime.Add(timeSpan);
        newTimeStamp.ToDateTime().ShouldBe(expectedTime);
    }
    /// <summary>
    /// 测试 - Subtract - 减去时间间隔
    /// </summary>
    [Fact]
    public void Subtract_WithTimeSpan_ReturnsCorrectResult()
    {
        // Arrange
        var originalTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp = new TimeStamp(originalTime);
        var timeSpan = TimeSpan.FromHours(3);
        // Act
        var newTimeStamp = timeStamp.Subtract(timeSpan);
        // Assert
        var expectedTime = originalTime.Subtract(timeSpan);
        newTimeStamp.ToDateTime().ShouldBe(expectedTime);
    }
    #endregion
    #region 格式化方法测试
    /// <summary>
    /// 测试 - ToString - 默认格式
    /// </summary>
    [Fact]
    public void ToString_DefaultFormat_ReturnsCorrectFormat()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45, 123);
        var timeStamp = new TimeStamp(dateTime);
        // Act
        var result = timeStamp.ToString();
        // Assert
        result.ShouldBe("2024-06-15 14:30:45.123");
    }
    /// <summary>
    /// 测试 - ToString - 自定义格式
    /// </summary>
    [Theory]
    [InlineData("yyyy-MM-dd", "2024-06-15")]
    [InlineData("HH:mm:ss", "14:30:45")]
    [InlineData("yyyy/MM/dd HH:mm", "2024/06/15 14:30")]
    [InlineData("yyyyMMddHHmmss", "20240615143045")]
    public void ToString_CustomFormat_ReturnsCorrectFormat(string format, string expected)
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45, 123);
        var timeStamp = new TimeStamp(dateTime);
        // Act
        var result = timeStamp.ToString(format);
        // Assert
        result.ShouldBe(expected);
    }
    #endregion
    #region 相等性和比较测试
    /// <summary>
    /// 测试 - Equals - 相等的时间戳
    /// </summary>
    [Fact]
    public void Equals_WithEqualTimeStamps_ReturnsTrue()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp1 = new TimeStamp(dateTime);
        var timeStamp2 = new TimeStamp(dateTime);
        // Act & Assert
        timeStamp1.Equals(timeStamp2).ShouldBeTrue();
        timeStamp1.Equals((object)timeStamp2).ShouldBeTrue();
        (timeStamp1 == timeStamp2).ShouldBeTrue();
        (timeStamp1 != timeStamp2).ShouldBeFalse();
    }
    /// <summary>
    /// 测试 - Equals - 不相等的时间戳
    /// </summary>
    [Fact]
    public void Equals_WithDifferentTimeStamps_ReturnsFalse()
    {
        // Arrange
        var timeStamp1 = new TimeStamp(new DateTime(2024, 1, 1, 12, 0, 0));
        var timeStamp2 = new TimeStamp(new DateTime(2024, 1, 1, 13, 0, 0));
        // Act & Assert
        timeStamp1.Equals(timeStamp2).ShouldBeFalse();
        timeStamp1.Equals((object)timeStamp2).ShouldBeFalse();
        (timeStamp1 == timeStamp2).ShouldBeFalse();
        (timeStamp1 != timeStamp2).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - Equals - Null对象
    /// </summary>
    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var timeStamp = new TimeStamp();
        // Act & Assert
        timeStamp.Equals(null).ShouldBeFalse();
        timeStamp.Equals((object)null).ShouldBeFalse();
        (timeStamp == null).ShouldBeFalse();
        (null == timeStamp).ShouldBeFalse();
        (timeStamp != null).ShouldBeTrue();
        (null != timeStamp).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - CompareTo - 时间戳比较
    /// </summary>
    [Fact]
    public void CompareTo_WithDifferentTimeStamps_ReturnsCorrectComparison()
    {
        // Arrange
        var early = new TimeStamp(new DateTime(2024, 1, 1, 12, 0, 0));
        var late = new TimeStamp(new DateTime(2024, 1, 1, 13, 0, 0));
        // Act & Assert
        early.CompareTo(late).ShouldBeLessThan(0);
        late.CompareTo(early).ShouldBeGreaterThan(0);
        early.CompareTo(early).ShouldBe(0);
        // 运算符测试
        (early < late).ShouldBeTrue();
        (late > early).ShouldBeTrue();
        (early <= late).ShouldBeTrue();
        (late >= early).ShouldBeTrue();
        (early <= early).ShouldBeTrue();
        (early >= early).ShouldBeTrue();
    }
    /// <summary>
    /// 测试 - GetHashCode - 相等对象具有相同哈希码
    /// </summary>
    [Fact]
    public void GetHashCode_EqualObjects_ReturnsSameHashCode()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp1 = new TimeStamp(dateTime);
        var timeStamp2 = new TimeStamp(dateTime);
        // Act & Assert
        timeStamp1.GetHashCode().ShouldBe(timeStamp2.GetHashCode());
    }
    #endregion
    #region 运算符重载测试
    /// <summary>
    /// 测试 - 加法运算符 - 时间戳加时间间隔
    /// </summary>
    [Fact]
    public void AdditionOperator_WithTimeSpan_ReturnsCorrectResult()
    {
        // Arrange
        var originalTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp = new TimeStamp(originalTime);
        var timeSpan = TimeSpan.FromHours(2);
        // Act
        var result = timeStamp + timeSpan;
        // Assert
        result.ToDateTime().ShouldBe(originalTime.Add(timeSpan));
    }
    /// <summary>
    /// 测试 - 减法运算符 - 时间戳减时间间隔
    /// </summary>
    [Fact]
    public void SubtractionOperator_WithTimeSpan_ReturnsCorrectResult()
    {
        // Arrange
        var originalTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var timeStamp = new TimeStamp(originalTime);
        var timeSpan = TimeSpan.FromMinutes(30);
        // Act
        var result = timeStamp - timeSpan;
        // Assert
        result.ToDateTime().ShouldBe(originalTime.Subtract(timeSpan));
    }
    /// <summary>
    /// 测试 - 减法运算符 - 时间戳减时间戳
    /// </summary>
    [Fact]
    public void SubtractionOperator_WithTimeStamp_ReturnsTimeSpan()
    {
        // Arrange
        var time1 = new DateTime(2024, 1, 1, 12, 0, 0);
        var time2 = new DateTime(2024, 1, 1, 14, 30, 0);
        var timeStamp1 = new TimeStamp(time1);
        var timeStamp2 = new TimeStamp(time2);
        // Act
        var result = timeStamp2 - timeStamp1;
        // Assert
        result.ShouldBe(TimeSpan.FromHours(2.5));
    }
    /// <summary>
    /// 测试 - 运算符 - Null参数验证
    /// </summary>
    [Fact]
    public void Operators_WithNullParameters_ThrowArgumentNullException()
    {
        // Arrange
        TimeStamp nullTimeStamp = null;
        var timeStamp = new TimeStamp();
        var timeSpan = TimeSpan.FromHours(1);
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => nullTimeStamp + timeSpan);
        Should.Throw<ArgumentNullException>(() => nullTimeStamp - timeSpan);
        Should.Throw<ArgumentNullException>(() => nullTimeStamp - timeStamp);
        Should.Throw<ArgumentNullException>(() => timeStamp - nullTimeStamp);
    }
    #endregion
    #region 边界条件测试
    /// <summary>
    /// 测试 - 最小和最大时间戳值
    /// </summary>
    [Fact]
    public void BoundaryValues_MinAndMaxTimestamps_WorkCorrectly()
    {
        // Arrange & Act
        var minTimeStamp = new TimeStamp(0);
        var maxTimeStamp = new TimeStamp(DateTime.MaxValue.Ticks);
        // Assert
        minTimeStamp.ToTimestamp().ShouldBe(0);
        maxTimeStamp.ToTimestamp().ShouldBe(DateTime.MaxValue.Ticks);
        minTimeStamp.ToDateTime().ShouldBe(DateTime.MinValue);
        maxTimeStamp.ToDateTime().ShouldBe(DateTime.MaxValue);
    }
    /// <summary>
    /// 测试 - 时间戳精度 - Ticks级别
    /// </summary>
    [Fact]
    public void Precision_TickLevel_MaintainsAccuracy()
    {
        // Arrange
        var originalTicks = 638000000000001234L; // 包含微秒精度
        var timeStamp = new TimeStamp(originalTicks);
        // Act & Assert
        timeStamp.ToTimestamp().ShouldBe(originalTicks);
        timeStamp.ToDateTime().Ticks.ShouldBe(originalTicks);
    }
    #endregion
    #region 兼容性测试
    /// <summary>
    /// 测试 - NowTimeStamp - 过时属性兼容性
    /// </summary>
    [Fact]
    public void NowTimeStamp_ObsoleteProperty_StillWorksCorrectly()
    {
        // Arrange
        var beforeCall = DateTime.Now.Ticks;
        // Act
#pragma warning disable CS0618 // 类型或成员已过时
        var timestamp = TimeStamp.NowTimeStamp();
#pragma warning restore CS0618 // 类型或成员已过时
        // Assert
        var afterCall = DateTime.Now.Ticks;
        timestamp.ShouldBeGreaterThanOrEqualTo(beforeCall);
        timestamp.ShouldBeLessThanOrEqualTo(afterCall);
    }
    /// <summary>
    /// 测试 - UtcNowTimeStamp - 过时属性兼容性
    /// </summary>
    [Fact]
    public void UtcNowTimeStamp_ObsoleteProperty_StillWorksCorrectly()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow.Ticks;
        // Act
#pragma warning disable CS0618 // 类型或成员已过时
        var timestamp = TimeStamp.UtcNowTimeStamp();
#pragma warning restore CS0618 // 类型或成员已过时
        // Assert
        var afterCall = DateTime.UtcNow.Ticks;
        timestamp.ShouldBeGreaterThanOrEqualTo(beforeCall);
        timestamp.ShouldBeLessThanOrEqualTo(afterCall);
    }
    #endregion
    #region 性能测试
    /// <summary>
    /// 测试 - 构造函数性能
    /// </summary>
    [Fact]
    public void Performance_Constructor_CompletesQuickly()
    {
        // Arrange
        const int iterations = 100000;
        var dateTime = DateTime.Now;
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var _ = new TimeStamp(dateTime);
            }
        }, TimeSpan.FromSeconds(1), $"创建{iterations}个TimeStamp实例应该在1秒内完成");
    }
    /// <summary>
    /// 测试 - 转换方法性能
    /// </summary>
    [Fact]
    public void Performance_ConversionMethods_CompleteQuickly()
    {
        // Arrange
        const int iterations = 100000;
        var timeStamp = new TimeStamp();
        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var _ = timeStamp.ToDateTime();
                var __ = timeStamp.ToTimestamp();
            }
        }, TimeSpan.FromSeconds(1), $"执行{iterations}次转换操作应该在1秒内完成");
    }
    #endregion
    #region 集成测试
    /// <summary>
    /// 测试 - 实际使用场景 - 时间序列处理
    /// </summary>
    [Fact]
    public void RealWorldScenario_TimeSeriesProcessing_WorksCorrectly()
    {
        // Arrange - 模拟时间序列数据处理
        var baseTime = new DateTime(2024, 1, 1, 0, 0, 0);
        var timeStamps = new List<TimeStamp>();
        // Act - 创建时间序列
        for (int i = 0; i < 24; i++)
        {
            var hourlyTime = baseTime.AddHours(i);
            timeStamps.Add(new TimeStamp(hourlyTime));
        }
        // Assert - 验证时间序列的完整性
        timeStamps.Count.ShouldBe(24);
        // 验证时间递增性
        for (int i = 1; i < timeStamps.Count; i++)
        {
            timeStamps[i].ShouldBeGreaterThan(timeStamps[i - 1]);
            var diff = timeStamps[i] - timeStamps[i - 1];
            diff.ShouldBe(TimeSpan.FromHours(1));
        }
        // 验证总时间跨度
        var totalSpan = timeStamps.Last() - timeStamps.First();
        totalSpan.ShouldBe(TimeSpan.FromHours(23));
    }
    /// <summary>
    /// 测试 - 实际使用场景 - 时间戳比较和排序
    /// </summary>
    [Fact]
    public void RealWorldScenario_TimestampSortingAndComparison_WorksCorrectly()
    {
        // Arrange - 创建随机时间戳
        var random = new Random(42); // 固定种子确保测试可重复
        var timeStamps = new List<TimeStamp>();
        var baseTime = new DateTime(2024, 1, 1);
        for (int i = 0; i < 100; i++)
        {
            var randomTime = baseTime.AddHours(random.Next(0, 8760)); // 一年内的随机时间
            timeStamps.Add(new TimeStamp(randomTime));
        }
        // Act - 排序
        var sortedTimeStamps = timeStamps.OrderBy(ts => ts).ToList();
        // Assert - 验证排序结果
        for (int i = 1; i < sortedTimeStamps.Count; i++)
        {
            sortedTimeStamps[i].ShouldBeGreaterThanOrEqualTo(sortedTimeStamps[i - 1]);
        }
        // 验证最早和最晚的时间戳
        var earliest = sortedTimeStamps.First();
        var latest = sortedTimeStamps.Last();
        foreach (var ts in sortedTimeStamps)
        {
            ts.ShouldBeGreaterThanOrEqualTo(earliest);
            ts.ShouldBeLessThanOrEqualTo(latest);
        }
    }
    #endregion
}
