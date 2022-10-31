using System;
using Bing.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// 时间操作测试
/// </summary>
public class TimeTest : TestBase, IDisposable
{
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Time.Reset();
    }

    /// <summary>
    /// 初始化一个<see cref="TestBase"/>类型的实例
    /// </summary>
    public TimeTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 日期字符串,"2012-01-02"
    /// </summary>
    public const string DateString1 = "2012-01-02";

    /// <summary>
    /// 日期,2012-01-02
    /// </summary>
    public static readonly DateTime Date1 = DateTime.Parse(DateString1);

    /// <summary>
    /// 日期字符串,"2012-11-12"
    /// </summary>
    public const string DateString2 = "2012-11-12";

    /// <summary>
    /// 日期时间字符串,"2012-01-02 01:02:03"
    /// </summary>
    public const string DatetimeString1 = "2012-01-02 01:02:03";

    /// <summary>
    /// 日期时间,2012-01-02 01:02:03
    /// </summary>
    public static readonly DateTime Datetime1 = DateTime.Parse(DatetimeString1);

    /// <summary>
    /// 日期时间字符串,"2012-11-12 13:04:05"
    /// </summary>
    public const string DatetimeString2 = "2012-11-12 13:04:05";

    /// <summary>
    /// 日期时间,2012-11-12 13:04:05
    /// </summary>
    public static readonly DateTime Datetime2 = DateTime.Parse(DatetimeString2);

    /// <summary>
    /// 测试设置时间
    /// </summary>
    [Fact]
    public void Test_SetTime()
    {
        Time.SetTime(Datetime1);
        Assert.Equal(Datetime1, Time.GetDateTime());
        Time.Reset();
        Assert.NotEqual(Datetime1, Time.GetDateTime());
    }

    /// <summary>
    /// 测试获取Unix时间戳
    /// </summary>
    [Fact]
    public void Test_GetUnixTimestamp()
    {
        Assert.Equal(15132, Time.GetUnixTimestamp(new DateTime(1970, 01, 01, 12, 12, 12)));
        Assert.Equal(976594332, Time.GetUnixTimestamp(new DateTime(2000, 12, 12, 12, 12, 12)));
        Assert.Equal(1392668699, Time.GetUnixTimestamp(new DateTime(2014, 02, 18, 04, 24, 59)));
    }

    /// <summary>
    /// 测试从Unix时间戳获取时间
    /// </summary>
    [Fact]
    public void Test_GetTimeFromUnixTimestamp()
    {
        Assert.Equal(new DateTime(1970, 01, 01, 12, 12, 12), Time.GetTimeFromUnixTimestamp(15132));
        Assert.Equal(new DateTime(2000, 12, 12, 12, 12, 12), Time.GetTimeFromUnixTimestamp(976594332));
        Assert.Equal(new DateTime(2014, 02, 18, 04, 24, 59), Time.GetTimeFromUnixTimestamp(1392668699));
    }
}