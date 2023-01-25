﻿using Bing.Helpers;
using Bing.Utils.Timing;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Timing;

/// <summary>
/// 测试时间操作辅助类
/// </summary>
public class DateTimeHelperTest : TestBase
{
    /// <summary>
    /// 测试初始化
    /// </summary>
    public DateTimeHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(DayOfWeek.Thursday, "2019/8/22 0:00:00")]
    [InlineData(DayOfWeek.Saturday, "2019/8/31 0:00:00")]
    [InlineData(DayOfWeek.Wednesday, "2020/1/22 0:00:00")]
    [InlineData(DayOfWeek.Friday, "2019/3/1 0:00:00")]
    [InlineData(DayOfWeek.Thursday, "2018/2/1 0:00:00")]
    public void Test_DateTimeHelper_GetWeekDay(DayOfWeek dw, string dateStr)
    {
        var res = DateTimeHelper.GetWeekDay(dateStr);
        Assert.Equal(dw, res);
    }

    [Fact]
    public void Test_GetDays()
    {
        var beginTime = Conv.ToDate("2020-10-01 00:00:00"); 
        var endTime = Conv.ToDate("2020-11-01 00:00:00");
        //var days = DateTimeHelper.GetDays(beginTime, endTime);
        //Output.WriteLine($"天数：{days}");
        //Output.WriteLine($"天数：{beginTime.AddDays(days):yyyy-MM-dd HH:mm:ss}");
    }
}