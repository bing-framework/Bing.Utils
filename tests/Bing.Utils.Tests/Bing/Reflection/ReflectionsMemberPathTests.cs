namespace Bing.Reflection;

/// <summary>
/// 验证公共实例属性枚举与成员路径的读写行为。
/// </summary>
[Trait("ReflectionUT", "Reflections.MemberPath")]
public class ReflectionsMemberPathTests
{
    /// <summary>
    /// 验证公共实例属性枚举排除静态属性并返回独立快照。
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
    /// 验证嵌套属性路径能够读取叶子值。
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
    /// 验证成员路径写入应将可转换的字符串值转换为字段的目标类型。
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
    /// 验证中间成员为 null 时，成员路径访问应返回 false 而不创建隐式对象。
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
    /// 验证不存在的成员路径应被 HasMemberPath 识别为无效。
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
    /// 验证实例 HasMember 入口应识别有效路径，并将 null 实例视为不可访问。
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
    /// 验证公开 getter 和私有 setter 在默认选项下应可读不可写。
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
    /// 验证私有 getter 和公开 setter 在默认选项下应可写不可读。
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
    /// 验证启用 IncludeNonPublic 后应允许调用私有访问器和私有字段。
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
    /// 验证私有字段、readonly 字段和常量字段在默认模式下均不可设置。
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
    /// 验证空实例及空白路径读取失败。
    /// </summary>
    /// <param name="path">用于构造无效输入的空值、空字符串或空白路径。</param>
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
    /// 验证属性 getter 和 setter 自身抛出的异常不应被 Try API 吞掉。
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
    /// 验证成员路径默认应严格匹配大小写，显式启用选项后才忽略大小写。
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
    /// 验证禁用字段选项后，成员路径不应解析字段；禁用赋值转换后，不应隐式转换字符串值。
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
    /// 验证空白路径或不存在的路径写入失败。
    /// </summary>
    /// <param name="path">用于验证写入失败的成员路径。</param>
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
    /// 验证非 Try API 对 null 实例和空白路径应给出参数异常，对不可访问路径应给出状态异常。
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
    /// 验证索引器不应作为普通成员路径解析，叶子 null 值仍应被成功读取。
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

    /// <summary>
    /// 提供嵌套属性路径及静态属性的根对象样例。
    /// </summary>
    public sealed class PathRoot
    {
        /// <summary>
        /// 获取或设置应被实例属性枚举排除的静态测试文本。
        /// </summary>
        public static string StaticValue { get; set; }
        /// <summary>
        /// 获取或设置成员路径中的中间对象。
        /// </summary>
        public PathChild Child { get; set; }
    }

    /// <summary>
    /// 提供成员路径的叶子属性和字段样例。
    /// </summary>
    public sealed class PathChild
    {
        /// <summary>
        /// 获取或设置成员路径的叶子测试文本。
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// 保存用于验证字段赋值转换的测试整数。
        /// </summary>
        public int Count;
    }

    /// <summary>
    /// 提供不同访问器可见性及字段写入限制的测试样例。
    /// </summary>
    public sealed class VisibilityRoot
    {
        /// <summary>
        /// 保存用于验证非公共字段访问的测试整数。
        /// </summary>
        private int PrivateField;
        /// <summary>
        /// 用于验证只读字段写入限制的测试整数。
        /// </summary>
        public readonly int ReadOnlyField;
        /// <summary>
        /// 用于验证常量字段不可写入的测试值。
        /// </summary>
        public const int ConstantField = 1;
        /// <summary>
        /// 获取或设置具有公开读取和私有写入访问器的测试文本。
        /// </summary>
        public string PublicGetPrivateSet { get; private set; } = "initial";
        /// <summary>
        /// 获取或设置具有私有读取和公开写入访问器的测试文本。
        /// </summary>
        public string PrivateGetPublicSet { private get; set; } = "initial";
    }

    /// <summary>
    /// 提供访问时抛出异常的属性样例。
    /// </summary>
    public sealed class ThrowingRoot
    {
        /// <summary>
        /// 获取用于验证读取异常传播的测试文本。
        /// </summary>
        /// <remarks>读取时总是抛出 <see cref="InvalidOperationException"/>。</remarks>
        public string GetterThrows => throw new InvalidOperationException("getter");
        /// <summary>
        /// 设置用于验证写入异常传播的测试文本。
        /// </summary>
        /// <remarks>写入时总是抛出 <see cref="InvalidOperationException"/>。</remarks>
        public string SetterThrows { set => throw new InvalidOperationException("setter"); }
    }

    /// <summary>
    /// 提供应被普通成员路径排除的索引器样例。
    /// </summary>
    public sealed class IndexerRoot
    {
        /// <summary>
        /// 获取索引的字符串表示。
        /// </summary>
        /// <param name="index">用于生成测试文本的索引。</param>
        /// <returns>索引的字符串表示。</returns>
        public string this[int index] => index.ToString();
    }
}
