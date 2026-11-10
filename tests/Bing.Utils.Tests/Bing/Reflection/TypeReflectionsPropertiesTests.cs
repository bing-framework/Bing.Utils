namespace Bing.Reflection;

/// <summary>
/// 验证公共实例属性的继承、去重、访问器筛选及缓存隔离。
/// </summary>
[Trait("ReflectionUT", "TypeReflections.Properties")]
public class TypeReflectionsPropertiesTests
{
    /// <summary>
    /// 验证继承类应包含基类公共实例属性，并排除索引器和非公共属性。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithDerivedType_ReturnsInheritedNonIndexerProperties()
    {
        // Act
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(DerivedProperties));

        // Assert
        properties.Select(property => property.Name).ShouldBe(new[] { nameof(BaseProperties.Base), nameof(DerivedProperties.Derived) });
    }

    /// <summary>
    /// 验证接口继承链和菱形继承中的同签名属性应只保留一个。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithDiamondInterface_DeduplicatesInheritedProperties()
    {
        // Act
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(IDiamond));

        // Assert
        properties.Count(property => property.Name == nameof(IBaseInterface.Shared)).ShouldBe(1);
        properties.Select(property => property.Name).ShouldContain(nameof(ILeft.Left));
        properties.Select(property => property.Name).ShouldContain(nameof(IRight.Right));
    }

    /// <summary>
    /// 验证同名不同类型的接口属性应同时保留，避免静默丢失元数据。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithSameNameDifferentTypes_KeepsBothProperties()
    {
        // Act
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(IConflictingProperties));

        // Assert
        properties.Count(property => property.Name == "Value").ShouldBe(2);
        properties.Select(property => property.PropertyType).ShouldContain(typeof(string));
        properties.Select(property => property.PropertyType).ShouldContain(typeof(int));
    }

    /// <summary>
    /// 验证null 类型应抛出 ArgumentNullException，重复调用的顺序应稳定。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithNullOrRepeatedCalls_ValidatesAndKeepsStableOrder()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => TypeReflections.GetPublicInstanceProperties(null!));
        var first = TypeReflections.GetPublicInstanceProperties(typeof(IDiamond)).Select(property => property.ToString()).ToArray();
        var second = TypeReflections.GetPublicInstanceProperties(typeof(IDiamond)).Select(property => property.ToString()).ToArray();
        second.ShouldBe(first);
    }

    /// <summary>
    /// 验证泛型入口应与 Type 入口返回相同属性，且静态属性不应进入结果。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_GenericEntry_ExcludesStaticProperties()
    {
        // Act
        var genericProperties = TypeReflections.GetPublicInstanceProperties<StaticProperties>();
        var typeProperties = TypeReflections.GetPublicInstanceProperties(typeof(StaticProperties));

        // Assert
        genericProperties.Select(property => property.Name).ShouldBe(typeProperties.Select(property => property.Name));
        genericProperties.Select(property => property.Name).ShouldBe(new[] { nameof(StaticProperties.Instance) });
    }

    /// <summary>
    /// 验证可读和可写属性筛选应按访问器实际可见性分别处理。
    /// </summary>
    [Fact]
    public void GetPublicReadableAndWritableInstanceProperties_UsesAccessorVisibility()
    {
        // Act
        var readable = TypeReflections.GetPublicReadableInstanceProperties(typeof(AccessorProperties)).Select(property => property.Name);
        var writable = TypeReflections.GetPublicWritableInstanceProperties(typeof(AccessorProperties)).Select(property => property.Name);

        // Assert
        readable.ShouldBe(new[]
        {
            nameof(AccessorProperties.InitOnly),
            nameof(AccessorProperties.PrivateSet),
            nameof(AccessorProperties.ReadOnly)
        });
        writable.ShouldBe(new[] { nameof(AccessorProperties.PrivateGet), nameof(AccessorProperties.WriteOnly) });
    }

    /// <summary>
    /// 验证单链接口继承应同时包含本接口和父接口声明的属性。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithSingleInheritedInterface_ReturnsOwnAndParentProperties()
    {
        // Act
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(IChildInterface));

        // Assert
        properties.Select(property => property.Name).ShouldBe(new[] { nameof(IChildInterface.Child), nameof(IParentInterface.Parent) });
    }

    /// <summary>
    /// 验证调用方修改返回数组不应污染内部缓存或下一次查询结果。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithMutatedReturnedArray_KeepsCachedSnapshotIntact()
    {
        // Arrange
        var first = TypeReflections.GetPublicInstanceProperties(typeof(StaticProperties));

        // Act
        ((PropertyInfo[])first)[0] = null!;
        var second = TypeReflections.GetPublicInstanceProperties(typeof(StaticProperties));

        // Assert
        second.Count.ShouldBe(1);
        second[0].Name.ShouldBe(nameof(StaticProperties.Instance));
    }

    /// <summary>
    /// 验证同名同类型但 getter/setter 形状不同的接口属性不应被错误合并。
    /// </summary>
    [Fact]
    public void GetPublicInstanceProperties_WithDifferentAccessorShapes_KeepsBothProperties()
    {
        // Act
        var properties = TypeReflections.GetPublicInstanceProperties(typeof(IAccessorShapeConflict));

        // Assert
        properties.Count(property => property.Name == nameof(IGetOnlyValue.Value)).ShouldBe(2);
        properties.Count(property => property.SetMethod != null).ShouldBe(1);
        properties.Count(property => property.GetMethod != null).ShouldBe(2);
    }

    /// <summary>
    /// 验证接口的 init 与 set 契约不会合并，可写结果不受声明类型名称排序影响。
    /// </summary>
    /// <param name="type">要枚举属性的接口类型。</param>
    /// <param name="writableDeclaringType">预期保留的可写属性声明类型。</param>
    [Theory]
    [InlineData(typeof(IZSetAfterInit), typeof(IZSetAfterInit))]
    [InlineData(typeof(IASetBeforeInit), typeof(IASetBeforeInit))]
    [InlineData(typeof(IInitAndSetSiblings), typeof(IZSetValue))]
    public void GetPublicWritableInstanceProperties_WithInterfaceInitAndSet_KeepsWritableContract(
        Type type, Type writableDeclaringType)
    {
        var properties = TypeReflections.GetPublicInstanceProperties(type);
        var writable = TypeReflections.GetPublicWritableInstanceProperties(type);

        properties.Count(property => property.Name == "Value").ShouldBe(2);
        writable.Count.ShouldBe(1);
        writable[0].DeclaringType.ShouldBe(writableDeclaringType);
        TypeReflections.GetPublicWritableInstanceProperties(type)[0].ShouldBe(writable[0]);
    }

    /// <summary>
    /// 验证接口属性保留不同访问器的公开契约。
    /// </summary>
    /// <param name="type">要枚举属性的接口类型。</param>
    /// <param name="publicDeclaringType">公共访问器所属的声明类型。</param>
    /// <param name="restrictedDeclaringType">受限访问器所属的声明类型。</param>
    /// <param name="privateGetter">为 true 时验证私有读取访问器，否则验证私有写入访问器。</param>
    [Theory]
    [InlineData(typeof(IGetterVisibilityPrivateFirst), typeof(IZPublicAccessors), typeof(IAPrivateGetter), true)]
    [InlineData(typeof(IGetterVisibilityPublicFirst), typeof(IAPublicAccessors), typeof(IZPrivateGetter), true)]
    [InlineData(typeof(ISetterVisibilityPrivateFirst), typeof(IZPublicAccessors), typeof(IAPrivateSetter), false)]
    [InlineData(typeof(ISetterVisibilityPublicFirst), typeof(IAPublicAccessors), typeof(IZPrivateSetter), false)]
    [InlineData(typeof(IZPublicAfterPrivateGetter), typeof(IZPublicAfterPrivateGetter), typeof(IAPrivateGetter), true)]
    [InlineData(typeof(IAPublicBeforePrivateGetter), typeof(IAPublicBeforePrivateGetter), typeof(IZPrivateGetter), true)]
    [InlineData(typeof(IZPublicAfterPrivateSetter), typeof(IZPublicAfterPrivateSetter), typeof(IAPrivateSetter), false)]
    [InlineData(typeof(IAPublicBeforePrivateSetter), typeof(IAPublicBeforePrivateSetter), typeof(IZPrivateSetter), false)]
    public void GetPublicInstanceProperties_WithInterfaceAccessorVisibility_KeepsPublicContracts(
        Type type, Type publicDeclaringType, Type restrictedDeclaringType, bool privateGetter)
    {
        var properties = TypeReflections.GetPublicInstanceProperties(type);
        var readable = TypeReflections.GetPublicReadableInstanceProperties(type);
        var writable = TypeReflections.GetPublicWritableInstanceProperties(type);
        var restrictedAccess = privateGetter ? readable : writable;
        var sharedAccess = privateGetter ? writable : readable;

        // 公共访问能力由契约决定，不应被字母排序靠前的私有访问器覆盖。
        restrictedAccess.Count.ShouldBe(1);
        restrictedAccess[0].DeclaringType.ShouldBe(publicDeclaringType);
        sharedAccess.Select(property => property.DeclaringType)
            .ShouldBe(new[] { publicDeclaringType, restrictedDeclaringType }.OrderBy(item => item.FullName, StringComparer.Ordinal));
        properties.Count.ShouldBe(2);
        properties.ShouldAllBe(property => property.Name == "Value" && property.PropertyType == typeof(int));

        TypeReflections.GetPublicInstanceProperties(type).ShouldBe(properties);
        TypeReflections.GetPublicReadableInstanceProperties(type).ShouldBe(readable);
        TypeReflections.GetPublicWritableInstanceProperties(type).ShouldBe(writable);
    }

    /// <summary>
    /// 提供继承属性、私有属性及索引器的基类样例。
    /// </summary>
    public class BaseProperties
    {
        /// <summary>
        /// 获取或设置用于继承属性发现的基类测试文本。
        /// </summary>
        public string Base { get; set; }
        /// <summary>
        /// 获取或设置应被公共属性枚举排除的私有测试文本。
        /// </summary>
        private string Hidden { get; set; }
        /// <summary>
        /// 获取索引的字符串表示。
        /// </summary>
        /// <param name="index">用于生成测试文本的索引。</param>
        /// <returns>索引的字符串表示。</returns>
        public string this[int index] => index.ToString();
    }

    /// <summary>
    /// 提供基类属性继承及自身属性发现的派生类型。
    /// </summary>
    public sealed class DerivedProperties : BaseProperties
    {
        /// <summary>
        /// 获取或设置派生类自身声明的测试文本。
        /// </summary>
        public string Derived { get; set; }
    }

    /// <summary>
    /// 提供静态与实例属性的筛选样例。
    /// </summary>
    public sealed class StaticProperties
    {
        /// <summary>
        /// 获取或设置应被实例属性枚举排除的静态测试文本。
        /// </summary>
        public static string Static { get; set; }
        /// <summary>
        /// 获取或设置应被实例属性枚举保留的测试文本。
        /// </summary>
        public string Instance { get; set; }
    }

    /// <summary>
    /// 提供不同访问器可见性的属性筛选样例。
    /// </summary>
    public sealed class AccessorProperties
    {
        /// <summary>
        /// 获取或初始化用于验证 init-only setter 筛选的测试整数。
        /// </summary>
        public int InitOnly { get; init; }
        /// <summary>
        /// 获取只读测试文本。
        /// </summary>
        public string ReadOnly { get; } = "read";
        /// <summary>
        /// 获取或设置具有私有读取和公开写入访问器的测试文本。
        /// </summary>
        public string WriteOnly { private get; set; }
        /// <summary>
        /// 获取或设置具有私有读取和公开写入访问器的测试文本。
        /// </summary>
        public string PrivateGet { private get; set; }
        /// <summary>
        /// 获取或设置具有公开读取和私有写入访问器的测试文本。
        /// </summary>
        public string PrivateSet { get; private set; } = "read";
    }

    /// <summary>
    /// 声明菱形接口继承中共享属性的基础契约。
    /// </summary>
    public interface IBaseInterface
    {
        /// <summary>
        /// 获取由两个继承分支共享的测试文本。
        /// </summary>
        string Shared { get; }
    }
    /// <summary>
    /// 声明菱形接口继承左侧分支的属性契约。
    /// </summary>
    public interface ILeft : IBaseInterface
    {
        /// <summary>
        /// 获取左侧分支的测试文本。
        /// </summary>
        string Left { get; }
    }
    /// <summary>
    /// 声明菱形接口继承右侧分支的属性契约。
    /// </summary>
    public interface IRight : IBaseInterface
    {
        /// <summary>
        /// 获取右侧分支的测试文本。
        /// </summary>
        string Right { get; }
    }
    /// <summary>
    /// 用于验证菱形接口属性去重的组合契约。
    /// </summary>
    public interface IDiamond : ILeft, IRight
    {
        /// <summary>
        /// 获取组合接口自身声明的测试文本。
        /// </summary>
        string Own { get; }
    }
    /// <summary>
    /// 声明字符串类型取值属性的测试契约。
    /// </summary>
    public interface IStringValue
    {
        /// <summary>
        /// 获取用于验证属性类型差异的测试值。
        /// </summary>
        string Value { get; }
    }
    /// <summary>
    /// 声明整数类型取值属性的测试契约。
    /// </summary>
    public interface IIntValue
    {
        /// <summary>
        /// 获取用于验证属性类型差异的测试值。
        /// </summary>
        int Value { get; }
    }
    /// <summary>
    /// 用于验证同名属性类型冲突的组合契约。
    /// </summary>
    public interface IConflictingProperties : IStringValue, IIntValue { }
    /// <summary>
    /// 声明单链继承中父接口属性的测试契约。
    /// </summary>
    public interface IParentInterface
    {
        /// <summary>
        /// 获取父接口声明的测试文本。
        /// </summary>
        string Parent { get; }
    }
    /// <summary>
    /// 用于验证父接口属性发现的子契约。
    /// </summary>
    public interface IChildInterface : IParentInterface
    {
        /// <summary>
        /// 获取子接口自身声明的测试文本。
        /// </summary>
        string Child { get; }
    }
    /// <summary>
    /// 声明只读取值属性的访问器测试契约。
    /// </summary>
    public interface IGetOnlyValue
    {
        /// <summary>
        /// 获取用于验证访问器差异的测试文本。
        /// </summary>
        string Value { get; }
    }
    /// <summary>
    /// 声明读写取值属性的访问器测试契约。
    /// </summary>
    public interface IGetSetValue
    {
        /// <summary>
        /// 获取或设置用于验证访问器差异的测试文本。
        /// </summary>
        string Value { get; set; }
    }
    /// <summary>
    /// 用于验证同名属性访问器差异的组合契约。
    /// </summary>
    public interface IAccessorShapeConflict : IGetOnlyValue, IGetSetValue { }

    /// <summary>
    /// 名称排序在派生可写接口之前的初始化契约。
    /// </summary>
    public interface IAInitValue
    {
        /// <summary>
        /// 获取或初始化测试值。
        /// </summary>
        int Value { get; init; }
    }

    /// <summary>
    /// 用普通 setter 隐藏基接口 init 属性。
    /// </summary>
    public interface IZSetAfterInit : IAInitValue
    {
        /// <summary>
        /// 获取或设置隐藏基接口初始化属性的测试值。
        /// </summary>
        new int Value { get; set; }
    }

    /// <summary>
    /// 名称排序在派生可写接口之后的初始化契约。
    /// </summary>
    public interface IZInitValue
    {
        /// <summary>
        /// 获取或初始化测试值。
        /// </summary>
        int Value { get; init; }
    }

    /// <summary>
    /// 交换名称排序后仍应保留可写属性。
    /// </summary>
    public interface IASetBeforeInit : IZInitValue
    {
        /// <summary>
        /// 获取或设置隐藏基接口初始化属性的测试值。
        /// </summary>
        new int Value { get; set; }
    }

    /// <summary>
    /// 与初始化接口并列的可写契约。
    /// </summary>
    public interface IZSetValue
    {
        /// <summary>
        /// 获取或设置测试值。
        /// </summary>
        int Value { get; set; }
    }

    /// <summary>
    /// 合并同名同类型但初始化语义不同的接口。
    /// </summary>
    public interface IInitAndSetSiblings : IAInitValue, IZSetValue { }

    /// <summary>
    /// 名称排序靠前且具有私有 getter 的默认接口属性。
    /// </summary>
    public interface IAPrivateGetter
    {
        /// <summary>
        /// 获取或设置具有私有读取访问器的测试值。
        /// </summary>
        int Value { private get => 0; set { } }
    }

    /// <summary>
    /// 名称排序靠后且具有私有 getter 的默认接口属性。
    /// </summary>
    public interface IZPrivateGetter
    {
        /// <summary>
        /// 获取或设置具有私有读取访问器的测试值。
        /// </summary>
        int Value { private get => 0; set { } }
    }

    /// <summary>
    /// 名称排序靠前且具有私有 setter 的默认接口属性。
    /// </summary>
    public interface IAPrivateSetter
    {
        /// <summary>
        /// 获取或设置具有私有写入访问器的测试值。
        /// </summary>
        int Value { get => 0; private set { } }
    }

    /// <summary>
    /// 名称排序靠后且具有私有 setter 的默认接口属性。
    /// </summary>
    public interface IZPrivateSetter
    {
        /// <summary>
        /// 获取或设置具有私有写入访问器的测试值。
        /// </summary>
        int Value { get => 0; private set { } }
    }

    /// <summary>
    /// 名称排序靠前的公共读写属性契约。
    /// </summary>
    public interface IAPublicAccessors
    {
        /// <summary>
        /// 获取或设置测试值。
        /// </summary>
        int Value { get; set; }
    }

    /// <summary>
    /// 名称排序靠后的公共读写属性契约。
    /// </summary>
    public interface IZPublicAccessors
    {
        /// <summary>
        /// 获取或设置测试值。
        /// </summary>
        int Value { get; set; }
    }

    /// <summary>
    /// 并列继承时先遇到私有 getter 的组合契约。
    /// </summary>
    public interface IGetterVisibilityPrivateFirst : IAPrivateGetter, IZPublicAccessors { }

    /// <summary>
    /// 并列继承时先遇到公共 getter 的组合契约。
    /// </summary>
    public interface IGetterVisibilityPublicFirst : IAPublicAccessors, IZPrivateGetter { }

    /// <summary>
    /// 并列继承时先遇到私有 setter 的组合契约。
    /// </summary>
    public interface ISetterVisibilityPrivateFirst : IAPrivateSetter, IZPublicAccessors { }

    /// <summary>
    /// 并列继承时先遇到公共 setter 的组合契约。
    /// </summary>
    public interface ISetterVisibilityPublicFirst : IAPublicAccessors, IZPrivateSetter { }

    /// <summary>
    /// 公共读写声明隐藏排序靠前的私有 getter 属性。
    /// </summary>
    public interface IZPublicAfterPrivateGetter : IAPrivateGetter
    {
        /// <summary>
        /// 获取或设置具有公共访问器的测试值。
        /// </summary>
        new int Value { get; set; }
    }

    /// <summary>
    /// 公共读写声明隐藏排序靠后的私有 getter 属性。
    /// </summary>
    public interface IAPublicBeforePrivateGetter : IZPrivateGetter
    {
        /// <summary>
        /// 获取或设置具有公共访问器的测试值。
        /// </summary>
        new int Value { get; set; }
    }

    /// <summary>
    /// 公共读写声明隐藏排序靠前的私有 setter 属性。
    /// </summary>
    public interface IZPublicAfterPrivateSetter : IAPrivateSetter
    {
        /// <summary>
        /// 获取或设置具有公共访问器的测试值。
        /// </summary>
        new int Value { get; set; }
    }

    /// <summary>
    /// 公共读写声明隐藏排序靠后的私有 setter 属性。
    /// </summary>
    public interface IAPublicBeforePrivateSetter : IZPrivateSetter
    {
        /// <summary>
        /// 获取或设置具有公共访问器的测试值。
        /// </summary>
        new int Value { get; set; }
    }
}
