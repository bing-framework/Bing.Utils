using System.Collections.Concurrent;

namespace Bing.Helpers;

/// <summary>
/// 映射器帮助类 单元测试
/// </summary>
[Trait("Bing.Helpers", "MapperHelper")]
public class MapperHelperTest : TestBase
{
    /// <inheritdoc />
    public MapperHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    #region 测试模型类

    /// <summary>
    /// 源模型类
    /// </summary>
    public class SourceModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public DateTime? BirthDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Salary { get; set; }
        public string Department { get; set; }
        public string Password { get; set; }
        public string ReadOnlyProperty => "ReadOnly";
        public string WriteOnlyProperty { private get; set; }
    }

    /// <summary>
    /// 目标模型类
    /// </summary>
    public class DestinationModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public DateTime? BirthDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Salary { get; set; }
        public string Department { get; set; }
        public string AdditionalInfo { get; set; }
        public string ReadOnlyProperty { get; }
        public string WriteOnlyProperty { set; private get; }
    }

    /// <summary>
    /// 部分属性模型类
    /// </summary>
    public class PartialModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    /// <summary>
    /// 不同属性名模型类
    /// </summary>
    public class DifferentPropertyModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
    }

    /// <summary>
    /// 空模型类
    /// </summary>
    public class EmptyModel
    {
    }

    /// <summary>
    /// 嵌套模型类
    /// </summary>
    public class NestedModel
    {
        public int Id { get; set; }
        public SourceModel NestedObject { get; set; }
    }

    /// <summary>
    /// 大小写测试源模型
    /// </summary>
    public class CaseTestSource
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string emailAddress { get; set; }
    }

    /// <summary>
    /// 大小写测试目标模型
    /// </summary>
    public class CaseTestDestination
    {
        public int id { get; set; }
        public string name { get; set; }
        public string EmailAddress { get; set; }
    }

    /// <summary>
    /// 复杂类型源模型
    /// </summary>
    public class ComplexTypeSource
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public decimal[] Scores { get; set; }
        public NestedModel NestedObject { get; set; }
        public DateTime CreatedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public Guid UniqueId { get; set; }
        public Uri Website { get; set; }
    }

    /// <summary>
    /// 复杂类型目标模型
    /// </summary>
    public class ComplexTypeDestination
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public decimal[] Scores { get; set; }
        public NestedModel NestedObject { get; set; }
        public DateTime CreatedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public Guid UniqueId { get; set; }
        public Uri Website { get; set; }
    }

    /// <summary>
    /// 异常测试源模型
    /// </summary>
    public class ExceptionTestSource
    {
        public int Id { get; set; }
        public string ThrowingProperty => throw new InvalidOperationException("Test exception");
    }

    /// <summary>
    /// 异常测试目标模型
    /// </summary>
    public class ExceptionTestDestination
    {
        public int Id { get; set; }
        public string ThrowingProperty { get; set; }
    }

    /// <summary>
    /// 值类型测试源模型
    /// </summary>
    public struct ValueTypeSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 值类型测试目标模型
    /// </summary>
    public struct ValueTypeDestination
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 索引器测试模型
    /// </summary>
    public class IndexerTestModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private readonly Dictionary<string, object> _data = new();
        public object this[string key]
        {
            get => _data.TryGetValue(key, out var value) ? value : null;
            set => _data[key] = value;
        }
    }

    /// <summary>
    /// 静态属性测试模型
    /// </summary>
    public class StaticPropertyModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public static string StaticProperty { get; set; } = "Static";
    }

    /// <summary>
    /// 只读字段测试模型
    /// </summary>
    public class ReadOnlyFieldModel
    {
        public readonly string ReadOnlyField = "ReadOnly";
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// 集合测试源模型
    /// </summary>
    public class CollectionTestSource
    {
        public int Id { get; set; }
        public IList<string> StringList { get; set; }
        public ICollection<int> IntCollection { get; set; }
        public IEnumerable<DateTime> DateEnumerable { get; set; }
        public string[] StringArray { get; set; }
        public HashSet<string> StringHashSet { get; set; }
        public Queue<int> IntQueue { get; set; }
        public Stack<string> StringStack { get; set; }
    }

    /// <summary>
    /// 集合测试目标模型
    /// </summary>
    public class CollectionTestDestination
    {
        public int Id { get; set; }
        public IList<string> StringList { get; set; }
        public ICollection<int> IntCollection { get; set; }
        public IEnumerable<DateTime> DateEnumerable { get; set; }
        public string[] StringArray { get; set; }
        public HashSet<string> StringHashSet { get; set; }
        public Queue<int> IntQueue { get; set; }
        public Stack<string> StringStack { get; set; }
    }

    #endregion

    #region 嵌套结构体测试模型

    /// <summary>
    /// 地址结构体（源）
    /// </summary>
    public struct AddressSource
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    /// <summary>
    /// 地址结构体（目标）
    /// </summary>
    public struct AddressDestination
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    /// <summary>
    /// 联系人结构体（源）
    /// </summary>
    public struct ContactSource
    {
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool IsPreferred { get; set; }
    }

    /// <summary>
    /// 联系人结构体（目标）
    /// </summary>
    public struct ContactDestination
    {
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool IsPreferred { get; set; }
    }

    /// <summary>
    /// 嵌套结构体测试源模型
    /// </summary>
    public struct NestedStructSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AddressSource Address { get; set; }
        public ContactSource Contact { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 嵌套结构体测试目标模型
    /// </summary>
    public struct NestedStructDestination
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AddressDestination Address { get; set; }
        public ContactDestination Contact { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 深度嵌套结构体源模型
    /// </summary>
    public struct DeepNestedSource
    {
        public int Level1Id { get; set; }
        public NestedStructSource Level2 { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 深度嵌套结构体目标模型
    /// </summary>
    public struct DeepNestedDestination
    {
        public int Level1Id { get; set; }
        public NestedStructDestination Level2 { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 混合嵌套测试模型（包含可空结构体）
    /// </summary>
    public struct MixedNestedSource
    {
        public int Id { get; set; }
        public AddressSource? OptionalAddress { get; set; }
        public ContactSource Contact { get; set; }
        public string[] Tags { get; set; }
    }

    /// <summary>
    /// 混合嵌套测试模型（包含可空结构体）
    /// </summary>
    public struct MixedNestedDestination
    {
        public int Id { get; set; }
        public AddressDestination? OptionalAddress { get; set; }
        public ContactDestination Contact { get; set; }
        public string[] Tags { get; set; }
    }

    #endregion

    #region Map 方法测试

    /// <summary>
    /// 测试 - Map - 基础属性映射
    /// </summary>
    [Fact]
    public void Map_BasicProperties_ShouldMapSuccessfully()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30,
            BirthDate = new DateTime(1993, 1, 1),
            IsActive = true,
            Salary = 50000.50m,
            Department = "IT",
            Password = "secret123"
        };

        // Act
        var result = MapperHelper.Map<SourceModel, DestinationModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
        result.Age.ShouldBe(source.Age);
        result.BirthDate.ShouldBe(source.BirthDate);
        result.IsActive.ShouldBe(source.IsActive);
        result.Salary.ShouldBe(source.Salary);
        result.Department.ShouldBe(source.Department);
    }

    /// <summary>
    /// 测试 - Map - 空源对象抛出异常
    /// </summary>
    [Fact]
    public void Map_NullSource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            MapperHelper.Map<SourceModel, DestinationModel>(null));
        exception.ParamName.ShouldBe("source");
    }

    /// <summary>
    /// 测试 - Map - 同类型映射
    /// </summary>
    [Fact]
    public void Map_SameType_ShouldMapSuccessfully()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com"
        };

        // Act
        var result = MapperHelper.Map<SourceModel, SourceModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
        result.ShouldNotBeSameAs(source); // 应该是新实例
    }

    /// <summary>
    /// 测试 - Map - 部分属性映射
    /// </summary>
    [Fact]
    public void Map_PartialProperties_ShouldMapMatchingProperties()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30,
            Department = "IT"
        };

        // Act
        var result = MapperHelper.Map<SourceModel, PartialModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
    }

    /// <summary>
    /// 测试 - Map - 不同属性名不映射
    /// </summary>
    [Fact]
    public void Map_DifferentPropertyNames_ShouldNotMap()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = MapperHelper.Map<SourceModel, DifferentPropertyModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(0); // 默认值
        result.UserName.ShouldBeNull(); // 默认值
        result.UserEmail.ShouldBeNull(); // 默认值
    }

    /// <summary>
    /// 测试 - Map - 空模型映射
    /// </summary>
    [Fact]
    public void Map_EmptyModel_ShouldReturnEmptyInstance()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe"
        };

        // Act
        var result = MapperHelper.Map<SourceModel, EmptyModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<EmptyModel>();
    }

    /// <summary>
    /// 测试 - Map - Null值属性映射
    /// </summary>
    [Fact]
    public void Map_NullProperties_ShouldMapNullValues()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = null,
            Email = null,
            BirthDate = null
        };

        // Act
        var result = MapperHelper.Map<SourceModel, DestinationModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBeNull();
        result.Email.ShouldBeNull();
        result.BirthDate.ShouldBeNull();
    }

    /// <summary>
    /// 测试 - Map - 大小写不敏感映射
    /// </summary>
    [Fact]
    public void Map_CaseInsensitive_ShouldMapSuccessfully()
    {
        // Arrange
        var source = new CaseTestSource
        {
            ID = 1,
            NAME = "John Doe",
            emailAddress = "john@example.com"
        };

        // Act
        var result = MapperHelper.Map<CaseTestSource, CaseTestDestination>(source);

        // Assert
        result.ShouldNotBeNull();
        result.id.ShouldBe(source.ID);
        result.name.ShouldBe(source.NAME);
        result.EmailAddress.ShouldBe(source.emailAddress);
    }

    #endregion

    #region MapWith 方法测试

    /// <summary>
    /// 测试 - MapWith - 指定属性映射
    /// </summary>
    [Fact]
    public void MapWith_SpecifiedProperties_ShouldMapOnlySpecifiedProperties()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30,
            Department = "IT"
        };

        // Act
        var result = MapperHelper.MapWith<SourceModel, DestinationModel>(
            source, "Id", "Name");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBeNull(); // 未指定，应为默认值
        result.Age.ShouldBe(0); // 未指定，应为默认值
        result.Department.ShouldBeNull(); // 未指定，应为默认值
    }

    /// <summary>
    /// 测试 - MapWith - 大小写不敏感属性名
    /// </summary>
    [Fact]
    public void MapWith_CaseInsensitivePropertyNames_ShouldMapSuccessfully()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = MapperHelper.MapWith<SourceModel, DestinationModel>(
            source, "id", "NAME", "Email");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
    }

    /// <summary>
    /// 测试 - MapWith - 空属性数组
    /// </summary>
    [Fact]
    public void MapWith_EmptyPropertyArray_ShouldMapNothing()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = MapperHelper.MapWith<SourceModel, DestinationModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(0); // 默认值
        result.Name.ShouldBeNull(); // 默认值
        result.Email.ShouldBeNull(); // 默认值
    }

    /// <summary>
    /// 测试 - MapWith - null属性数组
    /// </summary>
    [Fact]
    public void MapWith_NullPropertyArray_ShouldMapNothing()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = MapperHelper.MapWith<SourceModel, DestinationModel>(source, null);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(0); // 默认值
        result.Name.ShouldBeNull(); // 默认值
        result.Email.ShouldBeNull(); // 默认值
    }

    /// <summary>
    /// 测试 - MapWith - 不存在的属性名
    /// </summary>
    [Fact]
    public void MapWith_NonExistentProperties_ShouldIgnoreAndMapExisting()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe"
        };

        // Act
        var result = MapperHelper.MapWith<SourceModel, DestinationModel>(
            source, "Id", "NonExistentProperty", "Name");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
    }

    /// <summary>
    /// 测试 - MapWith - 空源对象抛出异常
    /// </summary>
    [Fact]
    public void MapWith_NullSource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            MapperHelper.MapWith<SourceModel, DestinationModel>(null, "Id", "Name"));
        exception.ParamName.ShouldBe("source");
    }

    #endregion

    #region MapWithout 方法测试

    /// <summary>
    /// 测试 - MapWithout - 排除指定属性映射
    /// </summary>
    [Fact]
    public void MapWithout_ExcludeSpecifiedProperties_ShouldMapAllExceptExcluded()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30,
            Department = "IT"
        };

        // Act
        var result = MapperHelper.MapWithout<SourceModel, DestinationModel>(
            source, "Email", "Age");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBeNull(); // 被排除
        result.Age.ShouldBe(0); // 被排除
        result.Department.ShouldBe(source.Department); // 未被排除
    }

    /// <summary>
    /// 测试 - MapWithout - 大小写不敏感排除
    /// </summary>
    [Fact]
    public void MapWithout_CaseInsensitiveExclusion_ShouldExcludeCorrectly()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = MapperHelper.MapWithout<SourceModel, DestinationModel>(
            source, "EMAIL", "name");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBeNull(); // 被排除（大小写不敏感）
        result.Email.ShouldBeNull(); // 被排除（大小写不敏感）
    }

    /// <summary>
    /// 测试 - MapWithout - 空排除数组
    /// </summary>
    [Fact]
    public void MapWithout_EmptyExclusionArray_ShouldMapAllProperties()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        // Act
        var result = MapperHelper.MapWithout<SourceModel, DestinationModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
        result.Age.ShouldBe(source.Age);
    }

    /// <summary>
    /// 测试 - MapWithout - 不存在的排除属性
    /// </summary>
    [Fact]
    public void MapWithout_NonExistentExclusionProperties_ShouldIgnoreAndMapAll()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = MapperHelper.MapWithout<SourceModel, DestinationModel>(
            source, "NonExistentProperty1", "NonExistentProperty2");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
    }

    /// <summary>
    /// 测试 - MapWithout - 空源对象抛出异常
    /// </summary>
    [Fact]
    public void MapWithout_NullSource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            MapperHelper.MapWithout<SourceModel, DestinationModel>(null, "Email"));
        exception.ParamName.ShouldBe("source");
    }

    #endregion

    #region 边界条件和性能测试

    /// <summary>
    /// 测试 - Map - 大量属性映射性能
    /// </summary>
    [Fact]
    public void Map_LargeObjectMapping_ShouldPerformReasonably()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30,
            BirthDate = DateTime.Now,
            IsActive = true,
            Salary = 50000.50m,
            Department = "IT",
            Password = "secret123"
        };

        // Act & Assert - 测试多次映射不应该抛出异常
        for (int i = 0; i < 1000; i++)
        {
            var result = MapperHelper.Map<SourceModel, DestinationModel>(source);
            result.ShouldNotBeNull();
            result.Id.ShouldBe(source.Id);
        }
    }

    /// <summary>
    /// 测试 - Map - 递归对象映射（嵌套对象）
    /// </summary>
    [Fact]
    public void Map_NestedObjects_ShouldHandleGracefully()
    {
        // Arrange
        var source = new NestedModel
        {
            Id = 1,
            NestedObject = new SourceModel
            {
                Id = 2,
                Name = "Nested"
            }
        };

        // Act
        var result = MapperHelper.Map<NestedModel, NestedModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        // 注意：当前实现可能不会深度复制嵌套对象
        // 这取决于具体的反射实现
    }

    /// <summary>
    /// 测试 - Map - 包含特殊字符的属性值
    /// </summary>
    [Fact]
    public void Map_SpecialCharactersInValues_ShouldMapCorrectly()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "名字with特殊字符!@#$%^&*()",
            Email = "test+special@example.com",
            Department = "IT & 开发部"
        };

        // Act
        var result = MapperHelper.Map<SourceModel, DestinationModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
        result.Department.ShouldBe(source.Department);
    }

    /// <summary>
    /// 测试 - Map - 极长字符串值
    /// </summary>
    [Fact]
    public void Map_VeryLongStringValues_ShouldMapCorrectly()
    {
        // Arrange
        var longString = new string('A', 10000);
        var source = new SourceModel
        {
            Id = 1,
            Name = longString,
            Email = "test@example.com"
        };

        // Act
        var result = MapperHelper.Map<SourceModel, DestinationModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(longString);
        result.Name.Length.ShouldBe(10000);
    }

    #endregion

    #region 类型转换和兼容性测试

    /// <summary>
    /// 测试模型 - 不同数据类型
    /// </summary>
    public class TypeConversionSource
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public DateTime DateValue { get; set; }
        public bool BoolValue { get; set; }
    }

    /// <summary>
    /// 测试模型 - 可空类型目标
    /// </summary>
    public class TypeConversionDestination
    {
        public int? IntValue { get; set; }
        public string StringValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }
    }

    /// <summary>
    /// 测试 - Map - 基本类型到可空类型映射
    /// </summary>
    [Fact]
    public void Map_BasicToNullableTypes_ShouldMapCorrectly()
    {
        // Arrange
        var source = new TypeConversionSource
        {
            IntValue = 42,
            StringValue = "test",
            DateValue = new DateTime(2023, 1, 1),
            BoolValue = true
        };

        // Act
        var result = MapperHelper.Map<TypeConversionSource, TypeConversionDestination>(source);

        // Assert
        result.ShouldNotBeNull();
        result.IntValue.ShouldBe(42);
        result.StringValue.ShouldBe("test");
        result.DateValue.ShouldBe(new DateTime(2023, 1, 1));
        result.BoolValue.ShouldBe(true);
    }

    #endregion

    #region 复杂类型测试

    /// <summary>
    /// 测试 - Map - 复杂类型映射
    /// </summary>
    [Fact]
    public void Map_ComplexTypes_ShouldMapCorrectly()
    {
        // Arrange
        var source = new ComplexTypeSource
        {
            Id = 1,
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Metadata = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 42 } },
            Scores = new decimal[] { 1.5m, 2.7m, 3.9m },
            NestedObject = new NestedModel { Id = 10 },
            CreatedAt = new DateTime(2023, 1, 1),
            Duration = TimeSpan.FromHours(2),
            UniqueId = Guid.NewGuid(),
            Website = new Uri("https://example.com")
        };

        // Act
        var result = MapperHelper.Map<ComplexTypeSource, ComplexTypeDestination>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Tags.ShouldBeSameAs(source.Tags); // 浅复制
        result.Metadata.ShouldBeSameAs(source.Metadata); // 浅复制
        result.Scores.ShouldBeSameAs(source.Scores); // 浅复制
        result.NestedObject.ShouldBeSameAs(source.NestedObject); // 浅复制
        result.CreatedAt.ShouldBe(source.CreatedAt);
        result.Duration.ShouldBe(source.Duration);
        result.UniqueId.ShouldBe(source.UniqueId);
        result.Website.ShouldBeSameAs(source.Website); // 浅复制
    }

    #endregion

    #region 值类型测试

    /// <summary>
    /// 测试 - Map - 值类型映射
    /// </summary>
    [Fact]
    public void Map_ValueTypes_ShouldMapCorrectly()
    {
        // Arrange
        var source = new ValueTypeSource
        {
            Id = 1,
            Name = "Test",
            CreatedAt = new DateTime(2023, 1, 1)
        };

        // Act
        var result = MapperHelper.Map<ValueTypeSource, ValueTypeDestination>(source);

        // Assert
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.CreatedAt.ShouldBe(source.CreatedAt);
    }

    /// <summary>
    /// 测试 - MapWith - 值类型指定属性映射
    /// </summary>
    [Fact]
    public void MapWith_ValueTypes_ShouldMapSpecifiedPropertiesOnly()
    {
        // Arrange
        var source = new ValueTypeSource
        {
            Id = 1,
            Name = "Test",
            CreatedAt = new DateTime(2023, 1, 1)
        };

        // Act
        var result = MapperHelper.MapWith<ValueTypeSource, ValueTypeDestination>(source, "Id", "Name");

        // Assert
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.CreatedAt.ShouldBe(default); // 未指定，应为默认值
    }

    /// <summary>
    /// 测试 - MapWithout - 值类型排除属性映射
    /// </summary>
    [Fact]
    public void MapWithout_ValueTypes_ShouldMapAllExceptExcluded()
    {
        // Arrange
        var source = new ValueTypeSource
        {
            Id = 1,
            Name = "Test",
            CreatedAt = new DateTime(2023, 1, 1)
        };

        // Act
        var result = MapperHelper.MapWithout<ValueTypeSource, ValueTypeDestination>(source, "CreatedAt");

        // Assert
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.CreatedAt.ShouldBe(default); // 被排除，应为默认值
    }

    #endregion

    #region 索引器和特殊属性测试

    /// <summary>
    /// 测试 - Map - 包含索引器的模型
    /// </summary>
    [Fact]
    public void Map_ModelsWithIndexers_ShouldMapNormalPropertiesOnly()
    {
        // Arrange
        var source = new IndexerTestModel
        {
            Id = 1,
            Name = "Test"
        };
        source["key1"] = "value1";

        // Act
        var result = MapperHelper.Map<IndexerTestModel, IndexerTestModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        // 索引器不应被映射
        result["key1"].ShouldBeNull();
    }

    /// <summary>
    /// 测试 - Map - 索引器属性被正确忽略
    /// </summary>
    [Fact]
    public void Map_IndexerProperties_ShouldBeIgnored()
    {
        // Arrange
        var source = new IndexerTestModel
        {
            Id = 10,
            Name = "Indexer Test"
        };

        // 设置索引器值
        source["test1"] = "value1";
        source["test2"] = 42;
        source["test3"] = DateTime.Now;

        var destination = new IndexerTestModel
        {
            Id = 20,
            Name = "Original"
        };

        // 设置目标对象的索引器值
        destination["test1"] = "original1";
        destination["test2"] = 100;

        // Act
        var result = MapperHelper.Map<IndexerTestModel, IndexerTestModel>(source);

        // Assert
        result.ShouldNotBeNull();

        // 普通属性应该被映射
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);

        // 索引器值应该是默认值（null），因为索引器属性被忽略
        result["test1"].ShouldBeNull();
        result["test2"].ShouldBeNull();
        result["test3"].ShouldBeNull();
    }

    /// <summary>
    /// 测试 - Map - 静态属性不被映射
    /// </summary>
    [Fact]
    public void Map_StaticProperties_ShouldNotBeMapped()
    {
        // Arrange
        StaticPropertyModel.StaticProperty = "Changed";
        var source = new StaticPropertyModel
        {
            Id = 1,
            Name = "Test"
        };

        // Act
        var result = MapperHelper.Map<StaticPropertyModel, StaticPropertyModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        // 静态属性应保持原值，不受映射影响
        StaticPropertyModel.StaticProperty.ShouldBe("Changed");
    }

    /// <summary>
    /// 测试 - Map - 只读字段不被映射
    /// </summary>
    [Fact]
    public void Map_ReadOnlyFields_ShouldNotBeMapped()
    {
        // Arrange
        var source = new ReadOnlyFieldModel
        {
            Id = 1,
            Name = "Test"
        };

        // Act
        var result = MapperHelper.Map<ReadOnlyFieldModel, ReadOnlyFieldModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        // 只读字段应保持默认值
        result.ReadOnlyField.ShouldBe("ReadOnly");
    }

    /// <summary>
    /// 测试 - MapWith - 尝试指定索引器属性应被忽略
    /// </summary>
    [Fact]
    public void MapWith_IndexerPropertyName_ShouldBeIgnored()
    {
        // Arrange
        var source = new IndexerTestModel
        {
            Id = 1,
            Name = "Test"
        };

        // Act - 尝试映射 "Item" 属性（索引器属性名）
        var result = MapperHelper.MapWith<IndexerTestModel, IndexerTestModel>(
            source, "Id", "Item", "Name");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        // "Item" 属性（索引器）应该被忽略，不会导致异常
    }

    #endregion

    #region 集合类型测试

    /// <summary>
    /// 测试 - Map - 集合类型映射
    /// </summary>
    [Fact]
    public void Map_CollectionTypes_ShouldMapReferences()
    {
        // Arrange
        var source = new CollectionTestSource
        {
            Id = 1,
            StringList = new List<string> { "a", "b", "c" },
            IntCollection = new List<int> { 1, 2, 3 },
            DateEnumerable = new[] { DateTime.Today, DateTime.Today.AddDays(1) },
            StringArray = new[] { "x", "y", "z" },
            StringHashSet = new HashSet<string> { "set1", "set2" },
            IntQueue = new Queue<int>(new[] { 10, 20, 30 }),
            StringStack = new Stack<string>(new[] { "stack1", "stack2" })
        };

        // Act
        var result = MapperHelper.Map<CollectionTestSource, CollectionTestDestination>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.StringList.ShouldBeSameAs(source.StringList); // 浅复制
        result.IntCollection.ShouldBeSameAs(source.IntCollection); // 浅复制
        result.DateEnumerable.ShouldBeSameAs(source.DateEnumerable); // 浅复制
        result.StringArray.ShouldBeSameAs(source.StringArray); // 浅复制
        result.StringHashSet.ShouldBeSameAs(source.StringHashSet); // 浅复制
        result.IntQueue.ShouldBeSameAs(source.IntQueue); // 浅复制
        result.StringStack.ShouldBeSameAs(source.StringStack); // 浅复制
    }

    /// <summary>
    /// 测试 - Map - 空集合映射
    /// </summary>
    [Fact]
    public void Map_EmptyCollections_ShouldMapCorrectly()
    {
        // Arrange
        var source = new CollectionTestSource
        {
            Id = 1,
            StringList = new List<string>(),
            IntCollection = new List<int>(),
            StringArray = new string[0],
            StringHashSet = new HashSet<string>()
        };

        // Act
        var result = MapperHelper.Map<CollectionTestSource, CollectionTestDestination>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.StringList.ShouldBeSameAs(source.StringList);
        result.IntCollection.ShouldBeSameAs(source.IntCollection);
        result.StringArray.ShouldBeSameAs(source.StringArray);
        result.StringHashSet.ShouldBeSameAs(source.StringHashSet);
    }

    /// <summary>
    /// 测试 - Map - null集合映射
    /// </summary>
    [Fact]
    public void Map_NullCollections_ShouldMapNulls()
    {
        // Arrange
        var source = new CollectionTestSource
        {
            Id = 1,
            StringList = null,
            IntCollection = null,
            StringArray = null,
            StringHashSet = null
        };

        // Act
        var result = MapperHelper.Map<CollectionTestSource, CollectionTestDestination>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.StringList.ShouldBeNull();
        result.IntCollection.ShouldBeNull();
        result.StringArray.ShouldBeNull();
        result.StringHashSet.ShouldBeNull();
    }

    #endregion

    #region 嵌套结构体测试

    /// <summary>
    /// 测试 - Map - 嵌套结构体映射
    /// </summary>
    [Fact]
    public void Map_NestedStructs_ShouldMapCorrectly()
    {
        // Arrange
        var source = new NestedStructSource
        {
            Id = 1,
            Name = "Test User",
            Address = new AddressSource
            {
                Street = "123 Main St",
                City = "New York",
                PostalCode = "10001",
                Country = "USA"
            },
            Contact = new ContactSource
            {
                Phone = "+1-555-0123",
                Email = "test@example.com",
                IsPreferred = true
            },
            CreatedAt = new DateTime(2023, 1, 1),
            IsActive = true
        };

        // Act
        var result = MapperHelper.Map<NestedStructSource, NestedStructDestination>(source);

        // Assert
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.CreatedAt.ShouldBe(source.CreatedAt);
        result.IsActive.ShouldBe(source.IsActive);

        // 验证嵌套的 Address 结构体
        result.Address.Street.ShouldBe(source.Address.Street);
        result.Address.City.ShouldBe(source.Address.City);
        result.Address.PostalCode.ShouldBe(source.Address.PostalCode);
        result.Address.Country.ShouldBe(source.Address.Country);

        // 验证嵌套的 Contact 结构体
        result.Contact.Phone.ShouldBe(source.Contact.Phone);
        result.Contact.Email.ShouldBe(source.Contact.Email);
        result.Contact.IsPreferred.ShouldBe(source.Contact.IsPreferred);
    }

    /// <summary>
    /// 测试 - Map - 深度嵌套结构体映射
    /// </summary>
    [Fact]
    public void Map_DeepNestedStructs_ShouldMapCorrectly()
    {
        // Arrange
        var source = new DeepNestedSource
        {
            Level1Id = 100,
            Description = "Deep nesting test",
            Level2 = new NestedStructSource
            {
                Id = 2,
                Name = "Level 2",
                Address = new AddressSource
                {
                    Street = "456 Oak Ave",
                    City = "Los Angeles",
                    PostalCode = "90210",
                    Country = "USA"
                },
                Contact = new ContactSource
                {
                    Phone = "+1-555-9876",
                    Email = "level2@example.com",
                    IsPreferred = false
                },
                CreatedAt = new DateTime(2023, 6, 15),
                IsActive = true
            }
        };

        // Act
        var result = MapperHelper.Map<DeepNestedSource, DeepNestedDestination>(source);

        // Assert
        result.Level1Id.ShouldBe(source.Level1Id);
        result.Description.ShouldBe(source.Description);

        // 验证二级嵌套
        result.Level2.Id.ShouldBe(source.Level2.Id);
        result.Level2.Name.ShouldBe(source.Level2.Name);
        result.Level2.CreatedAt.ShouldBe(source.Level2.CreatedAt);
        result.Level2.IsActive.ShouldBe(source.Level2.IsActive);

        // 验证三级嵌套的 Address
        result.Level2.Address.Street.ShouldBe(source.Level2.Address.Street);
        result.Level2.Address.City.ShouldBe(source.Level2.Address.City);
        result.Level2.Address.PostalCode.ShouldBe(source.Level2.Address.PostalCode);
        result.Level2.Address.Country.ShouldBe(source.Level2.Address.Country);

        // 验证三级嵌套的 Contact
        result.Level2.Contact.Phone.ShouldBe(source.Level2.Contact.Phone);
        result.Level2.Contact.Email.ShouldBe(source.Level2.Contact.Email);
        result.Level2.Contact.IsPreferred.ShouldBe(source.Level2.Contact.IsPreferred);
    }

    /// <summary>
    /// 测试 - Map - 混合嵌套结构体（包含可空类型）
    /// </summary>
    [Fact]
    public void Map_MixedNestedStructs_ShouldMapCorrectly()
    {
        // Arrange
        var source = new MixedNestedSource
        {
            Id = 1,
            OptionalAddress = new AddressSource
            {
                Street = "789 Pine St",
                City = "Chicago",
                PostalCode = "60601",
                Country = "USA"
            },
            Contact = new ContactSource
            {
                Phone = "+1-555-4567",
                Email = "mixed@example.com",
                IsPreferred = true
            },
            Tags = new[] { "tag1", "tag2", "tag3" }
        };

        // Act
        var result = MapperHelper.Map<MixedNestedSource, MixedNestedDestination>(source);

        // Assert
        result.Id.ShouldBe(source.Id);
        result.Tags.ShouldBeSameAs(source.Tags); // 引用类型浅复制

        // 验证可空嵌套结构体
        result.OptionalAddress.ShouldNotBeNull();
        result.OptionalAddress.Value.Street.ShouldBe(source.OptionalAddress.Value.Street);
        result.OptionalAddress.Value.City.ShouldBe(source.OptionalAddress.Value.City);
        result.OptionalAddress.Value.PostalCode.ShouldBe(source.OptionalAddress.Value.PostalCode);
        result.OptionalAddress.Value.Country.ShouldBe(source.OptionalAddress.Value.Country);

        // 验证普通嵌套结构体
        result.Contact.Phone.ShouldBe(source.Contact.Phone);
        result.Contact.Email.ShouldBe(source.Contact.Email);
        result.Contact.IsPreferred.ShouldBe(source.Contact.IsPreferred);
    }

    /// <summary>
    /// 测试 - Map - 空的可空嵌套结构体
    /// </summary>
    [Fact]
    public void Map_NullableNestedStructs_WithNullValue_ShouldMapCorrectly()
    {
        // Arrange
        var source = new MixedNestedSource
        {
            Id = 2,
            OptionalAddress = null, // 可空结构体设为 null
            Contact = new ContactSource
            {
                Phone = "+1-555-7890",
                Email = "null@example.com",
                IsPreferred = false
            },
            Tags = new[] { "nullable", "test" }
        };

        // Act
        var result = MapperHelper.Map<MixedNestedSource, MixedNestedDestination>(source);

        // Assert
        result.Id.ShouldBe(source.Id);
        result.OptionalAddress.ShouldBeNull(); // 应该正确映射 null 值
        result.Contact.Phone.ShouldBe(source.Contact.Phone);
        result.Contact.Email.ShouldBe(source.Contact.Email);
        result.Contact.IsPreferred.ShouldBe(source.Contact.IsPreferred);
        result.Tags.ShouldBeSameAs(source.Tags);
    }

    /// <summary>
    /// 测试 - MapWith - 嵌套结构体指定属性映射
    /// </summary>
    [Fact]
    public void MapWith_NestedStructs_ShouldMapSpecifiedPropertiesOnly()
    {
        // Arrange
        var source = new NestedStructSource
        {
            Id = 1,
            Name = "Partial Test",
            Address = new AddressSource
            {
                Street = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country"
            },
            Contact = new ContactSource
            {
                Phone = "+1-555-0000",
                Email = "partial@test.com",
                IsPreferred = true
            },
            CreatedAt = new DateTime(2023, 12, 25),
            IsActive = false
        };

        // Act - 只映射 Id 和 Address
        var result = MapperHelper.MapWith<NestedStructSource, NestedStructDestination>(
            source, "Id", "Address");

        // Assert
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBeNull(); // 未指定，应为默认值
        result.CreatedAt.ShouldBe(default); // 未指定，应为默认值
        result.IsActive.ShouldBe(default); // 未指定，应为默认值

        // Address 应该被正确映射
        result.Address.Street.ShouldBe(source.Address.Street);
        result.Address.City.ShouldBe(source.Address.City);
        result.Address.PostalCode.ShouldBe(source.Address.PostalCode);
        result.Address.Country.ShouldBe(source.Address.Country);

        // Contact 应该为默认值
        result.Contact.Phone.ShouldBeNull();
        result.Contact.Email.ShouldBeNull();
        result.Contact.IsPreferred.ShouldBe(default);
    }

    /// <summary>
    /// 测试 - MapWithout - 嵌套结构体排除属性映射
    /// </summary>
    [Fact]
    public void MapWithout_NestedStructs_ShouldMapAllExceptExcluded()
    {
        // Arrange
        var source = new NestedStructSource
        {
            Id = 1,
            Name = "Exclude Test",
            Address = new AddressSource
            {
                Street = "456 Exclude St",
                City = "Exclude City",
                PostalCode = "54321",
                Country = "Exclude Country"
            },
            Contact = new ContactSource
            {
                Phone = "+1-555-1111",
                Email = "exclude@test.com",
                IsPreferred = false
            },
            CreatedAt = new DateTime(2023, 7, 4),
            IsActive = true
        };

        // Act - 排除 Contact 和 CreatedAt
        var result = MapperHelper.MapWithout<NestedStructSource, NestedStructDestination>(
            source, "Contact", "CreatedAt");

        // Assert
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.IsActive.ShouldBe(source.IsActive);

        // Address 应该被映射
        result.Address.Street.ShouldBe(source.Address.Street);
        result.Address.City.ShouldBe(source.Address.City);
        result.Address.PostalCode.ShouldBe(source.Address.PostalCode);
        result.Address.Country.ShouldBe(source.Address.Country);

        // Contact 和 CreatedAt 应该为默认值（被排除）
        result.Contact.Phone.ShouldBeNull();
        result.Contact.Email.ShouldBeNull();
        result.Contact.IsPreferred.ShouldBe(default);
        result.CreatedAt.ShouldBe(default);
    }

    #endregion

    #region 压力和性能测试

    /// <summary>
    /// 测试 - Map - 并发映射安全性
    /// </summary>
    [Fact]
    public void Map_ConcurrentMapping_ShouldBeThreadSafe()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "Concurrent Test",
            Email = "concurrent@test.com"
        };

        var results = new ConcurrentBag<DestinationModel>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var result = MapperHelper.Map<SourceModel, DestinationModel>(source);
                    results.Add(result);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        results.Count.ShouldBe(1000);
        results.All(r => r.Id == source.Id).ShouldBeTrue();
        results.All(r => r.Name == source.Name).ShouldBeTrue();
        results.All(r => r.Email == source.Email).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - Map - 大量属性的对象映射
    /// </summary>
    [Fact]
    public void Map_LargeNumberOfProperties_ShouldPerformWell()
    {
        // 这个测试验证映射器在处理大量属性时的性能
        // 由于当前测试模型属性数量有限，这里主要测试重复映射的性能
        var source = new ComplexTypeSource
        {
            Id = 1,
            Tags = new List<string> { "performance", "test" },
            Metadata = new Dictionary<string, object> { { "perf", true } },
            Scores = new decimal[] { 1.0m, 2.0m, 3.0m },
            CreatedAt = DateTime.Now,
            Duration = TimeSpan.FromMinutes(30),
            UniqueId = Guid.NewGuid(),
            Website = new Uri("https://performance.test")
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 10000; i++)
        {
            var result = MapperHelper.Map<ComplexTypeSource, ComplexTypeDestination>(source);
            result.ShouldNotBeNull();
        }

        stopwatch.Stop();

        // Assert
        Output.WriteLine($"映射10000次复杂对象耗时: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // 应在5秒内完成
    }

    /// <summary>
    /// 测试 - Map - 嵌套结构体性能测试
    /// </summary>
    [Fact]
    public void Map_NestedStructs_PerformanceTest()
    {
        // Arrange
        var source = new NestedStructSource
        {
            Id = 1,
            Name = "Performance Test",
            Address = new AddressSource
            {
                Street = "Performance St",
                City = "Performance City",
                PostalCode = "00000",
                Country = "Performance Country"
            },
            Contact = new ContactSource
            {
                Phone = "+1-555-PERF",
                Email = "performance@test.com",
                IsPreferred = true
            },
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - 执行大量嵌套结构体映射
        for (int i = 0; i < 10000; i++)
        {
            var result = MapperHelper.Map<NestedStructSource, NestedStructDestination>(source);
        }

        stopwatch.Stop();

        // Assert
        Output.WriteLine($"嵌套结构体映射10000次耗时: {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(3000); // 应在3秒内完成
    }

    #endregion

    #region 边界值和极端情况测试

    /// <summary>
    /// 测试 - Map - 极值边界测试
    /// </summary>
    [Fact]
    public void Map_BoundaryValues_ShouldMapCorrectly()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = int.MaxValue,
            Age = int.MinValue,
            Salary = decimal.MaxValue,
            BirthDate = DateTime.MaxValue,
            Name = string.Empty,
            Email = new string('x', 1000), // 很长的字符串
            IsActive = false
        };

        // Act
        var result = MapperHelper.Map<SourceModel, DestinationModel>(source);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(int.MaxValue);
        result.Age.ShouldBe(int.MinValue);
        result.Salary.ShouldBe(decimal.MaxValue);
        result.BirthDate.ShouldBe(DateTime.MaxValue);
        result.Name.ShouldBe(string.Empty);
        result.Email.Length.ShouldBe(1000);
        result.IsActive.ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - MapWith - 大量属性名称
    /// </summary>
    [Fact]
    public void MapWith_LargeNumberOfPropertyNames_ShouldHandleCorrectly()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "Test",
            Email = "test@example.com",
            Age = 30,
            Department = "IT"
        };

        var propertyNames = new string[1000];
        for (int i = 0; i < 1000; i++)
        {
            propertyNames[i] = i < 5 ? new[] { "Id", "Name", "Email", "Age", "Department" }[i] : $"NonExistent{i}";
        }

        // Act
        var result = MapperHelper.MapWith<SourceModel, DestinationModel>(source, propertyNames);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Email.ShouldBe(source.Email);
        result.Age.ShouldBe(source.Age);
        result.Department.ShouldBe(source.Department);
    }

    /// <summary>
    /// 测试 - MapWithout - 排除所有属性
    /// </summary>
    [Fact]
    public void MapWithout_ExcludeAllProperties_ShouldReturnDefaultValues()
    {
        // Arrange
        var source = new SourceModel
        {
            Id = 1,
            Name = "Test",
            Email = "test@example.com",
            Age = 30
        };

        // Act
        var result = MapperHelper.MapWithout<SourceModel, DestinationModel>(
            source, "Id", "Name", "Email", "Age", "BirthDate", "IsActive", "Salary", "Department");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(0); // 默认值
        result.Name.ShouldBeNull(); // 默认值
        result.Email.ShouldBeNull(); // 默认值
        result.Age.ShouldBe(0); // 默认值
    }

    #endregion
}
