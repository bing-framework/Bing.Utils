using Bing.Date.DateUtils;

namespace Bing.Date;

/// <summary>
/// 星座帮助类
/// </summary>
[Trait("DateTimeUT", "Constellation")]
public class ConstellationHelperTest
{
    /// <summary>
    /// 测试 - 获取星座名称 - 返回正确的星座名称
    /// </summary>
    [Theory]
    [InlineData("2023-03-21", "白羊座")]
    [InlineData("2023-04-19", "白羊座")]
    [InlineData("2023-04-20", "金牛座")]
    [InlineData("2023-05-20", "金牛座")]
    [InlineData("2023-05-21", "双子座")]
    [InlineData("2023-06-20", "双子座")]
    [InlineData("2023-06-21", "巨蟹座")]
    [InlineData("2023-07-22", "巨蟹座")]
    [InlineData("2023-07-23", "狮子座")]
    [InlineData("2023-08-22", "狮子座")]
    [InlineData("2023-08-23", "处女座")]
    [InlineData("2023-09-22", "处女座")]
    [InlineData("2023-09-23", "天秤座")]
    [InlineData("2023-10-22", "天秤座")]
    [InlineData("2023-10-23", "天蝎座")]
    [InlineData("2023-11-21", "天蝎座")]
    [InlineData("2023-11-22", "射手座")]
    [InlineData("2023-12-21", "射手座")]
    [InlineData("2023-12-22", "摩羯座")]
    [InlineData("2023-01-19", "摩羯座")]
    [InlineData("2023-01-20", "水瓶座")]
    [InlineData("2023-02-18", "水瓶座")]
    [InlineData("2023-02-19", "双鱼座")]
    [InlineData("2023-03-20", "双鱼座")]
    public void Test_Get_ReturnsCorrectConstellationName(string date, string expectedConstellation)
    {
        // Arrange
        var dt = DateTime.Parse(date);

        // Act
        var constellation = ConstellationHelper.Get(dt);

        // Assert
        Assert.Equal(expectedConstellation, constellation);
    }

    /// <summary>
    /// 测试 - 获取指定月份和日期的星座名称 - 返回正确的星座名称
    /// </summary>
    [Theory]
    [InlineData(3, 21, "白羊座")]
    [InlineData(4, 19, "白羊座")]
    [InlineData(4, 20, "金牛座")]
    [InlineData(5, 20, "金牛座")]
    [InlineData(5, 21, "双子座")]
    [InlineData(6, 20, "双子座")]
    [InlineData(6, 21, "巨蟹座")]
    [InlineData(7, 22, "巨蟹座")]
    [InlineData(7, 23, "狮子座")]
    [InlineData(8, 22, "狮子座")]
    [InlineData(8, 23, "处女座")]
    [InlineData(9, 22, "处女座")]
    [InlineData(9, 23, "天秤座")]
    [InlineData(10, 22, "天秤座")]
    [InlineData(10, 23, "天蝎座")]
    [InlineData(11, 21, "天蝎座")]
    [InlineData(11, 22, "射手座")]
    [InlineData(12, 21, "射手座")]
    [InlineData(12, 22, "摩羯座")]
    [InlineData(1, 19, "摩羯座")]
    [InlineData(1, 20, "水瓶座")]
    [InlineData(2, 18, "水瓶座")]
    [InlineData(2, 19, "双鱼座")]
    [InlineData(3, 20, "双鱼座")]
    public void Test_GetByMonthAndDay_ReturnsCorrectConstellationName(int month, int day, string expectedConstellation)
    {
        // Act
        var constellation = ConstellationHelper.Get(month, day);

        // Assert
        Assert.Equal(expectedConstellation, constellation);
    }
}