using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Threading;
using Bing.Serialization.Csv;
using Bing.Diagnostics;
using Bing.Tabular;

namespace Bing.Utils.Tests.Bing.Tabular;

/// <summary>
/// 验证表格映射的中文别名、预检、转换和错误定位。
/// </summary>
public class TabularMapTest
{
    /// <summary>
    /// 中文字段别名可以映射到目标成员，并保留源行位置。
    /// </summary>
    [Fact]
    public void MapsChineseAliasesAndKeepsLocation()
    {
        var map = TabularMap<User>.Create(m =>
        {
            m.Field(x => x.Name).From("姓名", "用户名").RequiredColumn().RequiredValue();
            m.Field(x => x.Age).From("年龄").RequiredColumn();
        });

        var result = map.Map(new TestRow(7, new[] { "姓名", "年龄" }, new object[] { "小明", "18" }));

        Assert.True(result.IsSuccess);
        Assert.Equal("小明", result.Value.Name);
        Assert.Equal(18, result.Value.Age);
        Assert.Equal(7, ((TabularLocation)result.Location).RowNumber);
    }

    /// <summary>
    /// 缺列、空值和转换失败可以在同一行聚合返回，并且不返回半成品对象。
    /// </summary>
    [Fact]
    public void AggregatesRowIssuesWithoutPartialObject()
    {
        var map = TabularMap<User>.Create(m =>
        {
            m.Field(x => x.Name).From("姓名").RequiredColumn().RequiredValue();
            m.Field(x => x.Age).From("年龄").RequiredColumn();
            m.Field(x => x.Score).From("分数");
        });

        var result = map.Map(new TestRow(3, new[] { "姓名", "分数" }, new object[] { "", "bad" }));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Contains(result.Issues, issue => issue.Code == "mapping.required_value");
        Assert.Contains(result.Issues, issue => issue.Code == "mapping.conversion");
        Assert.Contains(result.Issues, issue => issue.Code == "mapping.missing_column");
        Assert.All(result.Issues, issue => Assert.Equal(3, ((TabularLocation)issue.Location).RowNumber));
    }

    /// <summary>
    /// 按序号要求的列缺失时，行结果及预检保留期望序号，并使用数据源自身的坐标约定。
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(int.MaxValue)]
    public void MissingRequiredOrdinalKeepsExpectedSourceLocation(int ordinal)
    {
        var map = TabularMap<User>.Create(m => m.Field(x => x.Age).FromOrdinal(ordinal).RequiredColumn());
        var tableResult = map.Map(new TestRow(7, Array.Empty<string>(), Array.Empty<object>()));
        Assert.False(tableResult.IsSuccess);
        Assert.Null(tableResult.Value);
        var tableIssue = Assert.Single(tableResult.Issues);
        Assert.Equal("mapping.missing_column", tableIssue.Code);
        Assert.Equal(ordinal, tableIssue.SourceOrdinal);
        Assert.Null(tableIssue.SourceColumnName);
        Assert.Equal(nameof(User.Age), tableIssue.TargetMember);
        var tableLocation = Assert.IsType<TabularLocation>(tableIssue.Location);
        Assert.Equal(7, tableLocation.RowNumber);
        Assert.Equal(ordinal, tableLocation.Ordinal);

        var csvHeaders = new[] { nameof(User.Name) };
        var csvRow = new CsvRow(new[] { "小明" }, csvHeaders, 9);
        var csvResult = map.Map(csvRow);
        Assert.False(csvResult.IsSuccess);
        var csvIssue = Assert.Single(csvResult.Issues);
        Assert.Equal(ordinal, csvIssue.SourceOrdinal);
        var csvLocation = Assert.IsType<CsvLocation>(csvIssue.Location);
        Assert.Equal(9, csvLocation.RecordNumber);
        Assert.Equal(ordinal == int.MaxValue ? 0 : ordinal + 1, csvLocation.ColumnNumber);

        var preflightIssue = Assert.Single(map.ValidateColumns(csvHeaders, csvRow.Location));
        Assert.Equal(csvIssue.Code, preflightIssue.Code);
        Assert.Equal(csvIssue.SourceOrdinal, preflightIssue.SourceOrdinal);
        Assert.Equal(csvLocation.ColumnNumber, Assert.IsType<CsvLocation>(preflightIssue.Location).ColumnNumber);
    }

    /// <summary>
    /// 映射别名冲突必须在处理数据前抛出稳定编码的配置异常。
    /// </summary>
    [Fact]
    public void RejectsDuplicateAliasesBeforeRows()
    {
        var exception = Assert.Throws<TabularMappingException>(() => TabularMap<User>.Create(m =>
        {
            m.Field(x => x.Name).From("值");
            m.Field(x => x.Age).From("值");
        }));

        Assert.Equal("mapping.duplicate_mapping", exception.Code);
    }

    /// <summary>
    /// 未知列可以按选项作为错误返回，并保留零基源序号。
    /// </summary>
    [Fact]
    public void ReportsUnknownColumnsWhenConfigured()
    {
        var map = TabularMap<User>.Create(m => m.Field(x => x.Name).From("姓名"), new TabularMapOptions
        {
            UnknownColumns = UnknownColumnBehavior.Error
        });

        var result = map.Map(new TestRow(1, new[] { "姓名", "额外" }, new object[] { "张三", "x" }));

        Assert.False(result.IsSuccess);
        var issue = Assert.Single(result.Issues, item => item.Code == "mapping.unknown_column");
        Assert.Equal("额外", issue.SourceColumnName);
        Assert.Equal(1, issue.SourceOrdinal);
    }

    /// <summary>
    /// 支持 ordinal、文化信息和自定义转换器，并可复用同一映射计划。
    /// </summary>
    [Fact]
    public void SupportsOrdinalCultureAndCustomConversion()
    {
        var map = TabularMap<LocalizedUser>.Create(m =>
        {
            m.Field(x => x.Amount).FromOrdinal(1);
            m.Field(x => x.Code).From("代码").ConvertUsing((value, _) => Convert.ToString(value)?.Trim().ToUpperInvariant());
        }, new TabularMapOptions { Culture = System.Globalization.CultureInfo.GetCultureInfo("de-DE") });

        var first = map.Map(new TestRow(1, new[] { "编号", "金额", "代码" }, new object[] { "1", "1,25", " ab " }));
        var second = map.Map(new TestRow(2, new[] { "编号", "金额", "代码" }, new object[] { "2", "2,50", " cd " }));

        Assert.True(first.IsSuccess);
        Assert.Equal(1.25m, first.Value.Amount);
        Assert.Equal("AB", first.Value.Code);
        Assert.Equal(2.5m, second.Value.Amount);
    }

    /// <summary>
    /// DataAnnotations 校验错误包含目标成员信息。
    /// </summary>
    [Fact]
    public void AppliesDataAnnotationsValidation()
    {
        var map = TabularMap<ValidatedUser>.Create(m => m.Field(x => x.Name).From("姓名"));
        var result = map.Map(new TestRow(2, new[] { "姓名" }, new object[] { "" }));

        Assert.False(result.IsSuccess);
        var issue = Assert.Single(result.Issues, item => item.Code == "mapping.validation");
        Assert.Equal(nameof(ValidatedUser.Name), issue.TargetMember);
    }

    [Fact]
    public async Task SameMapWorksWithCsvAndDatabaseSnapshots()
    {
        var map = TabularMap<User>.Create(m =>
        {
            m.Field(x => x.Name).From("姓名", "用户名").RequiredColumn();
            m.Field(x => x.Age).From("年龄").RequiredColumn();
        });
        using var csv = new MemoryStream(Encoding.UTF8.GetBytes("姓名,年龄\n小明,18\n小红,bad\n"));
        var rows = new List<RowResult<User>>();
        await foreach (var row in CsvStream.ReadAsync(csv)) rows.Add(map.Map(row));
        Assert.Equal(18, rows[0].Value.Age);
        var issue = Assert.Single(rows[1].Issues);
        var csvLocation = Assert.IsType<CsvLocation>(issue.Location);
        Assert.Equal(3, csvLocation.RecordNumber);
        Assert.Equal(2, csvLocation.ColumnNumber);
        Assert.Equal(1, issue.SourceOrdinal);
        Assert.Equal("年龄", csvLocation.ColumnName);

        using var table = new DataTable();
        table.Columns.Add("年龄", typeof(int));
        table.Columns.Add("用户名", typeof(string));
        table.Rows.Add(21, "小李");
        using var reader = table.CreateDataReader();
        Assert.True(await reader.ReadAsync());
        var snapshot = new DbDataReaderRow(reader, 5);
        Assert.False(await reader.ReadAsync());
        var mapped = map.Map(snapshot);
        Assert.Equal("小李", mapped.Value.Name);
        Assert.Equal(21, mapped.Value.Age);
        Assert.Equal(5, Assert.IsType<TabularLocation>(mapped.Location).RowNumber);
        Assert.False(reader.IsClosed);
    }

    [Fact]
    public void MissingOptionalColumnsPreserveInitializersAndDoNotCallSetters()
    {
        var map = TabularMap<Defaults>.Create(m =>
        {
            m.Field(x => x.Count).From("数量");
            m.Field(x => x.Name).From("姓名");
        });
        var result = map.Map(new TestRow(1, Array.Empty<string>(), Array.Empty<object>()));
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.Count);
        Assert.Equal("默认", result.Value.Name);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CasePolicyControlsNameBinding(bool ignoreCase, bool success)
    {
        var map = TabularMap<User>.Create(m => m.Field(x => x.Name).From("NAME").RequiredColumn(),
            new TabularMapOptions { IgnoreCase = ignoreCase });
        var result = map.Map(new TestRow(1, new[] { "name" }, new object[] { "张三" }));
        Assert.Equal(success, result.IsSuccess);
    }

    [Fact]
    public void SchemaAmbiguityIsRejectedBeforeValueReads()
    {
        var map = TabularMap<User>.Create(m => m.Field(x => x.Name).From("姓名", "用户名"));
        var row = new TestRow(1, new[] { "姓名", "用户名" }, new object[] { "A", "B" });
        var error = Assert.Throws<TabularMappingException>(() => map.Map(row));
        Assert.Equal("mapping.duplicate_mapping", error.Code);
        Assert.Equal(0, row.ValueReads);
        var mixed = TabularMap<User>.Create(m =>
        {
            m.Field(x => x.Name).FromOrdinal(0);
            m.Field(x => x.Age).From("姓名");
        });
        Assert.Throws<TabularMappingException>(() => mixed.ValidateColumns(new[] { "姓名" }));
        Assert.Throws<TabularMappingException>(() => map.ValidateColumns(new[] { "姓名", "姓名" }));
    }

    [Fact]
    public void ConfigurationFreezesAndRejectsMixedSourcesInBothOrders()
    {
        TabularFieldMap<User, string> saved = null;
        var map = TabularMap<User>.Create(m => saved = m.Field(x => x.Name).From("姓名"));
        Assert.Throws<InvalidOperationException>(() => saved.From("新名"));
        Assert.Throws<InvalidOperationException>(() => saved.RequiredValue());
        Assert.Throws<InvalidOperationException>(() => map.Field(x => x.Age));
        Assert.Throws<TabularMappingException>(() => TabularMap<User>.Create(m => m.Field(x => x.Name).FromOrdinal(0).From("姓名")));
        Assert.Throws<TabularMappingException>(() => TabularMap<User>.Create(m => m.Field(x => x.Name).From("姓名").FromOrdinal(0)));
        Assert.Throws<TabularMappingException>(() => TabularMap<User>.Create(m =>
        {
            m.Field(x => x.Name).FromOrdinal(0);
            m.Field(x => x.Age).FromOrdinal(0);
        }));
    }

    [Theory]
    [InlineData(UnknownColumnBehavior.Ignore, true, 0)]
    [InlineData(UnknownColumnBehavior.Warning, true, 1)]
    [InlineData(UnknownColumnBehavior.Error, false, 1)]
    public void UnknownColumnPoliciesMatchPreflight(UnknownColumnBehavior behavior, bool success, int count)
    {
        var map = TabularMap<User>.Create(m => m.Field(x => x.Name), new TabularMapOptions { UnknownColumns = behavior });
        var row = new TestRow(1, new[] { "Name", "extra" }, new object[] { "张三", 3 });
        Assert.Equal(count, map.ValidateColumns(new[] { "Name", "extra" }).Count);
        var result = map.Map(row);
        Assert.Equal(success, result.IsSuccess);
        Assert.Equal(count, result.Issues.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyOptionalNullableValuesBecomeNull(string input)
    {
        var map = TabularMap<NullableValue>.Create(m => m.Field(x => x.Value));
        var result = map.Map(new TestRow(1, new[] { "Value" }, new object[] { input }));
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Value);
        Assert.True(map.Map(new TestRow(2, new[] { "Value" }, new object[] { DBNull.Value })).IsSuccess);
    }

    [Fact]
    public void CompiledSettersHandleStructPropertiesAndFields()
    {
        var map = TabularMap<StructTarget>.Create(m =>
        {
            m.Field(x => x.Id);
            m.Field(x => x.Name);
        });
        var result = map.Map(new TestRow(1, new[] { "Id", "Name" }, new object[] { "12", "张三" }));
        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value.Id);
        Assert.Equal("张三", result.Value.Name);
    }

    [Fact]
    public void CustomConverterErrorsDoNotExposeValuesAndCancellationEscapes()
    {
        var secret = "private-value-123";
        var map = TabularMap<User>.Create(m =>
        {
            m.Field(x => x.Name).ConvertUsing((value, _) => throw new InvalidOperationException(Convert.ToString(value)));
            m.Field(x => x.Age);
        });
        var row = new TestRow(1, new[] { "Name", "Age" }, new object[] { secret, "not-an-int" });
        var result = map.Map(row);
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Issues.Count);
        Assert.All(result.Issues, issue => Assert.DoesNotContain(secret, issue.Message));
        using var cancellation = new CancellationTokenSource();
        var original = new OperationCanceledException(cancellation.Token);
        var cancelMap = TabularMap<User>.Create(m => m.Field(x => x.Name).ConvertUsing((_, __) => throw original));
        Assert.Same(original, Assert.Throws<OperationCanceledException>(() => cancelMap.Map(row)));
        cancellation.Cancel();
        Assert.Equal(cancellation.Token, Assert.Throws<OperationCanceledException>(() => map.Map(row, cancellation.Token)).CancellationToken);
    }

    [Fact]
    public void LazyValidatorErrorsBecomeIssuesAndValidationCancellationEscapes()
    {
        IEnumerable<string> Fails(User _)
        {
            yield return "姓名不符合规则";
            throw new InvalidOperationException("内部内容");
        }
        var map = TabularMap<User>.Create(m => m.Validate(Fails));
        var result = map.Map(new TestRow(1, Array.Empty<string>(), Array.Empty<object>()));
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Issues.Count);
        Assert.Null(result.Value);
        var original = new OperationCanceledException();
        var canceled = TabularMap<User>.Create(m => m.Validate(_ => throw original));
        Assert.Same(original, Assert.Throws<OperationCanceledException>(() => canceled.Map(new TestRow(1, Array.Empty<string>(), Array.Empty<object>()))));
    }

    [Fact]
    public void ResultFactoriesPreserveFailureInvariant()
    {
        var error = new DataIssue("mapping.conversion", DataIssueSeverity.Error, global::Bing.Operations.OperationStage.Transform, "转换失败");
        Assert.Throws<ArgumentException>(() => RowResult<User>.Success(new User(), issues: new[] { error }));
        Assert.Throws<ArgumentException>(() => RowResult<User>.Failure(Array.Empty<DataIssue>()));
        var result = RowResult<User>.Failure(new[] { error });
        Assert.Null(result.Value);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void DataAnnotationsIssuesIncludeSourceColumn()
    {
        var map = TabularMap<ValidatedUser>.Create(m => m.Field(x => x.Name).From("姓名"));
        var result = map.Map(new CsvRow(new[] { "" }, new[] { "姓名" }, 9));
        var issue = Assert.Single(result.Issues);
        Assert.Equal("姓名", issue.SourceColumnName);
        Assert.Equal(0, issue.SourceOrdinal);
        Assert.Equal(1, Assert.IsType<CsvLocation>(issue.Location).ColumnNumber);
    }

    private sealed class TestRow : ITabularRow
    {
        private readonly string[] _names;
        private readonly object[] _values;

        public TestRow(long rowNumber, string[] names, object[] values)
        {
            RowNumber = rowNumber;
            _names = names;
            _values = values;
            Location = new TabularLocation(rowNumber);
        }

        public long RowNumber { get; }
        public int Count => _values.Length;
        public DataLocation Location { get; }
        public int ValueReads { get; private set; }
        public string GetName(int ordinal) => _names[ordinal];
        public object GetValue(int ordinal) { ValueReads++; return _values[ordinal]; }
    }

    private sealed class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public decimal Score { get; set; }
    }

    private sealed class LocalizedUser
    {
        public decimal Amount { get; set; }
        public string Code { get; set; }
    }

    private sealed class ValidatedUser
    {
        [Required]
        public string Name { get; set; }
    }

    private sealed class Defaults
    {
        public int Count { get; set; } = 42;
        public string Name { get; set; } = "默认";
    }

    private sealed class NullableValue
    {
        public int? Value { get; set; }
    }

    private struct StructTarget
    {
        public int Id { get; set; }
        public string Name;
    }
}
