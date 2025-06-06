﻿using Bing.Helpers;

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
    /// 相等断言
    /// </summary>
    /// <param name="expected">预期日期字符串</param>
    /// <param name="actual">实际日期</param>
    private void AssertEqual(string expected, DateTime actual)
    {
        Assert.Equal(expected, actual.ToString(_dateFormat));
    }

    /// <summary>
    /// 不相等断言
    /// </summary>
    /// <param name="expected">预期日期字符串</param>
    /// <param name="actual">实际日期</param>
    private void AssertNotEqual(string expected, DateTime actual)
    {
        Assert.NotEqual(expected, actual.ToString(_dateFormat));
    }

    #endregion

    /// <summary>
    /// 测试 - 设置时间
    /// </summary>
    [Fact]
    public void Test_SetTime()
    {
        Time.SetTime(_testDate);
        AssertEqual(_testDateString, Time.Now);
        Time.Reset();
        AssertNotEqual(_testDateString, Time.Now);
    }

    /// <summary>
    /// 测试 - 设置Utc日期
    /// </summary>
    [Fact]
    public void Test_UseUtc()
    {
        Time.UseUtc();
        Assert.Equal(DateTime.UtcNow.ToString(_dateFormat), Time.Now.ToString(_dateFormat));
        Time.UseUtc(false);
        Assert.Equal(DateTime.Now.ToString(_dateFormat), Time.Now.ToString(_dateFormat));
    }

    /// <summary>
    /// 测试 - 本地日期转换为本地日期
    /// </summary>
    [Fact]
    public void Test_Normalize_1()
    {
        // 使用本地日期
        Time.UseUtc(false);

        // 转换本地日期为本地日期
        var result = Time.Normalize(_localDate);

        // 验证
        AssertEqual(_testDateString, result);
        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    /// <summary>
    /// 测试 - Utc日期转换为本地日期
    /// </summary>
    [Fact]
    public void Test_Normalize_2()
    {
        // 使用本地日期
        Time.UseUtc(false);

        // 转换Utc日期为本地日期
        var result = Time.Normalize(_utcDate);

        // 验证
        AssertEqual(_testDateString, result);
        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    /// <summary>
    /// 测试 - 本地日期转换为Utc日期
    /// </summary>
    [Fact]
    public void Test_Normalize_3()
    {
        // 使用Utc日期
        Time.UseUtc();

        // 转换Utc日期为本地日期
        var result = Time.Normalize(_localDate2);

        // 验证
        AssertEqual(_testDateString, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// 测试 - Utc日期转换为Utc日期
    /// </summary>
    [Fact]
    public void Test_Normalize_4()
    {
        // 使用Utc日期
        Time.UseUtc();

        // 转换Utc日期为本地日期
        var result = Time.Normalize(_utcDate2);

        // 验证
        AssertEqual(_testDateString, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// 测试 - 未指定日期转换为本地日期
    /// </summary>
    [Fact]
    public void Test_Normalize_5()
    {
        // 使用本地日期
        Time.UseUtc(false);

        // 转换未指定日期为本地日期
        var result = Time.Normalize(_unspecifiedDate);

        // 验证
        AssertEqual(_testDateString, result);
        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    /// <summary>
    /// 测试 - 未指定日期转换为Utc日期
    /// </summary>
    [Fact]
    public void Test_Normalize_6()
    {
        // 使用本地日期
        Time.UseUtc();

        // 转换未指定日期为本地日期
        var result = Time.Normalize(_unspecifiedDate);

        // 验证
        Assert.Equal(_utcDate, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// 测试 - 获取Unix时间戳
    /// </summary>
    [Fact]
    public void Test_GetUnixTimestamp()
    {
        Assert.Equal(15132, Time.GetUnixTimestamp(new DateTime(1970, 01, 01, 12, 12, 12)));
        Assert.Equal(976594332, Time.GetUnixTimestamp(new DateTime(2000, 12, 12, 12, 12, 12)));
        Assert.Equal(1392668699, Time.GetUnixTimestamp(new DateTime(2014, 02, 18, 04, 24, 59)));
    }

    /// <summary>
    /// 测试 - 从Unix时间戳获取时间
    /// </summary>
    [Fact]
    public void Test_GetTimeFromUnixTimestamp()
    {
        Assert.Equal(new DateTime(1970, 01, 01, 12, 12, 12), Time.GetTimeFromUnixTimestamp(15132));
        Assert.Equal(new DateTime(2000, 12, 12, 12, 12, 12), Time.GetTimeFromUnixTimestamp(976594332));
        Assert.Equal(new DateTime(2014, 02, 18, 04, 24, 59), Time.GetTimeFromUnixTimestamp(1392668699));
    }

}