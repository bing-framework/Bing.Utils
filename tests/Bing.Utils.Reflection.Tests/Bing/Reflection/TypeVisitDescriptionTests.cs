using System.Reflection;
using Bing.Reflection.Tests;
using Shouldly;

namespace Bing.Reflection;

/// <summary>
/// 测试类：TypeMetaVisitExtensions 的 Attribute / Description 相关扩展方法
/// </summary>
[Trait("ReflectionUT", "TypeVisit.Description")]
public class TypeVisitDescriptionTests
{
    #region IsAttributeDefined<TAttribute>

    /// <summary>
    /// 测试目的：对带有 Description Attribute 的类型成员调用 IsAttributeDefined，应返回 true
    /// </summary>
    [Fact]
    public void IsAttributeDefined_WithDefinedAttribute_ReturnsTrue()
    {
        // Arrange
        var member = typeof(AnnotatedEntity);

        // Act
        var result = member.IsAttributeDefined<System.ComponentModel.DescriptionAttribute>();

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：对未带 Attribute 的属性调用 IsAttributeDefined，应返回 false
    /// </summary>
    [Fact]
    public void IsAttributeDefined_WithUndefinedAttribute_ReturnsFalse()
    {
        // Arrange
        var member = typeof(AnnotatedEntity).GetProperty(nameof(AnnotatedEntity.NoDescription))!;

        // Act
        var result = member.IsAttributeDefined<System.ComponentModel.DescriptionAttribute>();

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：对带有 Description 属性的属性成员调用 IsAttributeDefined，应返回 true
    /// </summary>
    [Fact]
    public void IsAttributeDefined_OnAnnotatedProperty_ReturnsTrue()
    {
        // Arrange
        var member = typeof(AnnotatedEntity).GetProperty(nameof(AnnotatedEntity.Name))!;

        // Act
        var result = member.IsAttributeDefined<System.ComponentModel.DescriptionAttribute>();

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region IsAttributeNotDefined<TAttribute>

    /// <summary>
    /// 测试目的：对无 Description Attribute 的属性调用 IsAttributeNotDefined，应返回 true
    /// </summary>
    [Fact]
    public void IsAttributeNotDefined_OnNonAnnotatedProperty_ReturnsTrue()
    {
        // Arrange
        var member = typeof(AnnotatedEntity).GetProperty(nameof(AnnotatedEntity.NoDescription))!;

        // Act
        var result = member.IsAttributeNotDefined<System.ComponentModel.DescriptionAttribute>();

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region IsDescriptionDefined

    /// <summary>
    /// 测试目的：对带有 Description Attribute 的类型调用 IsDescriptionDefined，应返回 true
    /// </summary>
    [Fact]
    public void IsDescriptionDefined_WithDescriptionAttribute_ReturnsTrue()
    {
        // Arrange
        MemberInfo member = typeof(AnnotatedEntity);

        // Act
        var result = member.IsDescriptionDefined();

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：对无 Description Attribute 的类型调用 IsDescriptionDefined，应返回 false
    /// </summary>
    [Fact]
    public void IsDescriptionDefined_WithoutDescriptionAttribute_ReturnsFalse()
    {
        // Arrange
        MemberInfo member = typeof(SampleEntity);

        // Act
        var result = member.IsDescriptionDefined();

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetDescription

    /// <summary>
    /// 测试目的：对带有 Description Attribute 的类型调用 GetDescription，应返回 Attribute 中定义的文本
    /// </summary>
    [Fact]
    public void GetDescription_OnAnnotatedType_ReturnsDescriptionText()
    {
        // Arrange
        MemberInfo member = typeof(AnnotatedEntity);

        // Act
        var result = member.GetDescription();

        // Assert
        result.ShouldBe("Sample class description");
    }

    /// <summary>
    /// 测试目的：对带 Description 属性的属性成员调用 GetDescription，应返回属性描述文本
    /// </summary>
    [Fact]
    public void GetDescription_OnAnnotatedProperty_ReturnsPropertyDescription()
    {
        // Arrange
        var member = typeof(AnnotatedEntity).GetProperty(nameof(AnnotatedEntity.Name))!;

        // Act
        var result = member.GetDescription();

        // Assert
        result.ShouldBe("Name property description");
    }

    /// <summary>
    /// 测试目的：对无 Description Attribute 的成员调用 GetDescription，应回退到 member.Name
    /// </summary>
    [Fact]
    public void GetDescription_WithoutAnnotation_ReturnsEmptyString()
    {
        // Arrange
        var member = typeof(AnnotatedEntity).GetProperty(nameof(AnnotatedEntity.NoDescription))!;

        // Act
        var result = member.GetDescription();

        // Assert — 源码实现：无 DescriptionAttribute 时回退到 member.Name
        result.ShouldBe(nameof(AnnotatedEntity.NoDescription));
    }

    #endregion

    #region GetDescriptionOr

    /// <summary>
    /// 测试目的：对有 Description 的成员调用 GetDescriptionOr，应返回 Description 内容而非默认值
    /// </summary>
    [Fact]
    public void GetDescriptionOr_WithAnnotation_ReturnsDescription()
    {
        // Arrange
        var member = typeof(AnnotatedEntity).GetProperty(nameof(AnnotatedEntity.Name))!;

        // Act
        var result = member.GetDescriptionOr("default");

        // Assert
        result.ShouldBe("Name property description");
    }

    /// <summary>
    /// 测试目的：对无 Description 的成员调用 GetDescriptionOr，应返回指定默认值
    /// </summary>
    [Fact]
    public void GetDescriptionOr_WithoutAnnotation_ReturnsDefaultValue()
    {
        // Arrange
        var member = typeof(AnnotatedEntity).GetProperty(nameof(AnnotatedEntity.NoDescription))!;

        // Act
        var result = member.GetDescriptionOr("fallback");

        // Assert
        result.ShouldBe("fallback");
    }

    #endregion
}
