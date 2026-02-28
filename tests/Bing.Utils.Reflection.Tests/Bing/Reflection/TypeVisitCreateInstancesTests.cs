using Bing.Reflection.Tests;
using Shouldly;

namespace Bing.Reflection;

/// <summary>
/// 测试类：TypeVisit.CreateInstance 系列动态实例创建方法
/// </summary>
[Trait("ReflectionUT", "TypeVisit.CreateInstances")]
public class TypeVisitCreateInstancesTests
{
    #region CreateInstance(Type, params object[])

    /// <summary>
    /// 测试目的：对有无参公开构造函数的类型调用 CreateInstance，应成功返回实例
    /// </summary>
    [Fact]
    public void CreateInstance_WithParameterlessType_ReturnsNonNullInstance()
    {
        // Act
        var result = TypeVisit.CreateInstance(typeof(SampleEntity));

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<SampleEntity>();
    }

    /// <summary>
    /// 测试目的：提供与构造函数匹配的参数，CreateInstance 应使用对应的有参构造函数创建实例
    /// </summary>
    [Fact]
    public void CreateInstance_WithMatchingArgs_UsesCorrectConstructor()
    {
        // Act
        var result = TypeVisit.CreateInstance(typeof(SampleEntity), "TestName");

        // Assert
        result.ShouldNotBeNull();
        var entity = result.ShouldBeOfType<SampleEntity>();
        entity.ReadWriteProp.ShouldBe("TestName");
    }

    /// <summary>
    /// 测试目的：提供多个匹配参数，CreateInstance 应使用对应的多参构造函数创建实例
    /// </summary>
    [Fact]
    public void CreateInstance_WithMultipleMatchingArgs_UsesMultiParamConstructor()
    {
        // Act
        var result = TypeVisit.CreateInstance(typeof(SampleEntity), "Alice", 99);

        // Assert
        result.ShouldNotBeNull();
        var entity = result.ShouldBeOfType<SampleEntity>();
        entity.ReadWriteProp.ShouldBe("Alice");
        entity.ReadOnlyProp.ShouldBe(99);
    }

    /// <summary>
    /// 测试目的：对 null type 调用 CreateInstance，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void CreateInstance_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((Type)null!));
    }

    /// <summary>
    /// 测试目的：参数类型与所有构造函数不匹配时，CreateInstance 静默返回 null（非抛异常）
    /// </summary>
    [Fact]
    public void CreateInstance_WithNonMatchingArgs_ReturnsNull()
    {
        // Arrange — SampleEntity 没有 (int, int) 构造函数
        // Act
        var result = TypeVisit.CreateInstance(typeof(SampleEntity), 123, 456);

        // Assert — 静默失败，返回 null
        result.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：对无公开构造函数的类型调用 CreateInstance，应返回 null（无法找到构造函数）
    /// </summary>
    [Fact]
    public void CreateInstance_WithNoPublicCtor_ReturnsNull()
    {
        // Act
        var result = TypeVisit.CreateInstance(typeof(NoPublicCtorEntity));

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region CreateInstance<TInstance>(params object[])

    /// <summary>
    /// 测试目的：泛型 CreateInstance 无参版本应创建正确类型实例
    /// </summary>
    [Fact]
    public void CreateInstance_Generic_WithNoArgs_ReturnsTypedInstance()
    {
        // Act
        var result = TypeVisit.CreateInstance<SampleEntity>();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<SampleEntity>();
    }

    /// <summary>
    /// 测试目的：泛型 CreateInstance 带参版本应使用匹配构造函数
    /// </summary>
    [Fact]
    public void CreateInstance_Generic_WithMatchingArg_UsesParamConstructor()
    {
        // Act
        var result = TypeVisit.CreateInstance<SampleEntity>("GenericName");

        // Assert
        result.ShouldNotBeNull();
        result.ReadWriteProp.ShouldBe("GenericName");
    }

    /// <summary>
    /// 测试目的：泛型 CreateInstance 参数不匹配时，应返回类型默认值（引用类型为 null）
    /// </summary>
    [Fact]
    public void CreateInstance_Generic_WithNonMatchingArgs_ReturnsDefault()
    {
        // Arrange — SampleEntity 没有 (int, int) 构造函数
        // Act
        var result = TypeVisit.CreateInstance<SampleEntity>(100, 200);

        // Assert — 默认值，引用类型为 null
        result.ShouldBeNull();
    }

    #endregion

    #region CreateInstance<TInstance>(Type, params object[])

    /// <summary>
    /// 测试目的：显式指定类型的泛型 CreateInstance 应返回转换后的实例
    /// </summary>
    [Fact]
    public void CreateInstance_GenericWithType_ReturnsTypedInstance()
    {
        // Act
        var result = TypeVisit.CreateInstance<SampleEntity>(typeof(SampleEntity), "TypedName", 7);

        // Assert
        result.ShouldNotBeNull();
        result.ReadWriteProp.ShouldBe("TypedName");
        result.ReadOnlyProp.ShouldBe(7);
    }

    /// <summary>
    /// 测试目的：显式指定 null type 的泛型 CreateInstance，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void CreateInstance_GenericWithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.CreateInstance<SampleEntity>((Type)null!));
    }

    #endregion
}
