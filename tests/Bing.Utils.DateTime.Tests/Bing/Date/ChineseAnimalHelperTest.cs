using Bing.Date.Chinese;

namespace Bing.Date;

/// <summary>
/// 中国生肖帮助类测试
/// </summary>
[Trait("DateTimeUT", "ChineseAnimal")]
public class ChineseAnimalHelperTest
{
    /// <summary>
    /// 测试 - 获取生肖
    /// </summary>
    [Theory]
    [InlineData(1900, false, "鼠")]
    [InlineData(2023, false, "兔")]
    [InlineData(2024, false, "龙")]
    [InlineData(1900, true, "鼠")]
    [InlineData(2023, true, "兔")]
    [InlineData(2024, true, "龍")]
    public void Test_Get_ReturnsCorrectAnimal(int year, bool traditionalChineseCharacters, string expectedAnimal)
    {
        // Act
        var animal = ChineseAnimalHelper.Get(year, traditionalChineseCharacters);

        // Assert
        Assert.Equal(expectedAnimal, animal);
    }

    /// <summary>
    /// 测试 - 获取生肖 - 验证年份小于1900时抛出异常
    /// </summary>
    [Fact]
    public void Test_Get_ThrowsArgumentOutOfRangeException_ForYearLessThan1900()
    {
        // Arrange
        var year = 1899;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ChineseAnimalHelper.Get(year));
    }
}