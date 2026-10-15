using Bing.Date.Chinese;
namespace Bing.Date;
/// <summary>
/// 验证生肖名称的年份映射及简繁体输出。
/// </summary>
[Trait("DateTimeUT", "ChineseAnimal")]
public class ChineseAnimalHelperTest
{
    /// <summary>
    /// 验证指定年份与简繁体选项返回对应生肖。
    /// </summary>
    /// <param name="year">用于查询生肖的年份。</param>
    /// <param name="traditionalChineseCharacters">是否使用繁体中文。</param>
    /// <param name="expectedAnimal">预期生肖名称。</param>
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
    /// 验证生肖查询拒绝早于一九〇〇年的年份。
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
