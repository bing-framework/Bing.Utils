namespace Bing.Reflection;

/// <summary>
/// 测试类：公共实例属性枚举和成员路径访问。
/// </summary>
[Trait("ReflectionUT", "Reflections.MemberPath")]
public class ReflectionsMemberPathTests
{
    /// <summary>
    /// 测试目的：公共实例属性枚举应排除静态属性，并返回可独立修改的快照。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithMixedProperties_ReturnsInstancePropertiesOnly()
    {
        // Act
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(PathRoot));

        // Assert
        properties.ShouldContain(property => property.Name == nameof(PathRoot.Child));
        properties.ShouldNotContain(property => property.Name == nameof(PathRoot.StaticValue));
        var mutableSnapshot = properties.ToArray();
        mutableSnapshot[0] = null!;
        TypeReflections.GetPublicInstanceProperties(typeof(PathRoot)).ShouldNotContain(property => property == null);
    }

    /// <summary>
    /// 测试目的：成员路径应支持属性读取并默认按大小写精确匹配。
    /// </summary>
    [Fact]
    public void TryGetMemberValueByPath_WithPropertyAndFieldPath_ReturnsValue()
    {
        // Arrange
        var root = new PathRoot { Child = new PathChild { Label = "nested" } };

        // Act
        var success = Reflections.TryGetMemberValueByPath(root, "Child.Label", out var value);

        // Assert
        success.ShouldBeTrue();
        value.ShouldBe("nested");
    }

    /// <summary>
    /// 测试目的：成员路径写入应将可转换的字符串值转换为字段的目标类型。
    /// </summary>
    [Fact]
    public void TrySetMemberValueByPath_WithConvertibleFieldValue_UpdatesField()
    {
        // Arrange
        var root = new PathRoot { Child = new PathChild() };

        // Act
        var success = Reflections.TrySetMemberValueByPath(root, "Child.Count", "12");

        // Assert
        success.ShouldBeTrue();
        root.Child.Count.ShouldBe(12);
    }

    /// <summary>
    /// 测试目的：中间成员为 null 时，成员路径访问应返回 false 而不创建隐式对象。
    /// </summary>
    [Fact]
    public void TrySetMemberValueByPath_WithNullIntermediate_ReturnsFalse()
    {
        // Arrange
        var root = new PathRoot();

        // Act
        var success = Reflections.TrySetMemberValueByPath(root, "Child.Count", 3);

        // Assert
        success.ShouldBeFalse();
        root.Child.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：不存在的成员路径应被 HasMemberPath 识别为无效。
    /// </summary>
    [Fact]
    public void HasMemberPath_WithUnknownSegment_ReturnsFalse()
    {
        // Act
        var result = Reflections.HasMemberPath(typeof(PathRoot), "Child.Unknown");

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：实例 HasMember 入口应识别有效路径，并将 null 实例视为不可访问。
    /// </summary>
    [Fact]
    public void HasMember_WithInstanceOrNull_ReturnsExpectedResult()
    {
        // Act
        var existing = Reflections.HasMember(new PathRoot(), nameof(PathRoot.Child));
        var nullInstance = Reflections.HasMember((object)null!, nameof(PathRoot.Child));

        // Assert
        existing.ShouldBeTrue();
        nullInstance.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：公开 getter 和私有 setter 在默认选项下应可读不可写。
    /// </summary>
    [Fact]
    public void TrySetMemberValueByPath_PrivateSetterDisabled_ReturnsFalse()
    {
        // Arrange
        var root = new VisibilityRoot();

        // Act
        var canRead = Reflections.TryGetMemberValue(root, nameof(VisibilityRoot.PublicGetPrivateSet), out var value);
        var canWrite = Reflections.TrySetMemberValue(root, nameof(VisibilityRoot.PublicGetPrivateSet), "changed");

        // Assert
        canRead.ShouldBeTrue();
        value.ShouldBe("initial");
        canWrite.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：私有 getter 和公开 setter 在默认选项下应可写不可读。
    /// </summary>
    [Fact]
    public void TryGetMemberValue_PrivateGetterDisabled_ReturnsFalse()
    {
        // Arrange
        var root = new VisibilityRoot();

        // Act
        var canRead = Reflections.TryGetMemberValue(root, nameof(VisibilityRoot.PrivateGetPublicSet), out _);
        var canWrite = Reflections.TrySetMemberValue(root, nameof(VisibilityRoot.PrivateGetPublicSet), "changed");

        // Assert
        canRead.ShouldBeFalse();
        canWrite.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：启用 IncludeNonPublic 后应允许调用私有访问器和私有字段。
    /// </summary>
    [Fact]
    public void TryMemberValue_WithIncludeNonPublic_AllowsPrivateAccessorsAndFields()
    {
        // Arrange
        var root = new VisibilityRoot();
        var options = new MemberPathOptions { IncludeNonPublic = true };

        // Act
        var setProperty = Reflections.TrySetMemberValue(root, nameof(VisibilityRoot.PublicGetPrivateSet), "changed", options);
        var getProperty = Reflections.TryGetMemberValue(root, nameof(VisibilityRoot.PrivateGetPublicSet), out var propertyValue, options);
        var setField = Reflections.TrySetMemberValue(root, "PrivateField", 8, options);
        var getField = Reflections.TryGetMemberValue(root, "PrivateField", out var fieldValue, options);

        // Assert
        setProperty.ShouldBeTrue();
        getProperty.ShouldBeTrue();
        propertyValue.ShouldBe("initial");
        setField.ShouldBeTrue();
        getField.ShouldBeTrue();
        fieldValue.ShouldBe(8);
    }

    /// <summary>
    /// 测试目的：私有字段、readonly 字段和常量字段在默认模式下均不可设置。
    /// </summary>
    [Fact]
    public void TrySetMemberValue_WithForbiddenFields_ReturnsFalse()
    {
        // Arrange
        var root = new VisibilityRoot();

        // Act
        var privateResult = Reflections.TrySetMemberValue(root, "PrivateField", 1);
        var readOnlyResult = Reflections.TrySetMemberValue(root, nameof(VisibilityRoot.ReadOnlyField), 1);
        var constResult = Reflections.TrySetMemberValue(root, nameof(VisibilityRoot.ConstantField), 1);

        // Assert
        privateResult.ShouldBeFalse();
        readOnlyResult.ShouldBeFalse();
        constResult.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：Try API 对 null 实例、null 路径、空路径和空白路径应返回 false。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGetMemberValue_WithInvalidPathOrInstance_ReturnsFalse(string path)
    {
        // Arrange
        var root = new PathRoot();

        // Act
        var result = Reflections.TryGetMemberValue(path == null ? null! : root, path!, out _);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：属性 getter 和 setter 自身抛出的异常不应被 Try API 吞掉。
    /// </summary>
    [Fact]
    public void TryMemberValue_WhenAccessorThrows_PropagatesException()
    {
        // Arrange
        var root = new ThrowingRoot();

        // Act & Assert
        var getterException = Should.Throw<TargetInvocationException>(() => Reflections.TryGetMemberValue(root, nameof(ThrowingRoot.GetterThrows), out _));
        var setterException = Should.Throw<TargetInvocationException>(() => Reflections.TrySetMemberValue(root, nameof(ThrowingRoot.SetterThrows), "value"));
        getterException.InnerException.ShouldBeOfType<InvalidOperationException>();
        setterException.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// 测试目的：成员路径默认应严格匹配大小写，显式启用选项后才忽略大小写。
    /// </summary>
    [Fact]
    public void TryGetMemberValue_WithCaseSensitivityOption_UsesConfiguredComparison()
    {
        // Arrange
        var root = new PathRoot { Child = new PathChild { Label = "value" } };

        // Act
        var defaultResult = Reflections.TryGetMemberValue(root, "child.label", out _);
        var ignoredCaseResult = Reflections.TryGetMemberValue(root, "child.label", out var value,
            new MemberPathOptions { IgnoreCase = true });

        // Assert
        defaultResult.ShouldBeFalse();
        ignoredCaseResult.ShouldBeTrue();
        value.ShouldBe("value");
    }

    /// <summary>
    /// 测试目的：禁用字段选项后，成员路径不应解析字段；禁用赋值转换后，不应隐式转换字符串值。
    /// </summary>
    [Fact]
    public void TrySetMemberValue_WithFieldsOrConversionDisabled_ReturnsFalse()
    {
        // Arrange
        var root = new PathRoot { Child = new PathChild() };

        // Act
        var fieldDisabled = Reflections.TryGetMemberValue(root, "Child.Count", out _,
            new MemberPathOptions { IncludeFields = false });
        var conversionDisabled = Reflections.TrySetMemberValue(root, "Child.Count", "12",
            new MemberPathOptions { ConvertAssignedValue = false });

        // Assert
        fieldDisabled.ShouldBeFalse();
        conversionDisabled.ShouldBeFalse();
        root.Child.Count.ShouldBe(0);
    }

    /// <summary>
    /// 测试目的：Try 设置 API 对 null、空白和不存在路径应返回 false。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Child.Unknown")]
    public void TrySetMemberValue_WithInvalidPath_ReturnsFalse(string path)
    {
        // Arrange
        var root = new PathRoot { Child = new PathChild() };

        // Act
        var result = Reflections.TrySetMemberValue(root, path!, 1);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：非 Try API 对 null 实例和空白路径应给出参数异常，对不可访问路径应给出状态异常。
    /// </summary>
    [Fact]
    public void MemberValue_NonTryApis_WithInvalidInput_ThrowsExpectedException()
    {
        // Arrange
        var root = new PathRoot();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => Reflections.GetMemberValue(null!, nameof(PathRoot.Child)));
        Should.Throw<ArgumentException>(() => Reflections.GetMemberValue(root, " "));
        Should.Throw<ArgumentNullException>(() => Reflections.SetMemberValue(null!, nameof(PathRoot.Child), new PathChild()));
        Should.Throw<ArgumentException>(() => Reflections.SetMemberValue(root, "", new PathChild()));
        Should.Throw<InvalidOperationException>(() => Reflections.GetMemberValue(root, "Missing"));
        Should.Throw<InvalidOperationException>(() => Reflections.SetMemberValue(root, "Missing", 1));
        Should.Throw<ArgumentNullException>(() => Reflections.HasMemberPath(null!, nameof(PathRoot.Child)));
    }

    /// <summary>
    /// 测试目的：索引器不应作为普通成员路径解析，叶子 null 值仍应被成功读取。
    /// </summary>
    [Fact]
    public void TryGetMemberValue_WithIndexerOrNullLeaf_ReturnsExpectedResult()
    {
        // Arrange
        var root = new PathRoot { Child = new PathChild { Label = null } };

        // Act
        var indexerResult = Reflections.TryGetMemberValue(new IndexerRoot(), "Item", out _);
        var nullLeafResult = Reflections.TryGetMemberValue(root, "Child.Label", out var value);

        // Assert
        indexerResult.ShouldBeFalse();
        nullLeafResult.ShouldBeTrue();
        value.ShouldBeNull();
    }

    public sealed class PathRoot
    {
        public static string StaticValue { get; set; }
        public PathChild Child { get; set; }
    }

    public sealed class PathChild
    {
        public string Label { get; set; }
        public int Count;
    }

    public sealed class VisibilityRoot
    {
        private int PrivateField;
        public readonly int ReadOnlyField;
        public const int ConstantField = 1;
        public string PublicGetPrivateSet { get; private set; } = "initial";
        public string PrivateGetPublicSet { private get; set; } = "initial";
    }

    public sealed class ThrowingRoot
    {
        public string GetterThrows => throw new InvalidOperationException("getter");
        public string SetterThrows { set => throw new InvalidOperationException("setter"); }
    }

    public sealed class IndexerRoot
    {
        public string this[int index] => index.ToString();
    }
}
