namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `AssemblyVisitAndTypeVisitGuard` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "Guards")]
public class AssemblyVisitAndTypeVisitGuardTest
{
    /// <summary>
    /// 测试用例：验证 `GetFileVersion` 在 `NullAssembly` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetFileVersion_NullAssembly_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => AssemblyVisit.GetFileVersion(null));
        ex.ParamName.ShouldBe("assembly");
    }
    /// <summary>
    /// 测试用例：验证 `GetProductVersion` 在 `NullAssembly` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void GetProductVersion_NullAssembly_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => AssemblyVisit.GetProductVersion(null));
        ex.ParamName.ShouldBe("assembly");
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstance` 在 `NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateInstance_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 测试用例：验证 `CreateInstanceGeneric` 在 `NullType` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void CreateInstanceGeneric_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance<object>((Type)null));
        ex.ParamName.ShouldBe("type");
    }
}

