using System.Collections.Concurrent;
using System.Globalization;

namespace Bing.Date;

/// <summary>
/// Unix时间戳类测试
/// </summary>
[Trait("Bing.Date", "UnixTimeStamp")]
public class UnixTimeStampTest
{
    #region 常量测试

    /// <summary>
    /// 测试 - 常量定义 - 验证Unix纪元和边界值
    /// </summary>
    [Fact]
    public void Constants_UnixEpochAndBoundaries_AreCorrect()
    {
        // Assert
        UnixTimeStamp.UnixEpoch.ShouldBe(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        UnixTimeStamp.MinUnixTimestamp.ShouldBe(0L);
        UnixTimeStamp.MaxUnixTimestamp32Bit.ShouldBe(2147483647L);
        UnixTimeStamp.MaxUnixTimestamp.ShouldBeGreaterThan(UnixTimeStamp.MaxUnixTimestamp32Bit);
    }

    /// <summary>
    /// 测试 - 常量计算 - 验证MaxUnixTimestamp延迟计算
    /// </summary>
    [Fact]
    public void Constants_MaxUnixTimestamp_CalculatedCorrectly()
    {
        // Act
        var maxTimestamp1 = UnixTimeStamp.MaxUnixTimestamp;
        var maxTimestamp2 = UnixTimeStamp.MaxUnixTimestamp;

        // Assert
        maxTimestamp1.ShouldBe(maxTimestamp2); // 验证缓存机制
        maxTimestamp1.ShouldBeGreaterThan(0);

        // 验证计算结果的合理性
        var expectedMax = (long)(DateTime.MaxValue.ToUniversalTime() - UnixTimeStamp.UnixEpoch).TotalSeconds;
        maxTimestamp1.ShouldBe(expectedMax);
    }

    #endregion

    #region 构造函数测试

    /// <summary>
    /// 测试 - 默认构造函数 - 使用当前时间
    /// </summary>
    [Fact]
    public void Constructor_Default_CreatesWithCurrentTime()
    {
        // Arrange
        var beforeCreation = UnixTimeStamp.Now();

        // Act
        var unixTimeStamp = new UnixTimeStamp();

        // Assert
        var afterCreation = UnixTimeStamp.Now();
        var actualTimestamp = unixTimeStamp.ToTimestamp();

        actualTimestamp.ShouldBeGreaterThanOrEqualTo(beforeCreation - 1);
        actualTimestamp.ShouldBeLessThanOrEqualTo(afterCreation + 1);
    }

    /// <summary>
    /// 测试 - Unix时间戳构造函数 - 有效时间戳
    /// </summary>
    [Theory]
    [InlineData(0L)]                    // Unix epoch
    [InlineData(1640995200L)]          // 2022-01-01 00:00:00 UTC
    [InlineData(1672531200L)]          // 2023-01-01 00:00:00 UTC
    [InlineData(2147483647L)]          // 2038年问题边界
    public void Constructor_WithValidUnixTimestamp_CreatesCorrectly(long unixTimestamp)
    {
        // Act
        var unixTimeStampObj = new UnixTimeStamp(unixTimestamp);

        // Assert
        unixTimeStampObj.ToTimestamp().ShouldBe(unixTimestamp);

        var expectedUtcTime = UnixTimeStamp.UnixEpoch.AddSeconds(unixTimestamp);
        var actualUtcTime = unixTimeStampObj.ToUtcDateTime();
        actualUtcTime.ShouldBe(expectedUtcTime);
    }

    /// <summary>
    /// 测试 - Unix时间戳构造函数 - 无效时间戳
    /// </summary>
    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void Constructor_WithInvalidUnixTimestamp_ThrowsArgumentOutOfRangeException(long invalidTimestamp)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new UnixTimeStamp(invalidTimestamp))
            .ParamName.ShouldBe("timestamp");
    }

    /// <summary>
    /// 测试 - DateTime构造函数 - 各种时区
    /// </summary>
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_WithDateTime_HandlesTimeZonesCorrectly(DateTimeKind kind)
    {
        // Arrange
        var baseUtcTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var testDateTime = kind switch
        {
            DateTimeKind.Utc => baseUtcTime,
            DateTimeKind.Local => baseUtcTime.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(baseUtcTime, DateTimeKind.Unspecified),
            _ => throw new ArgumentException("Invalid DateTimeKind")
        };

        // Act
        var unixTimeStamp = new UnixTimeStamp(testDateTime);

        // Assert
        var expectedUnixTimestamp = (long)(baseUtcTime - UnixTimeStamp.UnixEpoch).TotalSeconds;
        unixTimeStamp.ToTimestamp().ShouldBe(expectedUnixTimestamp);
    }

    /// <summary>
    /// 测试 - 受保护构造函数 - 直接设置时间和时间戳
    /// </summary>
    [Fact]
    public void Constructor_Protected_SetsValuesCorrectly()
    {
        // Arrange
        var testDateTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var testTimestamp = 1704110400L; // 对应的Unix时间戳

        // Act - 通过公共构造函数间接测试受保护构造函数
        var unixTimeStamp = new UnixTimeStamp(testTimestamp);

        // Assert
        unixTimeStamp.ToTimestamp().ShouldBe(testTimestamp);
        Assert.NotNull(unixTimeStamp.ToDateTime());
    }


    #endregion

    #region 转换方法测试

    /// <summary>
    /// 测试 - ToDateTime - 返回正确的本地时间
    /// </summary>
    [Fact]
    public void ToDateTime_Always_ReturnsCorrectLocalTime()
    {
        // Arrange
        var unixTimestamp = 1640995200L; // 2022-01-01 00:00:00 UTC
        var expectedUtcTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedLocalTime = expectedUtcTime.ToLocalTime();
        var unixTimeStamp = new UnixTimeStamp(unixTimestamp);

        // Act
        var actualLocalTime = unixTimeStamp.ToDateTime();

        // Assert
        actualLocalTime.ShouldBe(expectedLocalTime);
    }

    /// <summary>
    /// 测试 - ToUtcDateTime - 返回正确的UTC时间
    /// </summary>
    [Fact]
    public void ToUtcDateTime_Always_ReturnsCorrectUtcTime()
    {
        // Arrange
        var unixTimestamp = 1640995200L; // 2022-01-01 00:00:00 UTC
        var expectedUtcTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var unixTimeStamp = new UnixTimeStamp(unixTimestamp);

        // Act
        var actualUtcTime = unixTimeStamp.ToUtcDateTime();

        // Assert
        actualUtcTime.ShouldBe(expectedUtcTime);
    }

    /// <summary>
    /// 测试 - ToTimestamp - 返回正确的Unix时间戳
    /// </summary>
    [Fact]
    public void ToTimestamp_Always_ReturnsCorrectUnixTimestamp()
    {
        // Arrange
        var expectedTimestamp = 1640995200L;
        var unixTimeStamp = new UnixTimeStamp(expectedTimestamp);

        // Act
        var actualTimestamp = unixTimeStamp.ToTimestamp();

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
        var originalUnixTimestamp = 1640995200L;
        var originalDateTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act - Unix timestamp -> UnixTimeStamp -> Unix timestamp
        var timeStampFromUnix = new UnixTimeStamp(originalUnixTimestamp);
        var convertedUnixTimestamp = timeStampFromUnix.ToTimestamp();

        // Act - DateTime -> UnixTimeStamp -> DateTime
        var timeStampFromDateTime = new UnixTimeStamp(originalDateTime);
        var convertedUtcDateTime = timeStampFromDateTime.ToUtcDateTime();

        // Assert
        convertedUnixTimestamp.ShouldBe(originalUnixTimestamp);
        convertedUtcDateTime.ShouldBe(originalDateTime);
        timeStampFromUnix.ToTimestamp().ShouldBe(timeStampFromDateTime.ToTimestamp());
    }

    #endregion

    #region 静态方法测试

    /// <summary>
    /// 测试 - Now - 返回当前Unix时间戳
    /// </summary>
    [Fact]
    public void Now_Always_ReturnsCurrentUnixTimestamp()
    {
        // Arrange
        var beforeCall = (long)(DateTime.Now.ToUniversalTime() - UnixTimeStamp.UnixEpoch).TotalSeconds;

        // Act
        var nowTimestamp = UnixTimeStamp.Now();

        // Assert
        var afterCall = (long)(DateTime.Now.ToUniversalTime() - UnixTimeStamp.UnixEpoch).TotalSeconds;
        nowTimestamp.ShouldBeGreaterThanOrEqualTo(beforeCall);
        nowTimestamp.ShouldBeLessThanOrEqualTo(afterCall);
    }

    /// <summary>
    /// 测试 - UtcNow - 返回当前UTC Unix时间戳
    /// </summary>
    [Fact]
    public void UtcNow_Always_ReturnsCurrentUtcUnixTimestamp()
    {
        // Arrange
        var beforeCall = (long)(DateTime.UtcNow - UnixTimeStamp.UnixEpoch).TotalSeconds;

        // Act
        var utcNowTimestamp = UnixTimeStamp.UtcNow();

        // Assert
        var afterCall = (long)(DateTime.UtcNow - UnixTimeStamp.UnixEpoch).TotalSeconds;
        utcNowTimestamp.ShouldBeGreaterThanOrEqualTo(beforeCall);
        utcNowTimestamp.ShouldBeLessThanOrEqualTo(afterCall);
    }

    /// <summary>
    /// 测试 - CreateNow和CreateUtcNow - 创建时间戳对象
    /// </summary>
    [Fact]
    public void CreateMethods_Always_ReturnValidTimeStampObjects()
    {
        // Act
        var nowStamp = UnixTimeStamp.CreateNow();
        var utcNowStamp = UnixTimeStamp.CreateUtcNow();

        // Assert
        nowStamp.ShouldNotBeNull();
        utcNowStamp.ShouldNotBeNull();

        var timeDiff = Math.Abs(nowStamp.ToTimestamp() - utcNowStamp.ToTimestamp());
        timeDiff.ShouldBeLessThanOrEqualTo(1); // 应该在1秒内
    }

    /// <summary>
    /// 测试 - FromUnixTime - 从Unix时间戳创建
    /// </summary>
    [Fact]
    public void FromUnixTime_WithValidTimestamp_CreatesCorrectly()
    {
        // Arrange
        var unixTimestamp = 1640995200L;

        // Act
        var unixTimeStamp = UnixTimeStamp.FromUnixTime(unixTimestamp);

        // Assert
        unixTimeStamp.ToTimestamp().ShouldBe(unixTimestamp);
    }

    /// <summary>
    /// 测试 - FromUnixTimeMilliseconds - 从毫秒级时间戳创建
    /// </summary>
    [Fact]
    public void FromUnixTimeMilliseconds_WithValidTimestamp_CreatesCorrectly()
    {
        // Arrange
        var millisecondsTimestamp = 1640995200000L; // 毫秒级
        var expectedSeconds = 1640995200L;

        // Act
        var unixTimeStamp = UnixTimeStamp.FromUnixTimeMilliseconds(millisecondsTimestamp);

        // Assert
        unixTimeStamp.ToTimestamp().ShouldBe(expectedSeconds);
    }

    /// <summary>
    /// 测试 - FromUnixTimeMilliseconds - 毫秒精度丢失处理
    /// </summary>
    [Theory]
    [InlineData(1640995200500L, 1640995200L)]  // 500毫秒被截断
    [InlineData(1640995200999L, 1640995200L)]  // 999毫秒被截断
    [InlineData(1640995201001L, 1640995201L)]  // 1毫秒被截断
    public void FromUnixTimeMilliseconds_WithMillisecondPrecision_TruncatesCorrectly(long millisecondsTimestamp, long expectedSeconds)
    {
        // Act
        var unixTimeStamp = UnixTimeStamp.FromUnixTimeMilliseconds(millisecondsTimestamp);

        // Assert
        unixTimeStamp.ToTimestamp().ShouldBe(expectedSeconds);
    }

    #endregion

    #region 实用工具方法测试

    /// <summary>
    /// 测试 - ToUnixTimeMilliseconds - 转换为毫秒级时间戳
    /// </summary>
    [Fact]
    public void ToUnixTimeMilliseconds_Always_ReturnsCorrectValue()
    {
        // Arrange
        var unixTimestamp = 1640995200L;
        var expectedMilliseconds = 1640995200000L;
        var unixTimeStamp = new UnixTimeStamp(unixTimestamp);

        // Act
        var actualMilliseconds = unixTimeStamp.ToUnixTimeMilliseconds();

        // Assert
        actualMilliseconds.ShouldBe(expectedMilliseconds);
    }

    /// <summary>
    /// 测试 - IsAfter2038Problem - 检查2038年问题
    /// </summary>
    [Theory]
    [InlineData(2147483647L, false)]   // 2038年边界，刚好不超过
    [InlineData(2147483648L, true)]    // 超过2038年边界
    [InlineData(1640995200L, false)]   // 2022年，远未到2038年
    public void IsAfter2038Problem_WithDifferentTimestamps_ReturnsCorrectResult(long timestamp, bool expectedResult)
    {
        // Arrange
        var unixTimeStamp = new UnixTimeStamp(timestamp);

        // Act
        var result = unixTimeStamp.IsAfter2038Problem();

        // Assert
        result.ShouldBe(expectedResult);
    }

    /// <summary>
    /// 测试 - GetDaysSinceEpoch - 计算距离纪元的天数
    /// </summary>
    [Theory]
    [InlineData(0L, 0)]                    // Unix epoch: 1970-01-01
    [InlineData(86400L, 1)]                // 1970-01-02: 1天后
    [InlineData(172800L, 2)]               // 1970-01-03: 2天后
    [InlineData(1640995200L, 18993)]       // 2022-01-01: 正确的天数
    [InlineData(1609459200L, 18628)]       // 2021-01-01: 验证另一个年份
    [InlineData(1577836800L, 18262)]       // 2020-01-01: 闰年验证
    [InlineData(946684800L, 10957)]        // 2000-01-01: 世纪交替验证
    public void GetDaysSinceEpoch_WithDifferentTimestamps_ReturnsCorrectDays(long timestamp, int expectedDays)
    {
        // Arrange
        var unixTimeStamp = new UnixTimeStamp(timestamp);

        // Act
        var actualDays = unixTimeStamp.GetDaysSinceEpoch();

        // Assert
        actualDays.ShouldBe(expectedDays);
    }

    /// <summary>
    /// 测试 - Add方法系列 - 时间加法运算
    /// </summary>
    [Theory]
    [InlineData(3600L, "AddSeconds")]   // 1小时
    [InlineData(60L, "AddMinutes")]     // 1分钟转秒
    [InlineData(1L, "AddHours")]        // 1小时转秒
    [InlineData(1L, "AddDays")]         // 1天转秒
    public void AddMethods_WithPositiveValues_ReturnsCorrectResult(long value, string methodName)
    {
        // Arrange
        var baseTimestamp = 1640995200L; // 2022-01-01 00:00:00 UTC
        var unixTimeStamp = new UnixTimeStamp(baseTimestamp);

        var expectedTimestamp = methodName switch
        {
            "AddSeconds" => baseTimestamp + value,
            "AddMinutes" => baseTimestamp + value * 60,
            "AddHours" => baseTimestamp + value * 3600,
            "AddDays" => baseTimestamp + value * 86400,
            _ => baseTimestamp
        };

        // Act
        var result = methodName switch
        {
            "AddSeconds" => unixTimeStamp.AddSeconds(value),
            "AddMinutes" => unixTimeStamp.AddMinutes(value),
            "AddHours" => unixTimeStamp.AddHours(value),
            "AddDays" => unixTimeStamp.AddDays(value),
            _ => unixTimeStamp
        };

        // Assert
        result.ToTimestamp().ShouldBe(expectedTimestamp);
    }

    /// <summary>
    /// 测试 - Add方法系列 - 负值处理
    /// </summary>
    [Theory]
    [InlineData(-3600L, "AddSeconds")]   // 减1小时
    [InlineData(-60L, "AddMinutes")]     // 减1分钟
    [InlineData(-1L, "AddHours")]        // 减1小时
    [InlineData(-1L, "AddDays")]         // 减1天
    public void AddMethods_WithNegativeValues_ReturnsCorrectResult(long value, string methodName)
    {
        // Arrange
        var baseTimestamp = 1640995200L; // 2022-01-01 00:00:00 UTC
        var unixTimeStamp = new UnixTimeStamp(baseTimestamp);

        // Act
        var result = methodName switch
        {
            "AddSeconds" => unixTimeStamp.AddSeconds(value),
            "AddMinutes" => unixTimeStamp.AddMinutes(value),
            "AddHours" => unixTimeStamp.AddHours(value),
            "AddDays" => unixTimeStamp.AddDays(value),
            _ => unixTimeStamp
        };

        // Assert
        result.ToTimestamp().ShouldBeLessThan(baseTimestamp);
        result.ShouldBeOfType<UnixTimeStamp>();
    }

    /// <summary>
    /// 测试 - Add和Subtract方法 - TimeSpan重载
    /// </summary>
    [Fact]
    public void AddSubtractWithTimeSpan_ReturnsUnixTimeStampType()
    {
        // Arrange
        var unixTimeStamp = new UnixTimeStamp(1640995200L);
        var timeSpan = TimeSpan.FromHours(2);

        // Act
        var addResult = unixTimeStamp.Add(timeSpan);
        var subtractResult = unixTimeStamp.Subtract(timeSpan);

        // Assert
        addResult.ShouldBeOfType<UnixTimeStamp>();
        subtractResult.ShouldBeOfType<UnixTimeStamp>();

        var addedUnix = addResult as UnixTimeStamp;
        var subtractedUnix = subtractResult as UnixTimeStamp;

        addedUnix.ToTimestamp().ShouldBe(unixTimeStamp.ToTimestamp() + (long)timeSpan.TotalSeconds);
        subtractedUnix.ToTimestamp().ShouldBe(unixTimeStamp.ToTimestamp() - (long)timeSpan.TotalSeconds);
    }

    /// <summary>
    /// 测试 - Add和Subtract方法 - 精度处理
    /// </summary>
    [Fact]
    public void AddSubtractWithTimeSpan_HandlesMillisecondPrecision()
    {
        // Arrange
        var unixTimeStamp = new UnixTimeStamp(1640995200L);
        var timeSpanWithMilliseconds = new TimeSpan(0, 0, 0, 1, 500); // 1.5秒

        // Act
        var addResult = unixTimeStamp.Add(timeSpanWithMilliseconds) as UnixTimeStamp;

        // Assert
        // 由于Unix时间戳是秒级精度，毫秒部分会被截断
        addResult.ToTimestamp().ShouldBe(unixTimeStamp.ToTimestamp() + 1);
    }

    #endregion

    #region 格式化方法测试

    /// <summary>
    /// 测试 - ToIso8601String - ISO 8601格式
    /// </summary>
    [Fact]
    public void ToIso8601String_Always_ReturnsCorrectFormat()
    {
        // Arrange
        var unixTimestamp = 1640995200L; // 2022-01-01 00:00:00 UTC
        var unixTimeStamp = new UnixTimeStamp(unixTimestamp);

        // Act
        var result = unixTimeStamp.ToIso8601String();

        // Assert
        result.ShouldBe("2022-01-01T00:00:00Z");
    }

    /// <summary>
    /// 测试 - ToRfc2822String - RFC 2822格式
    /// </summary>
    [Fact]
    public void ToRfc2822String_Always_ReturnsCorrectFormat()
    {
        // Arrange
        var unixTimestamp = 1640995200L; // 2022-01-01 00:00:00 UTC (Saturday)
        var unixTimeStamp = new UnixTimeStamp(unixTimestamp);

        // Act
        var result = unixTimeStamp.ToRfc2822String();

        // Assert
        result.ShouldBe("Sat, 01 Jan 2022 00:00:00 GMT");
    }

    /// <summary>
    /// 测试 - 格式化方法 - 文化无关性
    /// </summary>
    [Fact]
    public void FormatMethods_Always_UseCultureInvariant()
    {
        // Arrange
        var unixTimestamp = 1640995200L;
        var unixTimeStamp = new UnixTimeStamp(unixTimestamp);
        var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;

        try
        {
            // 设置不同的文化
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");

            // Act
            var iso8601 = unixTimeStamp.ToIso8601String();
            var rfc2822 = unixTimeStamp.ToRfc2822String();

            // Assert
            iso8601.ShouldBe("2022-01-01T00:00:00Z");
            rfc2822.ShouldBe("Sat, 01 Jan 2022 00:00:00 GMT");
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// 测试 - ToRelativeString - 相对时间字符串
    /// </summary>
    [Theory]
    [InlineData(-30, "30秒前")]
    [InlineData(-120, "2分钟前")]
    [InlineData(-7200, "2小时前")]
    [InlineData(-172800, "2天前")]
    [InlineData(30, "30秒后")]
    [InlineData(3600, "1小时后")]
    public void ToRelativeString_WithDifferentTimeOffsets_ReturnsCorrectRelativeString(long offsetSeconds, string expectedPattern)
    {
        // Arrange
        var currentTimestamp = UnixTimeStamp.UtcNow();
        var testTimestamp = currentTimestamp + offsetSeconds;
        var unixTimeStamp = new UnixTimeStamp(testTimestamp);

        // Act
        var result = unixTimeStamp.ToRelativeString();

        // Assert
        result.ShouldContain(expectedPattern.Split('前', '后')[0]); // 检查数值和单位
    }

    /// <summary>
    /// 测试 - ToRelativeString - 边界值处理
    /// </summary>
    [Theory]
    [InlineData(59, "59秒后")]           // 秒的边界
    [InlineData(3599, "59分钟后")]       // 分钟的边界
    [InlineData(86399, "23小时后")]      // 小时的边界
    [InlineData(2591999, "29天后")]      // 天的边界
    [InlineData(31535999, "12个月后")]   // 月的边界
    public void ToRelativeString_WithBoundaryValues_ReturnsCorrectUnit(long offsetSeconds, string expectedPattern)
    {
        // Arrange
        var currentTimestamp = UnixTimeStamp.UtcNow();
        var testTimestamp = currentTimestamp + offsetSeconds;
        var unixTimeStamp = new UnixTimeStamp(testTimestamp);

        // Act
        var result = unixTimeStamp.ToRelativeString();

        // Assert
        var expectedNumber = expectedPattern.Split('秒', '分', '小', '天', '个', '年')[0];
        result.ShouldContain(expectedNumber);
    }

    #endregion

    #region 继承关系测试

    /// <summary>
    /// 测试 - 继承关系 - UnixTimeStamp继承自TimeStamp
    /// </summary>
    [Fact]
    public void Inheritance_UnixTimeStampExtendsTimeStamp_WorksCorrectly()
    {
        // Arrange & Act
        var unixTimeStamp = new UnixTimeStamp();

        // Assert
        unixTimeStamp.ShouldBeOfType<UnixTimeStamp>();
        unixTimeStamp.ShouldBeAssignableTo<TimeStamp>();

        // 验证可以调用基类方法
        var dateTime = unixTimeStamp.ToDateTime();
        var timestamp = unixTimeStamp.ToTimestamp();

        dateTime.ShouldBeOfType<DateTime>();
        timestamp.ShouldBeOfType<long>();
    }

    /// <summary>
    /// 测试 - 多态性 - 基类引用指向派生类对象
    /// </summary>
    [Fact]
    public void Polymorphism_BaseReferenceToUnixTimeStamp_WorksCorrectly()
    {
        // Arrange
        TimeStamp baseRef = new UnixTimeStamp(1640995200L);

        // Act
        var dateTime = baseRef.ToDateTime();
        var timestamp = baseRef.ToTimestamp();

        // Assert
        timestamp.ShouldBe(1640995200L);
        Assert.NotNull(dateTime);

        // 验证运行时类型
        baseRef.ShouldBeOfType<UnixTimeStamp>();
    }

    /// <summary>
    /// 测试 - 静态方法隐藏 - new关键字的正确行为
    /// </summary>
    [Fact]
    public void StaticMethodHiding_NewKeyword_WorksCorrectly()
    {
        // Act
        var timeStampNow = TimeStamp.Now();
        var unixTimeStampNow = UnixTimeStamp.Now();

        // Assert
        // UnixTimeStamp.Now()应该返回Unix时间戳，而不是.NET Ticks
        unixTimeStampNow.ShouldBeLessThan(timeStampNow); // Unix时间戳比Ticks要小得多

        // 验证Unix时间戳的合理范围（应该在当前年份附近）
        var currentYear = DateTime.Now.Year;
        var expectedUnixRange = (long)(new DateTime(currentYear - 1, 1, 1) - UnixTimeStamp.UnixEpoch).TotalSeconds;
        unixTimeStampNow.ShouldBeGreaterThan(expectedUnixRange);
    }

    /// <summary>
    /// 测试 - 方法重写 - virtual方法的正确重写
    /// </summary>
    [Fact]
    public void MethodOverride_VirtualMethods_OverriddenCorrectly()
    {
        // Arrange
        var unixTimeStamp = new UnixTimeStamp(1640995200L);
        var timeSpan = TimeSpan.FromHours(1);

        // Act
        TimeStamp baseAddResult = unixTimeStamp.Add(timeSpan);
        TimeStamp baseSubtractResult = unixTimeStamp.Subtract(timeSpan);

        // Assert
        baseAddResult.ShouldBeOfType<UnixTimeStamp>();
        baseSubtractResult.ShouldBeOfType<UnixTimeStamp>();

        // 验证多态调用的正确性
        var addedUnix = (UnixTimeStamp)baseAddResult;
        var subtractedUnix = (UnixTimeStamp)baseSubtractResult;

        addedUnix.ToTimestamp().ShouldBe(1640995200L + 3600L);
        subtractedUnix.ToTimestamp().ShouldBe(1640995200L - 3600L);
    }

    #endregion

    #region 边界条件测试

    /// <summary>
    /// 测试 - Unix纪元时间处理
    /// </summary>
    [Fact]
    public void UnixEpoch_ZeroTimestamp_HandlesCorrectly()
    {
        // Arrange & Act
        var epochStamp = new UnixTimeStamp(0);

        // Assert
        epochStamp.ToTimestamp().ShouldBe(0);
        epochStamp.ToUtcDateTime().ShouldBe(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        epochStamp.GetDaysSinceEpoch().ShouldBe(0);
    }

    /// <summary>
    /// 测试 - 2038年问题边界
    /// </summary>
    [Fact]
    public void Year2038Problem_BoundaryTimestamp_HandlesCorrectly()
    {
        // Arrange
        var boundaryTimestamp = UnixTimeStamp.MaxUnixTimestamp32Bit;
        var unixTimeStamp = new UnixTimeStamp(boundaryTimestamp);

        // Act & Assert
        unixTimeStamp.IsAfter2038Problem().ShouldBeFalse();
        unixTimeStamp.ToTimestamp().ShouldBe(boundaryTimestamp);

        // 2038-01-19 03:14:07 UTC
        var expectedUtcTime = new DateTime(2038, 1, 19, 3, 14, 7, DateTimeKind.Utc);
        unixTimeStamp.ToUtcDateTime().ShouldBe(expectedUtcTime);
    }

    /// <summary>
    /// 测试 - 最大Unix时间戳处理
    /// </summary>
    [Fact]
    public void MaxUnixTimestamp_LargeValue_HandlesCorrectly()
    {
        // Arrange
        var maxTimestamp = UnixTimeStamp.MaxUnixTimestamp;

        // Act & Assert
        Should.NotThrow(() => new UnixTimeStamp(maxTimestamp));

        var unixTimeStamp = new UnixTimeStamp(maxTimestamp);
        unixTimeStamp.IsAfter2038Problem().ShouldBeTrue();
        unixTimeStamp.ToTimestamp().ShouldBe(maxTimestamp);
    }

    /// <summary>
    /// 测试 - 超出最大值的时间戳
    /// </summary>
    [Fact]
    public void OverMaxTimestamp_ThrowsException()
    {
        // Arrange
        var overMaxTimestamp = UnixTimeStamp.MaxUnixTimestamp + 1;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new UnixTimeStamp(overMaxTimestamp))
            .ParamName.ShouldBe("timestamp");
    }

    /// <summary>
    /// 测试 - 时间戳算术运算的边界处理
    /// </summary>
    [Fact]
    public void ArithmeticOperations_NearBoundaries_HandleCorrectly()
    {
        // Arrange
        var nearMaxTimestamp = UnixTimeStamp.MaxUnixTimestamp - 1000;
        var unixTimeStamp = new UnixTimeStamp(nearMaxTimestamp);

        // Act & Assert
        Should.NotThrow(() => unixTimeStamp.AddSeconds(500));

        var result = unixTimeStamp.AddSeconds(500);
        result.ToTimestamp().ShouldBe(nearMaxTimestamp + 500);
    }

    #endregion

    #region 兼容性测试

    /// <summary>
    /// 测试 - NowUnixTimeStamp - 过时属性兼容性
    /// </summary>
    [Fact]
    public void NowUnixTimeStamp_ObsoleteProperty_StillWorksCorrectly()
    {
        // Arrange
        var beforeCall = UnixTimeStamp.Now();

        // Act
#pragma warning disable CS0618 // 类型或成员已过时
        var timestamp = UnixTimeStamp.NowUnixTimeStamp();
#pragma warning restore CS0618 // 类型或成员已过时

        // Assert
        var afterCall = UnixTimeStamp.Now();
        timestamp.ShouldBeGreaterThanOrEqualTo(beforeCall - 1);
        timestamp.ShouldBeLessThanOrEqualTo(afterCall + 1);
    }

    /// <summary>
    /// 测试 - UtcNowUnixTimeStamp - 过时属性兼容性
    /// </summary>
    [Fact]
    public void UtcNowUnixTimeStamp_ObsoleteProperty_StillWorksCorrectly()
    {
        // Arrange
        var beforeCall = UnixTimeStamp.UtcNow();

        // Act
#pragma warning disable CS0618 // 类型或成员已过时
        var timestamp = UnixTimeStamp.UtcNowUnixTimeStamp();
#pragma warning restore CS0618 // 类型或成员已过时

        // Assert
        var afterCall = UnixTimeStamp.UtcNow();
        timestamp.ShouldBeGreaterThanOrEqualTo(beforeCall - 1);
        timestamp.ShouldBeLessThanOrEqualTo(afterCall + 1);
    }

    /// <summary>
    /// 测试 - 过时属性的委托行为
    /// </summary>
    [Fact]
    public void ObsoleteProperties_DelegatesBehavior_WorksCorrectly()
    {
        // Act
#pragma warning disable CS0618 // 类型或成员已过时
        var oldStyleNow = UnixTimeStamp.NowUnixTimeStamp.Invoke();
        var oldStyleUtcNow = UnixTimeStamp.UtcNowUnixTimeStamp.Invoke();
#pragma warning restore CS0618 // 类型或成员已过时

        var newStyleNow = UnixTimeStamp.Now();
        var newStyleUtcNow = UnixTimeStamp.UtcNow();

        // Assert
        Math.Abs(oldStyleNow - newStyleNow).ShouldBeLessThanOrEqualTo(1);
        Math.Abs(oldStyleUtcNow - newStyleUtcNow).ShouldBeLessThanOrEqualTo(1);
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
        var unixTimestamp = 1640995200L;

        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var _ = new UnixTimeStamp(unixTimestamp);
            }
        }, TimeSpan.FromSeconds(1), $"创建{iterations}个UnixTimeStamp实例应该在1秒内完成");
    }

    /// <summary>
    /// 测试 - 转换方法性能
    /// </summary>
    [Fact]
    public void Performance_ConversionMethods_CompleteQuickly()
    {
        // Arrange
        const int iterations = 100000;
        var unixTimeStamp = new UnixTimeStamp();

        // Act & Assert
        Should.CompleteIn(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var _ = unixTimeStamp.ToDateTime();
                var __ = unixTimeStamp.ToTimestamp();
                var ___ = unixTimeStamp.ToUtcDateTime();
            }
        }, TimeSpan.FromSeconds(1), $"执行{iterations}次转换操作应该在1秒内完成");
    }

    /// <summary>
    /// 测试 - 静态方法性能
    /// </summary>
    [Fact]
    public void Performance_StaticMethods_CompleteQuickly()
    {
        // Arrange
        const int iterations = 50000;

        // Act & Assert
        Should.CompleteIn(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var _ = UnixTimeStamp.Now();
                    var __ = UnixTimeStamp.UtcNow();
                    var ___ = UnixTimeStamp.CreateNow();
                    var ____ = UnixTimeStamp.CreateUtcNow();
                }
            }, TimeSpan.FromSeconds(1), $"执行{iterations}次静态方法调用应该在1秒内完成");
    }

    #endregion

    #region 集成测试

    /// <summary>
    /// 测试 - 实际使用场景 - Web API时间戳处理
    /// </summary>
    [Fact]
    public void RealWorldScenario_WebApiTimestampProcessing_WorksCorrectly()
    {
        // Arrange - 模拟API返回的Unix时间戳
        var apiTimestamps = new long[]
        {
            1640995200L, // 2022-01-01 00:00:00 UTC
            1672531200L, // 2023-01-01 00:00:00 UTC
            1704067200L, // 2024-01-01 00:00:00 UTC
        };

        // Act - 处理时间戳
        var processedTimes = apiTimestamps
            .Select(ts => new UnixTimeStamp(ts))
            .Select(uts => new
            {
                UnixTimestamp = uts.ToTimestamp(),
                LocalTime = uts.ToDateTime(),
                UtcTime = uts.ToUtcDateTime(),
                Iso8601 = uts.ToIso8601String(),
                DaysSinceEpoch = uts.GetDaysSinceEpoch(),
                IsAfter2038 = uts.IsAfter2038Problem()
            })
            .ToList();

        // Assert
        processedTimes.Count.ShouldBe(3);

        // 验证时间递增
        for (int i = 1; i < processedTimes.Count; i++)
        {
            processedTimes[i].UnixTimestamp.ShouldBeGreaterThan(processedTimes[i - 1].UnixTimestamp);
            processedTimes[i].DaysSinceEpoch.ShouldBeGreaterThan(processedTimes[i - 1].DaysSinceEpoch);
        }

        // 验证格式
        processedTimes[0].Iso8601.ShouldBe("2022-01-01T00:00:00Z");
        processedTimes[1].Iso8601.ShouldBe("2023-01-01T00:00:00Z");
        processedTimes[2].Iso8601.ShouldBe("2024-01-01T00:00:00Z");

        // 验证2038年问题检测
        processedTimes.ShouldAllBe(p => !p.IsAfter2038);
    }

    /// <summary>
    /// 测试 - 实际使用场景 - JavaScript时间戳互操作
    /// </summary>
    [Fact]
    public void RealWorldScenario_JavaScriptInteroperability_WorksCorrectly()
    {
        // Arrange - 模拟JavaScript Date.now()返回的毫秒级时间戳
        var jsTimestamps = new long[]
        {
            1640995200000L, // 2022-01-01 00:00:00 UTC (毫秒)
            1672531200500L, // 2023-01-01 00:00:00.500 UTC (毫秒)
        };

        // Act - 处理JavaScript时间戳
        var processedFromJs = jsTimestamps
            .Select(UnixTimeStamp.FromUnixTimeMilliseconds)
            .ToList();

        // 转换回JavaScript格式
        var convertedToJs = processedFromJs
            .Select(uts => uts.ToUnixTimeMilliseconds())
            .ToList();

        // Assert
        convertedToJs[0].ShouldBe(1640995200000L);
        convertedToJs[1].ShouldBe(1672531200000L); // 精度丢失，毫秒部分被截断

        // 验证秒级精度保持
        processedFromJs[0].ToTimestamp().ShouldBe(1640995200L);
        processedFromJs[1].ToTimestamp().ShouldBe(1672531200L);
    }

    /// <summary>
    /// 测试 - 实际使用场景 - 缓存过期时间管理
    /// </summary>
    [Fact]
    public void RealWorldScenario_CacheExpirationManagement_WorksCorrectly()
    {
        // Arrange - 模拟缓存管理场景
        var currentTime = UnixTimeStamp.CreateUtcNow();
        var cacheItems = new[]
        {
            new { Key = "user:123", ExpiresAt = currentTime.AddMinutes(15) }, // 15分钟后过期
            new { Key = "session:abc", ExpiresAt = currentTime.AddHours(1) },  // 1小时后过期
            new { Key = "token:xyz", ExpiresAt = currentTime.AddDays(7) },     // 7天后过期
        };

        // Act - 检查过期状态
        var now = UnixTimeStamp.CreateUtcNow();
        var expirationInfo = cacheItems.Select(item => new
        {
            item.Key,
            ExpiresAt = item.ExpiresAt.ToTimestamp(),
            IsExpired = item.ExpiresAt.ToTimestamp() <= now.ToTimestamp(),
            RelativeExpiration = item.ExpiresAt.ToRelativeString(),
            DaysUntilExpiration = (item.ExpiresAt.ToTimestamp() - now.ToTimestamp()) / 86400
        }).ToList();

        // Assert
        expirationInfo.Count.ShouldBe(3);

        // 验证过期时间递增
        for (int i = 1; i < expirationInfo.Count; i++)
        {
            expirationInfo[i].ExpiresAt.ShouldBeGreaterThan(expirationInfo[i - 1].ExpiresAt);
        }

        // 通常情况下，新创建的缓存项不应该立即过期
        expirationInfo.ShouldAllBe(info => !info.IsExpired);

        // 验证相对过期时间包含"后"字
        expirationInfo.ShouldAllBe(info => info.RelativeExpiration.Contains("后"));
    }

    /// <summary>
    /// 测试 - 实际使用场景 - 日志时间戳序列化
    /// </summary>
    [Fact]
    public void RealWorldScenario_LogTimestampSerialization_WorksCorrectly()
    {
        // Arrange - 模拟日志条目
        var logEntries = new[]
        {
            new { Level = "INFO", Message = "Application started", Timestamp = UnixTimeStamp.CreateUtcNow() },
            new { Level = "WARN", Message = "High memory usage", Timestamp = UnixTimeStamp.CreateUtcNow().AddMinutes(5) },
            new { Level = "ERROR", Message = "Database connection failed", Timestamp = UnixTimeStamp.CreateUtcNow().AddMinutes(10) }
        };

        // Act - 序列化为不同格式
        var serializedLogs = logEntries.Select(log => new
        {
            log.Level,
            log.Message,
            UnixTimestamp = log.Timestamp.ToTimestamp(),
            Iso8601 = log.Timestamp.ToIso8601String(),
            Rfc2822 = log.Timestamp.ToRfc2822String(),
            MillisecondsTimestamp = log.Timestamp.ToUnixTimeMilliseconds(),
            DaysSinceEpoch = log.Timestamp.GetDaysSinceEpoch()
        }).ToList();

        // Assert
        serializedLogs.Count.ShouldBe(3);

        // 验证时间戳递增
        for (int i = 1; i < serializedLogs.Count; i++)
        {
            serializedLogs[i].UnixTimestamp.ShouldBeGreaterThan(serializedLogs[i - 1].UnixTimestamp);
            serializedLogs[i].MillisecondsTimestamp.ShouldBeGreaterThan(serializedLogs[i - 1].MillisecondsTimestamp);
        }

        // 验证格式化字符串
        serializedLogs.ShouldAllBe(log => log.Iso8601.EndsWith("Z"));
        serializedLogs.ShouldAllBe(log => log.Rfc2822.EndsWith("GMT"));
        serializedLogs.ShouldAllBe(log => log.MillisecondsTimestamp == log.UnixTimestamp * 1000);
    }

    #endregion

    #region 线程安全测试

    /// <summary>
    /// 测试 - 多线程环境下的静态方法调用
    /// </summary>
    [Fact]
    public void ThreadSafety_StaticMethods_WorkCorrectlyInMultiThreadedEnvironment()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 1000;
        var results = new ConcurrentBag<long>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    var timestamp = UnixTimeStamp.UtcNow();
                    results.Add(timestamp);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        results.Count.ShouldBe(threadCount * operationsPerThread);

        // 验证所有时间戳都是合理的
        var timestampArray = results.ToArray();
        timestampArray.ShouldAllBe(ts => ts > 0);

        // 验证时间戳在合理范围内（当前时间附近）
        var currentTimestamp = UnixTimeStamp.UtcNow();
        timestampArray.ShouldAllBe(ts => Math.Abs(ts - currentTimestamp) < 10); // 10秒内
    }

    /// <summary>
    /// 测试 - MaxUnixTimestamp延迟计算的线程安全性
    /// </summary>
    [Fact]
    public void ThreadSafety_MaxUnixTimestampLazyCalculation_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 20;
        var results = new ConcurrentBag<long>();
        var tasks = new List<Task>();

        // Act - 多线程同时访问MaxUnixTimestamp
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var maxTimestamp = UnixTimeStamp.MaxUnixTimestamp;
                results.Add(maxTimestamp);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        results.Count.ShouldBe(threadCount);

        // 所有结果应该相同（延迟计算只执行一次）
        var distinctResults = results.Distinct().ToArray();
        distinctResults.Length.ShouldBe(1);

        // 验证计算结果正确
        var expectedMax = (long)(DateTime.MaxValue.ToUniversalTime() - UnixTimeStamp.UnixEpoch).TotalSeconds;
        distinctResults[0].ShouldBe(expectedMax);
    }

    #endregion
}