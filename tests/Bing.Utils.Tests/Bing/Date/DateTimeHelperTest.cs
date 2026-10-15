using Bing.Helpers;
namespace Bing.Date;
/// <summary>
/// 测试时间操作辅助类
/// </summary>
public class DateTimeHelperTest : TestBase
{
    /// <summary>
    /// 初始化一个 <see cref="DateTimeHelperTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">测试输出辅助器。</param>
    public DateTimeHelperTest(ITestOutputHelper output) : base(output)
    {
    }
    /// <summary>
    /// 验证日期文本解析出的星期值。
    /// </summary>
    /// <param name="dw">预期星期。</param>
    /// <param name="dateStr">日期文本。</param>
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
    /// <summary>
    /// 准备日期间隔示例的起止时间。
    /// </summary>
    /// <remarks>当前用例仅构造日期，未执行间隔计算或结果断言。</remarks>
    [Fact]
    public void Test_GetDays()
    {
        var beginTime = Conv.ToDate("2020-10-01 00:00:00");
        var endTime = Conv.ToDate("2020-11-01 00:00:00");
        //var days = DateTimeHelper.GetDays(beginTime, endTime);
        //Output.WriteLine($"天数：{days}");
        //Output.WriteLine($"天数：{beginTime.AddDays(days):yyyy-MM-dd HH:mm:ss}");
    }
    /// <summary>
    /// 输出当前毫秒时间戳及其转换后的日期时间。
    /// </summary>
    [Fact]
    public void Test_Current_1()
    {
        var result = DateTimeHelper.Current();
        Output.WriteLine(result.ToString());
        Output.WriteLine(DateTimeHelper.ConvertUnixTimestampToDateTime(result).ToMillisecondString());
    }
    /// <summary>
    /// 输出当前秒时间戳及其转换后的日期时间。
    /// </summary>
    [Fact]
    public void Test_CurrentSeconds_1()
    {
        var result = DateTimeHelper.CurrentSeconds();
        Output.WriteLine(result.ToString());
        Output.WriteLine(DateTimeHelper.ConvertUnixTimestampToDateTime(result, TimestampDigit.Second).ToDateTimeString());
    }
}
