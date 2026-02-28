using System.Linq.Expressions;
using System.Reflection;
using Bing.Reflection.Tests;
using Shouldly;

namespace Bing.Reflection;

/// <summary>
/// 测试类：TypeVisit 字段、属性、方法、构造函数的成员访问方法
/// </summary>
[Trait("ReflectionUT", "TypeVisit.Members")]
public class TypeVisitMembersTests
{
    #region GetFields(Type)

    /// <summary>
    /// 测试目的：对含公开字段的类型调用 GetFields，应返回该公开字段
    /// </summary>
    [Fact]
    public void GetFields_WithTypeHavingPublicField_ReturnsPublicField()
    {
        // Act
        var fields = TypeVisit.GetFields(typeof(SampleEntity)).ToList();

        // Assert
        fields.ShouldNotBeEmpty();
        fields.ShouldContain(f => f.Name == nameof(SampleEntity.PublicField));
    }

    /// <summary>
    /// 测试目的：对 null 类型调用 GetFields，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetFields_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => TypeVisit.GetFields(null!).ToList());
    }

    #endregion

    #region GetField<T, TField>(Expression)

    /// <summary>
    /// 测试目的：使用字段表达式调用 GetField，应返回正确的 FieldInfo
    /// </summary>
    [Fact]
    public void GetField_WithFieldExpression_ReturnsCorrectFieldInfo()
    {
        // Act
        var field = TypeVisit.GetField<SampleEntity, string>(x => x.PublicField);

        // Assert
        field.ShouldNotBeNull();
        field.Name.ShouldBe(nameof(SampleEntity.PublicField));
        field.FieldType.ShouldBe(typeof(string));
    }

    /// <summary>
    /// 测试目的：使用属性表达式（非字段）调用 GetField，应抛出 ArgumentException
    /// </summary>
    [Fact]
    public void GetField_WithPropertyExpression_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetField<SampleEntity, string>(x => x.ReadWriteProp));
    }

    /// <summary>
    /// 测试目的：对 null 表达式调用 GetField，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetField_WithNullSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetField<SampleEntity, string>(null!));
    }

    #endregion

    #region GetProperties(Type, PropertyAccessOptions)

    /// <summary>
    /// 测试目的：用 Both 选项获取属性，只返回同时有 getter 和 setter 的属性
    /// </summary>
    [Fact]
    public void GetProperties_WithBothAccessOptions_ReturnsOnlyReadWriteProperties()
    {
        // Act
        var props = TypeVisit.GetProperties(typeof(SampleEntity), PropertyAccessOptions.Both).ToList();

        // Assert
        props.ShouldContain(p => p.Name == nameof(SampleEntity.ReadWriteProp));
        // ReadOnlyProp 没有 setter，不应包含
        props.ShouldNotContain(p => p.Name == nameof(SampleEntity.ReadOnlyProp));
    }

    /// <summary>
    /// 测试目的：用 Getters 选项获取属性，应返回所有有公开 getter 的属性
    /// </summary>
    [Fact]
    public void GetProperties_WithGettersOption_ReturnsPropertiesWithGetter()
    {
        // Act
        var props = TypeVisit.GetProperties(typeof(SampleEntity), PropertyAccessOptions.Getters).ToList();

        // Assert
        props.ShouldContain(p => p.Name == nameof(SampleEntity.ReadWriteProp));
        props.ShouldContain(p => p.Name == nameof(SampleEntity.ReadOnlyProp));
        props.ShouldContain(p => p.Name == nameof(SampleEntity.ExpressionProp));
    }

    /// <summary>
    /// 测试目的：用 Setters 选项获取属性，只返回有公开 setter 的属性
    /// </summary>
    [Fact]
    public void GetProperties_WithSettersOption_ReturnsOnlyWritableProperties()
    {
        // Act
        var props = TypeVisit.GetProperties(typeof(SampleEntity), PropertyAccessOptions.Setters).ToList();

        // Assert
        props.ShouldContain(p => p.Name == nameof(SampleEntity.ReadWriteProp));
        props.ShouldNotContain(p => p.Name == nameof(SampleEntity.ReadOnlyProp));
    }

    /// <summary>
    /// 测试目的：对 null 类型调用 GetProperties，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetProperties_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetProperties(null!, PropertyAccessOptions.Both).ToList());
    }

    #endregion

    #region GetProperty<T, TProperty>(Expression, AccessOptions)

    /// <summary>
    /// 测试目的：使用属性表达式调用 GetProperty，应返回正确的 PropertyInfo
    /// </summary>
    [Fact]
    public void GetProperty_WithPropertyExpression_ReturnsCorrectPropertyInfo()
    {
        // Act
        var prop = TypeVisit.GetProperty<SampleEntity, string>(
            x => x.ReadWriteProp, PropertyAccessOptions.Both);

        // Assert
        prop.ShouldNotBeNull();
        prop.Name.ShouldBe(nameof(SampleEntity.ReadWriteProp));
    }

    /// <summary>
    /// 测试目的：对只读属性使用 Setters 访问选项，应抛出 ArgumentException
    /// </summary>
    [Fact]
    public void GetProperty_ReadOnlyPropertyWithSettersOption_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetProperty<SampleEntity, int>(
                x => x.ReadOnlyProp, PropertyAccessOptions.Setters));
    }

    /// <summary>
    /// 测试目的：使用字段表达式（非属性）调用 GetProperty，应抛出 ArgumentException
    /// </summary>
    [Fact]
    public void GetProperty_WithFieldExpression_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            TypeVisit.GetProperty<SampleEntity, object>(
                x => (object)x.PublicField, PropertyAccessOptions.Both));
    }

    /// <summary>
    /// 测试目的：对 null 表达式调用 GetProperty，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetProperty_WithNullSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetProperty<SampleEntity, string>(null!, PropertyAccessOptions.Both));
    }

    #endregion

    #region Constructor

    /// <summary>
    /// 测试目的：对有无参构造函数的类调用 HasParameterlessConstructor，应返回 true
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WithDefaultCtor_ReturnsTrue()
    {
        // Act
        var result = TypeVisit.HasParameterlessConstructor(typeof(SampleEntity));

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：对无公开无参构造函数的类调用 HasParameterlessConstructor，应返回 false
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_WithNoPublicCtor_ReturnsFalse()
    {
        // Act
        var result = TypeVisit.HasParameterlessConstructor(typeof(NoPublicCtorEntity));

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：HasParameterlessConstructor 的扩展方法与静态方法结果一致
    /// </summary>
    [Fact]
    public void HasParameterlessConstructor_ExtensionMethod_MatchesStaticMethod()
    {
        // Act
        var staticResult = TypeVisit.HasParameterlessConstructor(typeof(SampleEntity));
        var extensionResult = typeof(SampleEntity).HasParameterlessConstructor();

        // Assert
        extensionResult.ShouldBe(staticResult);
    }

    /// <summary>
    /// 测试目的：GetParameterlessConstructor 对有无参构造的类返回 ConstructorInfo，对无无参构造的类返回 null
    /// </summary>
    [Theory]
    [InlineData(typeof(SampleEntity), true)]
    [InlineData(typeof(NoPublicCtorEntity), false)]
    public void GetParameterlessConstructor_ReturnsExpectedResult(Type type, bool expectNonNull)
    {
        // Act
        var ctor = TypeVisit.GetParameterlessConstructor(type);

        // Assert
        if (expectNonNull)
            ctor.ShouldNotBeNull();
        else
            ctor.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：GetMatchingConstructor 以匹配参数类型返回构造函数，不匹配时返回 null
    /// </summary>
    [Fact]
    public void GetMatchingConstructor_WithMatchingTypes_ReturnsConstructorInfo()
    {
        // Act
        var ctor = TypeVisit.GetMatchingConstructor(typeof(SampleEntity), new[] { typeof(string) });

        // Assert
        ctor.ShouldNotBeNull();
        ctor.GetParameters().Length.ShouldBe(1);
    }

    /// <summary>
    /// 测试目的：GetMatchingConstructor 参数类型不匹配时应返回 null
    /// </summary>
    [Fact]
    public void GetMatchingConstructor_WithNonMatchingTypes_ReturnsNull()
    {
        // Act — 无 (int, int) 构造函数
        var ctor = TypeVisit.GetMatchingConstructor(typeof(SampleEntity), new[] { typeof(int), typeof(int) });

        // Assert
        ctor.ShouldBeNull();
    }

    /// <summary>
    /// 测试目的：GetMatchingConstructor 传空数组时等同于查找无参构造函数
    /// </summary>
    [Fact]
    public void GetMatchingConstructor_WithEmptyTypes_ReturnsParameterlessCtor()
    {
        // Act
        var ctor = TypeVisit.GetMatchingConstructor(typeof(SampleEntity), Array.Empty<Type>());

        // Assert
        ctor.ShouldNotBeNull();
        ctor.GetParameters().Length.ShouldBe(0);
    }

    #endregion

    #region GetMethodBySignature

    /// <summary>
    /// 测试目的：GetMethodBySignature 能在目标类型中找到与源方法签名匹配的方法
    /// </summary>
    [Fact]
    public void GetMethodBySignature_WithMatchingMethod_ReturnsMethodInfo()
    {
        // Arrange
        var sourceMethod = typeof(SampleEntity).GetMethod(nameof(SampleEntity.Greet), Type.EmptyTypes)!;

        // Act
        var result = TypeVisit.GetMethodBySignature(typeof(SampleEntity), sourceMethod);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(nameof(SampleEntity.Greet));
    }

    /// <summary>
    /// 测试目的：GetMethodBySignature 对 null type 参数应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetMethodBySignature_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var method = typeof(SampleEntity).GetMethod(nameof(SampleEntity.Greet), Type.EmptyTypes)!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetMethodBySignature(null!, method));
    }

    /// <summary>
    /// 测试目的：GetMethodBySignature 对 null method 参数应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetMethodBySignature_WithNullMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.GetMethodBySignature(typeof(SampleEntity), null!));
    }

    #endregion

    #region IsVisibleAndVirtual

    /// <summary>
    /// 测试目的：虚属性调用 IsVisibleAndVirtual 应返回 true
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_WithVirtualProperty_ReturnsTrue()
    {
        // Arrange
        var prop = typeof(VirtualPropEntity).GetProperty(nameof(VirtualPropEntity.VirtualProp))!;

        // Act
        var result = TypeVisit.IsVisibleAndVirtual(prop);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：非虚属性调用 IsVisibleAndVirtual 应返回 false
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_WithNonVirtualProperty_ReturnsFalse()
    {
        // Arrange
        var prop = typeof(VirtualPropEntity).GetProperty(nameof(VirtualPropEntity.NonVirtualProp))!;

        // Act
        var result = TypeVisit.IsVisibleAndVirtual(prop);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：对 null 属性调用 IsVisibleAndVirtual 应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_WithNullProperty_ThrowsArgumentNullException()
    {
        // Act & Assert — 显式转型 PropertyInfo 消除与 MethodInfo 重载的二义性
        Should.Throw<ArgumentNullException>(() =>
            TypeVisit.IsVisibleAndVirtual((System.Reflection.PropertyInfo)null!));
    }

    #endregion
}
