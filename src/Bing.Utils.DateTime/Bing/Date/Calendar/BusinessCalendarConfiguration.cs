using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Bing.Date.Calendars;
/// <summary>
/// 从业务维护的版本化 JSON 配置加载不可变年度日历。
/// </summary>
/// <remarks>加载失败不修改已有实例；业务可在加载成功后原子替换实例引用。</remarks>
public static class BusinessCalendarConfiguration
{
    /// <summary>
    /// 单份配置允许的最大字符数，避免无限制解析配置内容。
    /// </summary>
    public const int MaxJsonLength = 65536;
    /// <summary>
    /// 从 JSON 字符串加载 schemaVersion 为 1 的年度日历。
    /// </summary>
    /// <param name="json">包含 region、year、version、source 及例外日期数组的 JSON。</param>
    /// <returns>与输入配置脱离的不可变年度日历。</returns>
    /// <exception cref="ArgumentNullException">输入为空。</exception>
    /// <exception cref="ArgumentException">输入超过长度限制。</exception>
    /// <exception cref="JsonException">配置结构、版本、字段或日期规则无效。</exception>
    public static AnnualBusinessCalendar FromJson(string json)
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));
        if (json.Length > MaxJsonLength)
            throw new ArgumentException("日历配置超过最大字符数。", nameof(json));
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("日历配置必须是 JSON 对象。");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            if (!names.Add(property.Name))
                throw new JsonException($"字段重复：{property.Name}。");
            switch (property.Name)
            {
                case "schemaVersion": case "region": case "year": case "version":
                case "source": case "restDays": case "holidays": case "workingDayOverrides":
                    break;
                default:
                    throw new JsonException($"未知字段：{property.Name}。");
            }
        }
        if (GetInteger(root, "schemaVersion") != 1)
            throw new JsonException("不支持的日历配置结构版本。");
        var year = GetInteger(root, "year");
        var region = GetText(root, "region");
        var version = GetText(root, "version");
        var source = GetText(root, "source");
        var holidays = ReadDates(root, "holidays");
        var workingDays = ReadDates(root, "workingDayOverrides");
        DayOfWeek[] restDays = null;
        if (root.TryGetProperty("restDays", out var days))
        {
            ValidateArray(days, "restDays", 7);
            restDays = new DayOfWeek[days.GetArrayLength()];
            var index = 0;
            foreach (var day in days.EnumerateArray())
            {
                if (day.ValueKind != JsonValueKind.Number || !day.TryGetInt32(out var value) || value < 0 || value > 6)
                    throw new JsonException("restDays 必须是 0～6 的星期整数。");
                restDays[index++] = (DayOfWeek)value;
            }
        }
        try
        {
            return new AnnualBusinessCalendar(year, region, version, source, holidays, workingDays, restDays);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("日历配置的日期或星期规则无效。", exception);
        }
    }
    /// <summary>
    /// 从本地 UTF-8 文件限量读取并加载年度日历。
    /// </summary>
    /// <param name="path">本地配置路径；支持 UTF-8 BOM，不接受其他编码。</param>
    /// <returns>加载完成的不可变日历。</returns>
    /// <exception cref="ArgumentException">路径为空白。</exception>
    /// <exception cref="IOException">无法读取文件或文件超过大小限制。</exception>
    /// <exception cref="DecoderFallbackException">文件不是有效 UTF-8。</exception>
    /// <exception cref="JsonException">文件中的 JSON 配置无效。</exception>
    public static AnnualBusinessCalendar LoadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("日历配置路径不能为空。", nameof(path));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaxJsonLength * 4L + 3)
            throw new IOException("日历配置文件超过大小限制。");
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), false);
        // 小配置不预分配 64K 字符大对象；增长中的文件也受字符上限约束。
        var buffer = new char[1024];
        var content = new StringBuilder(1024);
        var firstChunk = true;
        int count;
        while ((count = reader.ReadBlock(buffer, 0, buffer.Length)) > 0)
        {
            var start = firstChunk && buffer[0] == '\uFEFF' ? 1 : 0;
            firstChunk = false;
            if (content.Length + count - start > MaxJsonLength)
                throw new IOException("日历配置文件超过最大字符数。");
            content.Append(buffer, start, count - start);
        }
        return FromJson(content.ToString());
    }
    /// <summary>
    /// 读取必需的整数配置字段。
    /// </summary>
    /// <param name="root">配置根节点。</param>
    /// <param name="name">字段名。</param>
    /// <returns>字段的整数值。</returns>
    private static int GetInteger(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var value))
            throw new JsonException($"字段 {name} 必须是整数。");
        return value;
    }
    /// <summary>
    /// 读取必需且非空白的元数据字段。
    /// </summary>
    /// <param name="root">配置根节点。</param>
    /// <param name="name">字段名。</param>
    /// <returns>元数据字符串。</returns>
    private static string GetText(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            throw new JsonException($"字段 {name} 必须是非空白字符串。");
        return item.GetString();
    }
    /// <summary>
    /// 检查配置数组类型和最大元素数量。
    /// </summary>
    /// <param name="item">配置节点。</param>
    /// <param name="name">字段名称。</param>
    /// <param name="maximum">允许的最大元素数。</param>
    private static void ValidateArray(JsonElement item, string name, int maximum)
    {
        if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() > maximum)
            throw new JsonException($"字段 {name} 必须是最多 {maximum} 项的数组。");
    }
    /// <summary>
    /// 读取严格 yyyy-MM-dd 日期数组；未提供时使用空数组。
    /// </summary>
    /// <param name="root">根节点。</param>
    /// <param name="name">日期字段名。</param>
    /// <returns>解析出的自然日数组。</returns>
    private static DateTime[] ReadDates(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var dates))
            return Array.Empty<DateTime>();
        ValidateArray(dates, name, 366);
        var result = new DateTime[dates.GetArrayLength()];
        var index = 0;
        foreach (var item in dates.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !DateTime.TryParseExact(item.GetString(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                throw new JsonException($"字段 {name} 的日期必须是 yyyy-MM-dd 格式。");
            result[index++] = date;
        }
        return result;
    }
}
