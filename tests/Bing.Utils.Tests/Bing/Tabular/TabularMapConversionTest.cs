using System.Globalization;
using Bing.Diagnostics;
using Bing.Operations;
using Bing.Tabular;

namespace Bing.Utils.Tests.Bing.Tabular;

/// <summary>
/// 验证表格映射内置标量转换及其诊断信息。
/// </summary>
public class TabularMapConversionTest
{
    private static readonly string[] ColumnNames =
    {
        nameof(ScalarTarget.Mode), nameof(ScalarTarget.Identifier), nameof(ScalarTarget.Date),
        nameof(ScalarTarget.Timestamp), nameof(ScalarTarget.Duration), nameof(ScalarTarget.Endpoint),
        nameof(ScalarTarget.Enabled), nameof(ScalarTarget.Marker), nameof(ScalarTarget.Count),
        nameof(ScalarTarget.Ratio), nameof(ScalarTarget.OptionalCount)
    };

    /// <summary>
    /// 将常见标量字符串转换为对应的枚举、日期、URI 和数值类型。
    /// </summary>
    [Fact]
    public void MapsBuiltInScalarValues()
    {
        var values = ValidValues();
        var result = CreateMap().Map(new TestRow(8, ColumnNames, values));

        Assert.True(result.IsSuccess);
        Assert.Equal(State.Ready, result.Value.Mode);
        Assert.Equal(Guid.Parse((string)values[1]), result.Value.Identifier);
        Assert.Equal(new DateTime(2026, 10, 27, 12, 34, 56, DateTimeKind.Utc), result.Value.Date);
        Assert.Equal(DateTimeKind.Utc, result.Value.Date.Kind);
        Assert.Equal(new DateTimeOffset(2026, 10, 27, 12, 34, 56, TimeSpan.FromHours(8)), result.Value.Timestamp);
        Assert.Equal(new TimeSpan(1, 2, 3, 4).Add(TimeSpan.FromMilliseconds(5)), result.Value.Duration);
        Assert.Equal(new Uri((string)values[5], UriKind.RelativeOrAbsolute), result.Value.Endpoint);
        Assert.True(result.Value.Enabled);
        Assert.Equal('Z', result.Value.Marker);
        Assert.Equal(int.MaxValue, result.Value.Count);
        Assert.Equal(12.5m, result.Value.Ratio);
        Assert.Equal(7, result.Value.OptionalCount);
    }

    /// <summary>
    /// 枚举字段支持名称和底层数值两种原始值。
    /// </summary>
    [Theory]
    [InlineData("ready", 1)]
    [InlineData(2, 2)]
    public void ConvertsEnumNamesAndNumericValues(object input, int expected)
    {
        var map = TabularMap<EnumTarget>.Create(config => config.Field(target => target.Mode).From(nameof(EnumTarget.Mode)));

        var result = map.Map(new TestRow(1, new[] { nameof(EnumTarget.Mode) }, new[] { input }));

        Assert.True(result.IsSuccess);
        Assert.Equal((State)expected, result.Value.Mode);
    }

    /// <summary>
    /// 转换失败返回带源列、目标成员和底层异常类型的稳定诊断。
    /// </summary>
    [Theory]
    [InlineData(nameof(ScalarTarget.Mode), "NotAState", typeof(ArgumentException))]
    [InlineData(nameof(ScalarTarget.Identifier), "not-a-guid", typeof(FormatException))]
    [InlineData(nameof(ScalarTarget.Date), "not-a-date", typeof(FormatException))]
    [InlineData(nameof(ScalarTarget.Timestamp), "not-an-offset", typeof(FormatException))]
    [InlineData(nameof(ScalarTarget.Duration), "not-a-duration", typeof(FormatException))]
    [InlineData(nameof(ScalarTarget.Endpoint), "http://[invalid", typeof(UriFormatException))]
    [InlineData(nameof(ScalarTarget.Enabled), "sometimes", typeof(FormatException))]
    [InlineData(nameof(ScalarTarget.Marker), "XY", typeof(FormatException))]
    [InlineData(nameof(ScalarTarget.Count), "2147483648", typeof(OverflowException))]
    public void ConversionFailuresReturnLocatedIssues(string columnName, string input, Type exceptionType)
    {
        var values = ValidValues();
        var ordinal = Array.IndexOf(ColumnNames, columnName);
        Assert.True(ordinal >= 0);
        values[ordinal] = input;

        var result = CreateMap().Map(new TestRow(17, ColumnNames, values));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("mapping.conversion", issue.Code);
        Assert.Equal(DataIssueSeverity.Error, issue.Severity);
        Assert.Equal(OperationStage.Transform, issue.Stage);
        Assert.Equal(columnName, issue.SourceColumnName);
        Assert.Equal(ordinal, issue.SourceOrdinal);
        Assert.Equal(columnName, issue.TargetMember);
        Assert.Equal(exceptionType.FullName, issue.ExceptionType);
        var location = Assert.IsType<TabularLocation>(issue.Location);
        Assert.Equal(17, location.RowNumber);
        Assert.Equal(ordinal, location.Ordinal);
        Assert.Equal(columnName, location.ColumnName);
    }

    /// <summary>
    /// 默认文化选项控制数字解析且不依赖当前线程文化。
    /// </summary>
    [Theory]
    [InlineData("de-DE", "1,25")]
    [InlineData("en-US", "1.25")]
    public void UsesConfiguredCultureForNumericValues(string cultureName, string input)
    {
        var values = ValidValues();
        values[Array.IndexOf(ColumnNames, nameof(ScalarTarget.Ratio))] = input;

        var result = CreateMap(CultureInfo.GetCultureInfo(cultureName))
            .Map(new TestRow(3, ColumnNames, values));

        Assert.True(result.IsSuccess);
        Assert.Equal(1.25m, result.Value.Ratio);
    }

    /// <summary>
    private static TabularMap<ScalarTarget> CreateMap(CultureInfo culture = null)
        => TabularMap<ScalarTarget>.Create(config =>
        {
            config.Field(target => target.Mode).From(nameof(ScalarTarget.Mode));
            config.Field(target => target.Identifier).From(nameof(ScalarTarget.Identifier));
            config.Field(target => target.Date).From(nameof(ScalarTarget.Date));
            config.Field(target => target.Timestamp).From(nameof(ScalarTarget.Timestamp));
            config.Field(target => target.Duration).From(nameof(ScalarTarget.Duration));
            config.Field(target => target.Endpoint).From(nameof(ScalarTarget.Endpoint));
            config.Field(target => target.Enabled).From(nameof(ScalarTarget.Enabled));
            config.Field(target => target.Marker).From(nameof(ScalarTarget.Marker));
            config.Field(target => target.Count).From(nameof(ScalarTarget.Count));
            config.Field(target => target.Ratio).From(nameof(ScalarTarget.Ratio));
            config.Field(target => target.OptionalCount).From(nameof(ScalarTarget.OptionalCount));
        }, new TabularMapOptions { Culture = culture ?? CultureInfo.InvariantCulture });

    private static object[] ValidValues() => new object[]
    {
        "Ready",
        "01234567-89ab-cdef-0123-456789abcdef",
        "2026-10-27T12:34:56.0000000Z",
        "2026-10-27T12:34:56.0000000+08:00",
        "1.02:03:04.005",
        "https://example.test/items/42",
        "true",
        "Z",
        "2147483647",
        "12.5",
        "7"
    };

    private sealed class TestRow : ITabularRow
    {
        private readonly string[] _names;
        private readonly object[] _values;

        internal TestRow(long rowNumber, string[] names, object[] values)
        {
            RowNumber = rowNumber;
            _names = names;
            _values = values;
            Location = new TabularLocation(rowNumber);
        }

        public long RowNumber { get; }
        public int Count => _values.Length;
        public DataLocation Location { get; }
        public string GetName(int ordinal) => _names[ordinal];
        public object GetValue(int ordinal) => _values[ordinal];
    }

    private enum State
    {
        Ready = 1,
        Stopped = 2
    }

    private sealed class ScalarTarget
    {
        public State Mode { get; set; }
        public Guid Identifier { get; set; }
        public DateTime Date { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public Uri Endpoint { get; set; }
        public bool Enabled { get; set; }
        public char Marker { get; set; }
        public int Count { get; set; }
        public decimal Ratio { get; set; }
        public int? OptionalCount { get; set; }
    }

    private sealed class EnumTarget
    {
        public State Mode { get; set; }
    }
}
