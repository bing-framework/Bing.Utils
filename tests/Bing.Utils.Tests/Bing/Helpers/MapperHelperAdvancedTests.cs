namespace Bing.Helpers;

/// <summary>
/// 测试类：MapperHelper 新增严格映射功能。
/// </summary>
[Trait("Bing.Helpers", "MapperHelper.Advanced")]
public class MapperHelperAdvancedTests
{
    /// <summary>
    /// 测试目的：字典键应能按大小写忽略规则、显式别名和目标成员类型完成映射。
    /// </summary>
    [Fact]
    public void MapDictionary_WithAliasesAndConvertibleValues_MapsTypedObject()
    {
        // Arrange
        var id = Guid.NewGuid();
        IReadOnlyDictionary<string, object> values = new Dictionary<string, object>
        {
            ["display_name"] = "Ada",
            ["age"] = "42",
            ["status"] = "Active",
            ["id"] = id.ToString()
        };
        var options = new DictionaryMapOptions
        {
            PropertyMappings = new Dictionary<string, string> { ["display_name"] = nameof(DictionaryTarget.Name) }
        };

        // Act
        var result = MapperHelper.MapDictionary<DictionaryTarget>(values, options);

        // Assert
        result.Name.ShouldBe("Ada");
        result.Age.ShouldBe(42);
        result.Status.ShouldBe(DictionaryStatus.Active);
        result.Id.ShouldBe(id);
    }

    /// <summary>
    /// 测试目的：字典中复杂对象的 JSON 字符串应按已知目标成员类型反序列化。
    /// </summary>
    [Fact]
    public void MapDictionary_WithJsonObjectValue_DeserializesKnownTargetType()
    {
        // Arrange
        IReadOnlyDictionary<string, object> values = new Dictionary<string, object>
        {
            [nameof(DictionaryTarget.Address)] = "{\"city\":\"Shanghai\",\"zipCode\":\"200000\"}"
        };

        // Act
        var result = values.ToObject<DictionaryTarget>();

        // Assert
        result.Address.ShouldNotBeNull();
        result.Address.City.ShouldBe("Shanghai");
        result.Address.ZipCode.ShouldBe("200000");
    }

    /// <summary>
    /// 测试目的：未忽略转换失败时，应抛出包含源键和目标成员名称的异常。
    /// </summary>
    [Fact]
    public void MapDictionary_WithInvalidValue_ThrowsContextualException()
    {
        // Arrange
        IReadOnlyDictionary<string, object> values = new Dictionary<string, object>
        {
            [nameof(DictionaryTarget.Age)] = "not-an-integer"
        };

        // Act
        var exception = Should.Throw<ArgumentException>(() => MapperHelper.MapDictionary<DictionaryTarget>(values));

        // Assert
        exception.Message.ShouldContain(nameof(DictionaryTarget.Age));
        exception.Message.ShouldContain(typeof(int).FullName!);
    }

    /// <summary>
    /// 测试目的：启用字段映射时，应支持属性到字段、字段到属性，并尊重排除集合。
    /// </summary>
    [Fact]
    public void MapTo_WithFieldsAndExclusions_MapsAllowedMembersOnly()
    {
        // Arrange
        var source = new MapSource { Name = "Grace", Count = 7, Optional = null };
        var destination = new MapDestination { Count = -1, Optional = "keep" };
        var options = new MapOptions
        {
            IncludeFields = true,
            IgnoreNullValues = true,
            ExcludedMembers = new HashSet<string> { nameof(MapDestination.Count) }
        };

        // Act
        var count = MapperHelper.MapTo(source, destination, options);

        // Assert
        count.ShouldBe(1);
        destination.Name.ShouldBe("Grace");
        destination.Count.ShouldBe(-1);
        destination.Optional.ShouldBe("keep");
    }

    /// <summary>
    /// 测试目的：MapList 应按源集合顺序创建目标对象并应用类型转换。
    /// </summary>
    [Fact]
    public void MapList_WithConvertibleMembers_ReturnsMappedListInSourceOrder()
    {
        // Arrange
        var source = new[]
        {
            new ListSource { Number = "3" },
            new ListSource { Number = "9" }
        };

        // Act
        var result = MapperHelper.MapList<ListSource, ListDestination>(source);

        // Assert
        result.Count.ShouldBe(2);
        result[0].Number.ShouldBe(3);
        result[1].Number.ShouldBe(9);
    }

    /// <summary>
    /// 测试目的：字典映射应正确处理 DBNull、nullable、日期偏移量、时长和 JSON 集合。
    /// </summary>
    [Fact]
    public void MapDictionary_WithBoundaryValueTypes_MapsAllSupportedValues()
    {
        // Arrange
        IReadOnlyDictionary<string, object> values = new Dictionary<string, object>
        {
            [nameof(BoundaryTarget.Optional)] = DBNull.Value,
            [nameof(BoundaryTarget.Offset)] = "2024-01-02T03:04:05+08:00",
            [nameof(BoundaryTarget.Duration)] = "01:02:03",
            [nameof(BoundaryTarget.Numbers)] = "[1,2,3]"
        };

        // Act
        var result = MapperHelper.MapDictionary<BoundaryTarget>(values);

        // Assert
        result.Optional.ShouldBeNull();
        result.Offset.Offset.ShouldBe(TimeSpan.FromHours(8));
        result.Duration.ShouldBe(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(3));
        result.Numbers.ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 测试目的：忽略转换异常选项启用后，应跳过无效值并继续映射其他成员。
    /// </summary>
    [Fact]
    public void MapDictionary_WithIgnoreConversionErrors_SkipsOnlyInvalidValue()
    {
        // Arrange
        IReadOnlyDictionary<string, object> values = new Dictionary<string, object>
        {
            [nameof(DictionaryTarget.Age)] = "invalid",
            [nameof(DictionaryTarget.Name)] = "Ada"
        };

        // Act
        var result = MapperHelper.MapDictionary<DictionaryTarget>(values, new DictionaryMapOptions { IgnoreConversionErrors = true });

        // Assert
        result.Age.ShouldBe(0);
        result.Name.ShouldBe("Ada");
    }

    /// <summary>
    /// 测试目的：关闭 ConvertValues 后，类型不一致的成员应抛出上下文明确的异常。
    /// </summary>
    [Fact]
    public void MapTo_WithConvertValuesDisabled_ThrowsContextualException()
    {
        // Arrange
        var source = new ListSource { Number = "9" };
        var destination = new ListDestination();

        // Act
        var exception = Should.Throw<ArgumentException>(() => MapperHelper.MapTo(source, destination, new MapOptions { ConvertValues = false }));

        // Assert
        exception.Message.ShouldContain(nameof(ListSource.Number));
        exception.Message.ShouldContain(nameof(ListDestination.Number));
    }

    /// <summary>
    /// 测试目的：对象映射启用宽松转换失败时，应跳过失败成员且保留其他成功映射。
    /// </summary>
    [Fact]
    public void MapTo_WithIgnoreConversionErrors_SkipsInvalidMember()
    {
        // Arrange
        var source = new MapFailureSource { Number = "invalid", Name = "Lin" };
        var destination = new MapFailureDestination();

        // Act
        var count = MapperHelper.MapTo(source, destination, new MapOptions { IgnoreConversionErrors = true });

        // Assert
        count.ShouldBe(1);
        destination.Number.ShouldBe(0);
        destination.Name.ShouldBe("Lin");
    }

    /// <summary>
    /// 测试目的：对象映射不应尝试读写索引器属性。
    /// </summary>
    [Fact]
    public void MapTo_WithIndexerProperties_MapsOnlyOrdinaryProperties()
    {
        // Arrange
        var source = new IndexerSource { Name = "Index ignored" };
        var destination = new IndexerDestination();

        // Act
        var count = MapperHelper.MapTo(source, destination);

        // Assert
        count.ShouldBe(1);
        destination.Name.ShouldBe("Index ignored");
    }

    /// <summary>
    /// 测试目的：MapList 应跳过 null 元素，并保留非 null 元素的相对顺序。
    /// </summary>
    [Fact]
    public void MapList_WithNullElements_SkipsNullAndPreservesOrder()
    {
        // Arrange
        var source = new ListSource[] { new() { Number = "1" }, null!, new() { Number = "2" } };

        // Act
        var result = MapperHelper.MapList<ListSource, ListDestination>(source);

        // Assert
        result.Select(item => item.Number).ShouldBe(new[] { 1, 2 });
    }

    /// <summary>
    /// 测试目的：多个源键映射到同一目标成员时，第一个成功值应保留；失败转换不应占用目标成员。
    /// </summary>
    [Fact]
    public void MapDictionary_WithDuplicateMappedTarget_UsesFirstSuccessfulValue()
    {
        // Arrange
        IReadOnlyDictionary<string, object> firstSuccessfulValues = new Dictionary<string, object>
        {
            ["first"] = "first",
            ["second"] = "second"
        };
        IReadOnlyDictionary<string, object> retryAfterFailureValues = new Dictionary<string, object>
        {
            ["first"] = "invalid",
            ["second"] = "7"
        };
        var nameMappings = new Dictionary<string, string>
        {
            ["first"] = nameof(DictionaryTarget.Name),
            ["second"] = nameof(DictionaryTarget.Name)
        };
        var ageMappings = new Dictionary<string, string>
        {
            ["first"] = nameof(DictionaryTarget.Age),
            ["second"] = nameof(DictionaryTarget.Age)
        };

        // Act
        var firstSuccessful = MapperHelper.MapDictionary<DictionaryTarget>(firstSuccessfulValues,
            new DictionaryMapOptions { PropertyMappings = nameMappings });
        var retried = MapperHelper.MapDictionary<DictionaryTarget>(retryAfterFailureValues,
            new DictionaryMapOptions { PropertyMappings = ageMappings, IgnoreConversionErrors = true });

        // Assert
        firstSuccessful.Name.ShouldBe("first");
        retried.Age.ShouldBe(7);
    }

    /// <summary>
    /// 测试目的：字典映射应优先精确大小写匹配；无法消歧的忽略大小写匹配应抛异常。
    /// </summary>
    [Fact]
    public void MapDictionary_WithCaseConflicts_UsesExactMatchOrThrowsAmbiguity()
    {
        // Arrange
        IReadOnlyDictionary<string, object> exactValues = new Dictionary<string, object> { ["Name"] = "upper" };
        IReadOnlyDictionary<string, object> ambiguousValues = new Dictionary<string, object> { ["NAME"] = "ambiguous" };

        // Act
        var exact = MapperHelper.MapDictionary<CaseDictionaryTarget>(exactValues);

        // Assert
        exact.Name.ShouldBe("upper");
        exact.name.ShouldBeNull();
        Should.Throw<AmbiguousMatchException>(() => MapperHelper.MapDictionary<CaseDictionaryTarget>(ambiguousValues));
    }

    /// <summary>
    /// 测试目的：未知或空白键在严格模式下应失败，非泛型字典的非字符串键也应失败。
    /// </summary>
    [Fact]
    public void MapDictionary_WithInvalidKeys_ThrowsArgumentException()
    {
        // Arrange
        IReadOnlyDictionary<string, object> unknownValues = new Dictionary<string, object> { ["Unknown"] = 1 };
        IReadOnlyDictionary<string, object> whitespaceValues = new Dictionary<string, object> { [" "] = 1 };
        IDictionary nonStringKeyValues = new Hashtable { [1] = "value" };

        // Act & Assert
        Should.Throw<ArgumentException>(() => MapperHelper.MapDictionary<DictionaryTarget>(unknownValues,
            new DictionaryMapOptions { IgnoreUnknownKeys = false }));
        Should.Throw<ArgumentException>(() => MapperHelper.MapDictionary<DictionaryTarget>(whitespaceValues,
            new DictionaryMapOptions { IgnoreUnknownKeys = false }));
        Should.Throw<ArgumentException>(() => MapperHelper.MapDictionary<DictionaryTarget>(nonStringKeyValues));
        Should.Throw<ArgumentNullException>(() => MapperHelper.MapDictionary<DictionaryTarget>((IReadOnlyDictionary<string, object>)null!));
    }

    /// <summary>
    /// 测试目的：无效 JSON 在严格模式下应报告转换上下文，在宽松模式下应跳过。
    /// </summary>
    [Fact]
    public void MapDictionary_WithInvalidJson_UsesConfiguredConversionFailureBehavior()
    {
        // Arrange
        IReadOnlyDictionary<string, object> values = new Dictionary<string, object>
        {
            [nameof(BoundaryTarget.Numbers)] = "[invalid]"
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => MapperHelper.MapDictionary<BoundaryTarget>(values));
        MapperHelper.MapDictionary<BoundaryTarget>(values, new DictionaryMapOptions { IgnoreConversionErrors = true })
            .Numbers.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：MapTo 应按精确大小写优先选择源成员，无法消歧时抛出标准歧义异常。
    /// </summary>
    [Fact]
    public void MapTo_WithCaseConflicts_UsesExactSourceOrThrowsAmbiguity()
    {
        // Arrange
        var source = new CaseMapSource { Name = "exact", name = "insensitive" };
        var exactDestination = new CaseMapDestination();
        var ambiguousDestination = new AmbiguousCaseMapDestination();

        // Act
        var count = MapperHelper.MapTo(source, exactDestination);

        // Assert
        count.ShouldBe(1);
        exactDestination.Name.ShouldBe("exact");
        Should.Throw<AmbiguousMatchException>(() => MapperHelper.MapTo(source, ambiguousDestination));
    }

    /// <summary>
    /// 测试目的：包含和排除集合应按排除优先级筛选目标成员。
    /// </summary>
    [Fact]
    public void MapTo_WithIncludedAndExcludedMembers_ExclusionTakesPrecedence()
    {
        // Arrange
        var source = new MapFailureSource { Name = "name", Number = "5" };
        var destination = new MapFailureDestination { Name = "original" };
        var options = new MapOptions
        {
            IncludedMembers = new HashSet<string> { nameof(MapFailureDestination.Name), nameof(MapFailureDestination.Number) },
            ExcludedMembers = new HashSet<string> { nameof(MapFailureDestination.Name) }
        };

        // Act
        var count = MapperHelper.MapTo(source, destination, options);

        // Assert
        count.ShouldBe(1);
        destination.Name.ShouldBe("original");
        destination.Number.ShouldBe(5);
    }

    /// <summary>
    /// 测试目的：MapTo 应将 getter 和 setter 的用户异常包装为包含原始异常的状态异常。
    /// </summary>
    [Fact]
    public void MapTo_WhenAccessorThrows_WrapsOriginalException()
    {
        // Arrange
        var getterDestination = new AccessorMapDestination();
        var setterSource = new AccessorMapSource { Value = "value" };

        // Act
        var getterException = Should.Throw<InvalidOperationException>(() => MapperHelper.MapTo(new ThrowingGetterMapSource(), getterDestination));
        var setterException = Should.Throw<InvalidOperationException>(() => MapperHelper.MapTo(setterSource, new ThrowingSetterMapDestination()));

        // Assert
        getterException.InnerException.ShouldNotBeNull();
        setterException.InnerException.ShouldNotBeNull();
    }

    /// <summary>
    /// 测试目的：MapTo 的 null 输入和值类型目标，以及 MapList 的 null/空集合和值类型目标应遵循参数契约。
    /// </summary>
    [Fact]
    public void MapToAndMapList_WithInvalidInput_ThrowsOrReturnsEmptyCollection()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => MapperHelper.MapTo<ListSource, ListDestination>(null!, new ListDestination()));
        Should.Throw<ArgumentNullException>(() => MapperHelper.MapTo(new ListSource(), (ListDestination)null!));
        Should.Throw<ArgumentException>(() => MapperHelper.MapTo(new ListSource(), new ValueDestination()));
        Should.Throw<ArgumentNullException>(() => MapperHelper.MapList<ListSource, ListDestination>(null!));
        MapperHelper.MapList<ListSource, ListDestination>(Array.Empty<ListSource>()).ShouldBeEmpty();
        Should.Throw<ArgumentException>(() => MapperHelper.MapList<ListSource, ValueDestination>(Array.Empty<ListSource>()));
    }

    /// <summary>
    /// 测试目的：旧 Map 系列也应排除索引器，避免索引参数参与映射。
    /// </summary>
    [Fact]
    public void Map_WithIndexerProperties_MapsOrdinaryPropertyOnly()
    {
        // Arrange
        var source = new IndexerSource { Name = "legacy" };

        // Act
        var result = MapperHelper.Map<IndexerSource, IndexerDestination>(source);

        // Assert
        result.Name.ShouldBe("legacy");
    }

    public sealed class DictionaryTarget
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DictionaryStatus Status { get; set; }
        public Guid Id { get; set; }
        public AddressTarget Address { get; set; }
    }

    public sealed class CaseDictionaryTarget
    {
        public string Name { get; set; }
        public string name { get; set; }
    }

    public enum DictionaryStatus
    {
        Unknown,
        Active
    }

    public sealed class AddressTarget
    {
        public string City { get; set; }
        public string ZipCode { get; set; }
    }

    private sealed class MapSource
    {
        public string Name { get; set; }
        public int Count;
        public string Optional { get; set; }
    }

    private sealed class MapDestination
    {
        public string Name;
        public int Count { get; set; }
        public string Optional { get; set; }
    }

    private sealed class ListSource
    {
        public string Number { get; set; }
    }

    private sealed class ListDestination
    {
        public int Number { get; set; }
    }

    private sealed class BoundaryTarget
    {
        public int? Optional { get; set; }
        public DateTimeOffset Offset { get; set; }
        public TimeSpan Duration { get; set; }
        public int[] Numbers { get; set; }
    }

    private sealed class MapFailureSource
    {
        public string Number { get; set; }
        public string Name { get; set; }
    }

    private sealed class MapFailureDestination
    {
        public int Number { get; set; }
        public string Name { get; set; }
    }

    private sealed class IndexerSource
    {
        public string Name { get; set; }
        public string this[int index] => index.ToString();
    }

    private sealed class IndexerDestination
    {
        public string Name { get; set; }
        public string this[int index] { set { } }
    }

    private sealed class CaseMapSource
    {
        public string Name { get; set; }
        public string name { get; set; }
    }

    private sealed class CaseMapDestination
    {
        public string Name { get; set; }
    }

    private sealed class AmbiguousCaseMapDestination
    {
        public string NAME { get; set; }
    }

    private sealed class ThrowingGetterMapSource
    {
        public string Value => throw new InvalidOperationException("getter");
    }

    private sealed class AccessorMapSource
    {
        public string Value { get; set; }
    }

    private sealed class AccessorMapDestination
    {
        public string Value { get; set; }
    }

    private sealed class ThrowingSetterMapDestination
    {
        public string Value { set => throw new InvalidOperationException("setter"); }
    }

    private struct ValueDestination
    {
        public int Number { get; set; }
    }
}
