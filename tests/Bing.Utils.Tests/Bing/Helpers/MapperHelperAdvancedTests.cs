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

    /// <summary>
    /// 用于验证字典值映射到多个目标属性类型的模型。
    /// </summary>
    public sealed class DictionaryTarget
    {
        /// <summary>
        /// 获取或设置目标名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置目标年龄。
        /// </summary>
        public int Age { get; set; }
        /// <summary>
        /// 获取或设置目标状态。
        /// </summary>
        public DictionaryStatus Status { get; set; }
        /// <summary>
        /// 获取或设置目标标识。
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// 获取或设置目标地址。
        /// </summary>
        public AddressTarget Address { get; set; }
    }

    /// <summary>
    /// 包含仅大小写不同属性名的字典映射目标。
    /// </summary>
    public sealed class CaseDictionaryTarget
    {
        /// <summary>
        /// 获取或设置大写开头的名称属性。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置小写名称属性。
        /// </summary>
        public string name { get; set; }
    }

    /// <summary>
    /// 字典映射测试使用的状态枚举。
    /// </summary>
    public enum DictionaryStatus
    {
        /// <summary>
        /// 未知状态。
        /// </summary>
        Unknown,
        /// <summary>
        /// 有效状态。
        /// </summary>
        Active
    }

    /// <summary>
    /// 字典映射测试使用的地址模型。
    /// </summary>
    public sealed class AddressTarget
    {
        /// <summary>
        /// 获取或设置城市名称。
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// 获取或设置邮政编码。
        /// </summary>
        public string ZipCode { get; set; }
    }

    /// <summary>
    /// 用于字段和属性映射测试的源模型。
    /// </summary>
    private sealed class MapSource
    {
        /// <summary>
        /// 获取或设置源名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 源模型中的公开字段。
        /// </summary>
        public int Count;
        /// <summary>
        /// 获取或设置可空语义测试值。
        /// </summary>
        public string Optional { get; set; }
    }

    /// <summary>
    /// 用于字段和属性映射测试的目标模型。
    /// </summary>
    private sealed class MapDestination
    {
        /// <summary>
        /// 映射得到的名称字段。
        /// </summary>
        public string Name;
        /// <summary>
        /// 获取或设置映射得到的计数。
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// 获取或设置可空语义测试值。
        /// </summary>
        public string Optional { get; set; }
    }

    /// <summary>
    /// 用于字符串到数值集合映射测试的源模型。
    /// </summary>
    private sealed class ListSource
    {
        /// <summary>
        /// 获取或设置待转换的数字文本。
        /// </summary>
        public string Number { get; set; }
    }

    /// <summary>
    /// 用于字符串到数值集合映射测试的目标模型。
    /// </summary>
    private sealed class ListDestination
    {
        /// <summary>
        /// 获取或设置转换后的数值。
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// 用于验证特殊目标值类型映射的模型。
    /// </summary>
    private sealed class BoundaryTarget
    {
        /// <summary>
        /// 获取或设置可空整数。
        /// </summary>
        public int? Optional { get; set; }
        /// <summary>
        /// 获取或设置带偏移量的日期时间。
        /// </summary>
        public DateTimeOffset Offset { get; set; }
        /// <summary>
        /// 获取或设置持续时间。
        /// </summary>
        public TimeSpan Duration { get; set; }
        /// <summary>
        /// 获取或设置整数数组。
        /// </summary>
        public int[] Numbers { get; set; }
    }

    /// <summary>
    /// 用于验证转换失败路径的源模型。
    /// </summary>
    private sealed class MapFailureSource
    {
        /// <summary>
        /// 获取或设置无法转换为目标数值的文本。
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 获取或设置名称。
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 用于验证转换失败路径的目标模型。
    /// </summary>
    private sealed class MapFailureDestination
    {
        /// <summary>
        /// 获取或设置待映射的数值。
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 获取或设置名称。
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 包含索引器的映射源样例。
    /// </summary>
    private sealed class IndexerSource
    {
        /// <summary>
        /// 获取或设置普通名称属性。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 返回指定索引的文本表示。
        /// </summary>
        /// <param name="index">要格式化的索引值。</param>
        /// <returns>索引值的十进制文本。</returns>
        public string this[int index] => index.ToString();
    }

    /// <summary>
    /// 包含索引器的映射目标样例。
    /// </summary>
    private sealed class IndexerDestination
    {
        /// <summary>
        /// 获取或设置普通名称属性。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 设置索引器值；该访问器用于确认映射时忽略索引器。
        /// </summary>
        /// <param name="index">索引器键。</param>
        public string this[int index] { set { } }
    }

    /// <summary>
    /// 包含大小写不同同名属性的映射源。
    /// </summary>
    private sealed class CaseMapSource
    {
        /// <summary>
        /// 获取或设置大写开头的名称属性。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 获取或设置小写名称属性。
        /// </summary>
        public string name { get; set; }
    }

    /// <summary>
    /// 用于大小写映射测试的目标模型。
    /// </summary>
    private sealed class CaseMapDestination
    {
        /// <summary>
        /// 获取或设置名称。
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 包含大小写歧义名称的映射目标。
    /// </summary>
    private sealed class AmbiguousCaseMapDestination
    {
        /// <summary>
        /// 获取或设置全大写名称属性。
        /// </summary>
        public string NAME { get; set; }
    }

    /// <summary>
    /// 读取属性时会抛出异常的映射源。
    /// </summary>
    private sealed class ThrowingGetterMapSource
    {
        /// <summary>
        /// 读取时引发异常的属性。
        /// </summary>
        public string Value => throw new InvalidOperationException("getter");
    }

    /// <summary>
    /// 用于映射访问器测试的源模型。
    /// </summary>
    private sealed class AccessorMapSource
    {
        /// <summary>
        /// 获取或设置源文本值。
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// 用于映射访问器测试的目标模型。
    /// </summary>
    private sealed class AccessorMapDestination
    {
        /// <summary>
        /// 获取或设置目标文本值。
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// 设置目标属性时会抛出异常的映射目标。
    /// </summary>
    private sealed class ThrowingSetterMapDestination
    {
        /// <summary>
        /// 设置时引发异常的属性。
        /// </summary>
        public string Value { set => throw new InvalidOperationException("setter"); }
    }

    /// <summary>
    /// 用于验证值类型目标映射限制的结构体。
    /// </summary>
    private struct ValueDestination
    {
        /// <summary>
        /// 获取或设置目标数值。
        /// </summary>
        public int Number { get; set; }
    }
}
