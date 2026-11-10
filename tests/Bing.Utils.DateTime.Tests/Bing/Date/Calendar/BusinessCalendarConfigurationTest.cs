using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Bing.Date.Calendars;
/// <summary>
/// 验证版本化日历配置的结构、资源限制与 UTF-8 加载。
/// </summary>
public class BusinessCalendarConfigurationTest
{
    /// <summary>
    /// 由业务定义的演示日历，不代表法定节假日。
    /// </summary>
    private const string Example = """
        {"schemaVersion":1,"region":"demo-region","year":2024,"version":"v1","source":"业务配置",
         "holidays":["2024-01-01"],"workingDayOverrides":["2024-01-06"]}
        """;
    /// <summary>
    /// 验证配置元数据和实际补班运算，后续版本不修改原实例。
    /// </summary>
    [Fact]
    public void FromJson_VersionedSnapshot_IsIndependentOfReload()
    {
        var calendar = BusinessCalendarConfiguration.FromJson(Example);
        calendar.Region.ShouldBe("demo-region");
        calendar.Year.ShouldBe(2024);
        calendar.Version.ShouldBe("v1");
        calendar.Source.ShouldBe("业务配置");
        calendar.IsBusinessDay(new DateTime(2024, 1, 1)).ShouldBeFalse();
        calendar.IsBusinessDay(new DateTime(2024, 1, 6)).ShouldBeTrue();
        calendar.CountBusinessDays(new DateTime(2024, 1, 1), new DateTime(2024, 1, 8)).ShouldBe(5);
        var replacement = BusinessCalendarConfiguration.FromJson(Example.Replace("v1", "v2").Replace("2024-01-06", "2024-01-07"));
        replacement.Version.ShouldBe("v2");
        replacement.IsBusinessDay(new DateTime(2024, 1, 6)).ShouldBeFalse();
        calendar.IsBusinessDay(new DateTime(2024, 1, 6)).ShouldBeTrue();
        Should.Throw<JsonException>(() => BusinessCalendarConfiguration.FromJson("{}"));
        calendar.Version.ShouldBe("v1");
    }
    /// <summary>
    /// 验证结构、未知或重复字段、日期格式和年度边界均严格拒绝。
    /// </summary>
    /// <param name="oldText">合法配置片段。</param>
    /// <param name="newText">应被拒绝的配置片段。</param>
    [Theory]
    [InlineData("\"schemaVersion\":1", "\"schemaVersion\":2")]
    [InlineData("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1")]
    [InlineData("\"region\":\"demo-region\"", "\"region\":\" \"")]
    [InlineData("\"year\":2024", "\"year\":\"2024\"")]
    [InlineData("\"year\":2024", "\"year\":0")]
    [InlineData("\"year\":2024", "\"Year\":2024")]
    [InlineData("2024-01-01", "2025-01-01")]
    [InlineData("2024-01-01", "2024-1-1")]
    [InlineData("2024-01-01", "2024-02-30")]
    [InlineData("2024-01-01", "2024-01-01T00:00:00Z")]
    [InlineData("\"schemaVersion\":1", "\"schemaVersion\":1,\"restDays\":[7]")]
    [InlineData("\"schemaVersion\":1", "\"schemaVersion\":1,\"restDays\":[0,1,2,3,4,5,6]")]
    [InlineData("\"schemaVersion\":1", "\"schemaVersion\":1,\"holidys\":[]")]
    [InlineData("[\"2024-01-01\"]", "null")]
    public void FromJson_InvalidConfiguration_Throws(string oldText, string newText) =>
        Should.Throw<JsonException>(() => BusinessCalendarConfiguration.FromJson(Example.Replace(oldText, newText)));
    /// <summary>
    /// 验证缺省数组与空星期数组的明确行为。
    /// </summary>
    [Fact]
    public void FromJson_NoRestDays_CanConfigureEveryDayAsBusinessDay()
    {
        var json = """{"schemaVersion":1,"region":"demo","year":2024,"version":"v1","source":"test","restDays":[]} """;
        BusinessCalendarConfiguration.FromJson(json).TotalBusinessDays.ShouldBe(366);
        Should.Throw<JsonException>(() => BusinessCalendarConfiguration.FromJson("[]"));
        Should.Throw<ArgumentNullException>(() => BusinessCalendarConfiguration.FromJson(null));
        Should.Throw<ArgumentException>(() => BusinessCalendarConfiguration.FromJson(new string(' ', BusinessCalendarConfiguration.MaxJsonLength + 1)));
        var dates = string.Join(",", Enumerable.Repeat("\"2024-01-01\"", 367));
        Should.Throw<JsonException>(() => BusinessCalendarConfiguration.FromJson(Example.Replace("[\"2024-01-01\"]", "[" + dates + "]")));
    }
    /// <summary>
    /// 验证 UTF-8 文件和可选 BOM，拒绝错误编码及超长文件。
    /// </summary>
    /// <param name="bom">是否写入 BOM。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadFile_UsesBoundedUtf8Reading(bool bom)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, Example, new UTF8Encoding(bom));
            BusinessCalendarConfiguration.LoadFile(path).Source.ShouldBe("业务配置");
            // 模拟 UTF-16 数据，不能自动切换到其他编码读取。
            File.WriteAllBytes(path, new byte[] { 0xFF, 0xFE, 0x7B, 0x00 });
            Should.Throw<DecoderFallbackException>(() => BusinessCalendarConfiguration.LoadFile(path));
            File.WriteAllText(path, new string(' ', BusinessCalendarConfiguration.MaxJsonLength + 1), new UTF8Encoding(bom));
            Should.Throw<IOException>(() => BusinessCalendarConfiguration.LoadFile(path));
            File.WriteAllBytes(path, new byte[BusinessCalendarConfiguration.MaxJsonLength * 4 + 4]);
            Should.Throw<IOException>(() => BusinessCalendarConfiguration.LoadFile(path));
        }
        finally { File.Delete(path); }
        Should.Throw<ArgumentException>(() => BusinessCalendarConfiguration.LoadFile(" "));
    }
}
