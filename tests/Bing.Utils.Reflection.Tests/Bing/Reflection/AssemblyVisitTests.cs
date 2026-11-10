using System.Reflection;
using System.Reflection.Emit;
using Shouldly;

namespace Bing.Reflection;

/// <summary>
/// 测试类：AssemblyVisit 程序集版本访问工具方法
/// </summary>
[Trait("ReflectionUT", "AssemblyVisit")]
public class AssemblyVisitTests
{
    #region GetFileVersion

    /// <summary>
    /// 验证空程序集输入会抛出参数空异常。
    /// </summary>
    [Fact]
    public void GetFileVersion_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => AssemblyVisit.GetFileVersion(null!));
    }

    /// <summary>
    /// 验证有效程序集可以读取文件版本。
    /// </summary>
    [Fact]
    public void GetFileVersion_WithValidAssembly_DoesNotThrow()
    {
        // Arrange — 使用当前执行程序集（测试 DLL 本身）
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var version = AssemblyVisit.GetFileVersion(assembly);

        // Assert — 优先读取程序集文件版本特性，不依赖 Assembly.Location
        version.ShouldBe(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? string.Empty);
    }

    #endregion

    #region GetProductVersion

    /// <summary>
    /// 验证空程序集输入会抛出参数空异常。
    /// </summary>
    [Fact]
    public void GetProductVersion_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => AssemblyVisit.GetProductVersion(null!));
    }

    /// <summary>
    /// 验证产品版本优先使用信息版本，并保留预发布版本。
    /// </summary>
    [Fact]
    public void GetProductVersion_WithInformationalVersion_PrefersItOverFileAndAssemblyVersion()
    {
        var assembly = CreateVersionAssembly();
        SetVersionAttribute<AssemblyFileVersionAttribute>(assembly, "4.5.6.7");
        SetVersionAttribute<AssemblyInformationalVersionAttribute>(assembly, "1.2.3-beta+build.123");

        AssemblyVisit.GetProductVersion(assembly).ShouldBe("1.2.3-beta");
        AssemblyVisit.GetFileVersion(assembly).ShouldBe("4.5.6.7");
    }

    /// <summary>
    /// 验证程序集没有版本特性时回退到名称版本。
    /// </summary>
    [Fact]
    public void GetVersion_WhenAssemblyHasNoVersionAttributes_UsesAssemblyNameVersion()
    {
        var name = new AssemblyName("AssemblyVisitVersionFallback")
        {
            Version = new Version(2, 3, 4, 5)
        };
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        AssemblyVisit.GetFileVersion(assembly).ShouldBe("2.3.4.5");
        AssemblyVisit.GetProductVersion(assembly).ShouldBe("2.3.4.5");
    }

    /// <summary>
    /// 验证产品版本移除完整的构建元数据后缀。
    /// </summary>
    /// <param name="version">程序集信息版本字符串。</param>
    /// <param name="expected">预期的产品版本。</param>
    [Theory]
    [InlineData("1.0.0+abc1234def", "1.0.0")]
    [InlineData("1.2.3+build.123", "1.2.3")]
    [InlineData("1.2.3+build-abc", "1.2.3")]
    [InlineData("1.2.3-beta+build.123", "1.2.3-beta")]
    [InlineData("2.3.1", "2.3.1")]
    [InlineData("1.2.3-beta", "1.2.3-beta")]
    [InlineData("1.2.3+", "1.2.3")]
    [InlineData("", "")]
    public void GetProductVersion_WithInformationalVersion_RemovesEntireBuildSuffix(string version, string expected)
    {
        var assembly = CreateVersionAssembly();
        SetVersionAttribute<AssemblyInformationalVersionAttribute>(assembly, version);

        AssemblyVisit.GetProductVersion(assembly).ShouldBe(expected);
    }

    /// <summary>
    /// 验证缺少信息版本时回退到文件版本。
    /// </summary>
    [Fact]
    public void GetProductVersion_WithoutInformationalVersion_UsesFileVersion()
    {
        var assembly = CreateVersionAssembly();
        SetVersionAttribute<AssemblyFileVersionAttribute>(assembly, "4.5.6.7");

        AssemblyVisit.GetProductVersion(assembly).ShouldBe("4.5.6.7");
    }

    /// <summary>
    /// 创建用于版本测试的动态程序集。
    /// </summary>
    /// <returns>包含测试程序集名称版本的动态程序集。</returns>
    private static AssemblyBuilder CreateVersionAssembly() => AssemblyBuilder.DefineDynamicAssembly(
        new AssemblyName("AssemblyVisitVersion_" + Guid.NewGuid().ToString("N")) { Version = new Version(2, 3, 4, 5) },
        AssemblyBuilderAccess.RunAndCollect);

    /// <summary>
    /// 为动态程序集设置版本特性。
    /// </summary>
    /// <typeparam name="TAttribute">具有字符串构造参数的版本特性类型。</typeparam>
    /// <param name="assembly">目标动态程序集。</param>
    /// <param name="version">写入特性的版本字符串。</param>
    private static void SetVersionAttribute<TAttribute>(AssemblyBuilder assembly, string version)
        where TAttribute : Attribute => assembly.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(TAttribute).GetConstructor(new[] { typeof(string) })!, new object[] { version }));

    #endregion
}
