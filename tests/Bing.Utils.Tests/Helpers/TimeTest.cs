using Bing.Date;
using Bing.Helpers;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Helpers;

/// <summary>
/// 时间操作测试
/// </summary>
public class TimeTest : TestBase, IDisposable
{
    #region 测试初始化

    /// <summary>
    /// 日期格式
    /// </summary>
    private static readonly string _dateFormat = "yyyy-MM-dd HH:mm:ss";

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
    private const string TestDateString = "2012-01-02 01:02:03";

    /// <summary>
    /// 日期时间,2012-01-02 01:02:03
    /// </summary>
    public static readonly DateTime TestDate = DateTime.Parse(TestDateString);

    /// <summary>
    /// 日期时间字符串,"2012-11-12 13:04:05"
    /// </summary>
    public const string DatetimeString2 = "2012-11-12 13:04:05";

    /// <summary>
    /// 日期时间,2012-11-12 13:04:05
    /// </summary>
    public static readonly DateTime Datetime2 = DateTime.Parse(DatetimeString2);

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
    /// 相等断言
    /// </summary>
    /// <param name="expected">预期日期字符串</param>
    /// <param name="actual">实际日期</param>
    private void AssertEqual( string expected,DateTime actual ) {
        Assert.Equal( expected, actual.ToString( _dateFormat ) );
    }

    /// <summary>
    /// 不相等断言
    /// </summary>
    /// <param name="expected">预期日期字符串</param>
    /// <param name="actual">实际日期</param>
    private void AssertNotEqual( string expected, DateTime actual ) {
        Assert.NotEqual( expected, actual.ToString( _dateFormat ) );
    }

    #endregion

    /// <summary>
    /// 测试设置时间
    /// </summary>
    [Fact]
    public void Test_SetTime()
    {
        Time.SetTime(TestDate);
        AssertEqual(TestDateString, Time.Now);
        Time.Reset();
        AssertNotEqual(TestDateString, Time.Now);
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