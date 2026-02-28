using System.Collections.Generic;
using Bing.Reflection.Tests;
using Shouldly;

namespace Bing.Reflection;

/// <summary>
/// 测试类：TypeVisit 类型全名与完全限定名相关方法
/// </summary>
[Trait("ReflectionUT", "TypeVisit.Types")]
public class TypeVisitTypesTests
{
    #region GetFullName(Type)

    /// <summary>
    /// 测试目的：对普通值类型调用 GetFullName，应返回其 FullName（包含命名空间）
    /// </summary>
    [Theory]
    [InlineData(typeof(int), "System.Int32")]
    [InlineData(typeof(string), "System.String")]
    [InlineData(typeof(bool), "System.Boolean")]
    public void GetFullName_WithPrimitiveType_ReturnsFullTypeName(Type type, string expected)
    {
        // Act
        var result = TypeVisit.GetFullName(type);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：对 null 调用 GetFullName，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetFullName_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => TypeVisit.GetFullName((Type)null!));
    }

    #endregion

    #region GetFullyQualifiedName(Type)

    /// <summary>
    /// 测试目的：对非泛型类型调用 GetFullyQualifiedName，结果与 FullName 一致
    /// </summary>
    [Theory]
    [InlineData(typeof(int), "System.Int32")]
    [InlineData(typeof(string), "System.String")]
    public void GetFullyQualifiedName_WithNonGenericType_ReturnsFullName(Type type, string expected)
    {
        // Act
        var result = TypeVisit.GetFullyQualifiedName(type);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：对单泛型参数类型调用 GetFullyQualifiedName，应返回含泛型参数的完全限定名
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_WithSingleGenericType_ContainsGenericArgument()
    {
        // Arrange
        var type = typeof(List<int>);

        // Act
        var result = TypeVisit.GetFullyQualifiedName(type);

        // Assert
        result.ShouldContain("System.Collections.Generic.List");
        result.ShouldContain("System.Int32");
    }

    /// <summary>
    /// 测试目的：对嵌套泛型类型调用 GetFullyQualifiedName，应递归展开所有泛型参数
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_WithNestedGenericType_ContainsAllArguments()
    {
        // Arrange
        var type = typeof(Dictionary<string, List<int>>);

        // Act
        var result = TypeVisit.GetFullyQualifiedName(type);

        // Assert
        result.ShouldContain("System.Collections.Generic.Dictionary");
        result.ShouldContain("System.String");
        result.ShouldContain("System.Collections.Generic.List");
        result.ShouldContain("System.Int32");
    }

    /// <summary>
    /// 测试目的：对 null 调用 GetFullyQualifiedName，应抛出 ArgumentNullException
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => TypeVisit.GetFullyQualifiedName((Type)null!));
    }

    /// <summary>
    /// 测试目的：扩展方法 GetFullyQualifiedName() 与静态方法结果一致
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_ExtensionMethod_MatchesStaticMethod()
    {
        // Arrange
        var type = typeof(List<string>);

        // Act
        var staticResult = TypeVisit.GetFullyQualifiedName(type);
        var extensionResult = type.GetFullyQualifiedName();

        // Assert
        extensionResult.ShouldBe(staticResult);
    }

    #endregion

    #region GetFullName(MethodInfo)

    /// <summary>
    /// 测试目的：对普通方法调用 GetFullName，应返回"类全名.方法名"格式
    /// </summary>
    [Fact]
    public void GetFullName_WithMethod_ReturnsClassAndMethodName()
    {
        // Arrange
        var method = typeof(SampleEntity).GetMethod(nameof(SampleEntity.Greet), Type.EmptyTypes);

        // Act
        var result = TypeVisit.GetFullName(method!);

        // Assert
        result.ShouldContain("SampleEntity");
        result.ShouldEndWith(".Greet");
    }

    /// <summary>
    /// 测试目的：对重载方法 GetFullName 不包含参数信息，仅含类名.方法名
    /// </summary>
    [Fact]
    public void GetFullName_WithOverloadedMethod_OnlyContainsMethodName()
    {
        // Arrange
        var method = typeof(SampleEntity).GetMethod(nameof(SampleEntity.Greet), new[] { typeof(string) });

        // Act
        var result = TypeVisit.GetFullName(method!);

        // Assert
        result.ShouldContain("SampleEntity");
        result.ShouldEndWith(".Greet");
    }

    #endregion

    #region GetFullyQualifiedName(MethodInfo)

    /// <summary>
    /// 测试目的：对无参方法调用 GetFullyQualifiedName，应包含返回类型、方法名和空括号
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_WithParameterlessMethod_ContainsReturnTypeAndEmptyParams()
    {
        // Arrange
        var method = typeof(SampleEntity).GetMethod(nameof(SampleEntity.Greet), Type.EmptyTypes);

        // Act
        var result = TypeVisit.GetFullyQualifiedName(method!);

        // Assert
        result.ShouldContain("System.String");
        result.ShouldContain("Greet");
        result.ShouldEndWith("()");
    }

    /// <summary>
    /// 测试目的：对有参方法调用 GetFullyQualifiedName，应包含参数类型
    /// </summary>
    [Fact]
    public void GetFullyQualifiedName_WithParameteredMethod_ContainsParameterTypes()
    {
        // Arrange
        var method = typeof(SampleEntity).GetMethod(nameof(SampleEntity.Greet), new[] { typeof(string) });

        // Act
        var result = TypeVisit.GetFullyQualifiedName(method!);

        // Assert
        result.ShouldContain("System.String");
        result.ShouldContain("Greet");
        result.ShouldContain("System.String"); // parameter type
    }

    #endregion
}
