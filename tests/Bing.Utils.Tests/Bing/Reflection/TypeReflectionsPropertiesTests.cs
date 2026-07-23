namespace Bing.Reflection;

/// <summary>
/// 测试类：TypeReflections 公共实例属性枚举。
/// </summary>
[Trait("ReflectionUT", "TypeReflections.Properties")]
public class TypeReflectionsPropertiesTests
{
    /// <summary>
    /// 测试目的：继承类应包含基类公共实例属性，并排除索引器和非公共属性。
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
    /// 测试目的：接口继承链和菱形继承中的同签名属性应只保留一个。
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
    /// 测试目的：同名不同类型的接口属性应同时保留，避免静默丢失元数据。
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
    /// 测试目的：null 类型应抛出 ArgumentNullException，重复调用的顺序应稳定。
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
    /// 测试目的：泛型入口应与 Type 入口返回相同属性，且静态属性不应进入结果。
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
    /// 测试目的：可读和可写属性筛选应按访问器实际可见性分别处理。
    /// </summary>
    [Fact]
    public void GetPublicReadableAndWritableInstanceProperties_UsesAccessorVisibility()
    {
        // Act
        var readable = TypeReflections.GetPublicReadableInstanceProperties(typeof(AccessorProperties)).Select(property => property.Name);
        var writable = TypeReflections.GetPublicWritableInstanceProperties(typeof(AccessorProperties)).Select(property => property.Name);

        // Assert
        readable.ShouldBe(new[] { nameof(AccessorProperties.PrivateSet), nameof(AccessorProperties.ReadOnly) });
        writable.ShouldBe(new[] { nameof(AccessorProperties.PrivateGet), nameof(AccessorProperties.WriteOnly) });
    }

    /// <summary>
    /// 测试目的：单链接口继承应同时包含本接口和父接口声明的属性。
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
    /// 测试目的：调用方修改返回数组不应污染内部缓存或下一次查询结果。
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
    /// 测试目的：同名同类型但 getter/setter 形状不同的接口属性不应被错误合并。
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

    public class BaseProperties
    {
        public string Base { get; set; }
        private string Hidden { get; set; }
        public string this[int index] => index.ToString();
    }

    public sealed class DerivedProperties : BaseProperties
    {
        public string Derived { get; set; }
    }

    public sealed class StaticProperties
    {
        public static string Static { get; set; }
        public string Instance { get; set; }
    }

    public sealed class AccessorProperties
    {
        public string ReadOnly { get; } = "read";
        public string WriteOnly { private get; set; }
        public string PrivateGet { private get; set; }
        public string PrivateSet { get; private set; } = "read";
    }

    public interface IBaseInterface { string Shared { get; } }
    public interface ILeft : IBaseInterface { string Left { get; } }
    public interface IRight : IBaseInterface { string Right { get; } }
    public interface IDiamond : ILeft, IRight { string Own { get; } }
    public interface IStringValue { string Value { get; } }
    public interface IIntValue { int Value { get; } }
    public interface IConflictingProperties : IStringValue, IIntValue { }
    public interface IParentInterface { string Parent { get; } }
    public interface IChildInterface : IParentInterface { string Child { get; } }
    public interface IGetOnlyValue { string Value { get; } }
    public interface IGetSetValue { string Value { get; set; } }
    public interface IAccessorShapeConflict : IGetOnlyValue, IGetSetValue { }
}