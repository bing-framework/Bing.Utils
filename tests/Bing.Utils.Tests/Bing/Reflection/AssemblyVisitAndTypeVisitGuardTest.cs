namespace Bing.Reflection;
/// <summary>
/// 验证程序集版本读取及实例创建的空参数检查。
/// </summary>
[Trait("Bing.Reflection", "Guards")]
public class AssemblyVisitAndTypeVisitGuardTest
{
    /// <summary>
    /// 验证读取文件版本时拒绝空程序集。
    /// </summary>
    [Fact]
    public void GetFileVersion_NullAssembly_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => AssemblyVisit.GetFileVersion(null));
        ex.ParamName.ShouldBe("assembly");
    }
    /// <summary>
    /// 验证读取产品版本时拒绝空程序集。
    /// </summary>
    [Fact]
    public void GetProductVersion_NullAssembly_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => AssemblyVisit.GetProductVersion(null));
        ex.ParamName.ShouldBe("assembly");
    }
    /// <summary>
    /// 验证非泛型实例创建入口拒绝空类型。
    /// </summary>
    [Fact]
    public void CreateInstance_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance((Type)null));
        ex.ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证泛型实例创建入口拒绝空类型。
    /// </summary>
    [Fact]
    public void CreateInstanceGeneric_NullType_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => TypeVisit.CreateInstance<object>((Type)null));
        ex.ParamName.ShouldBe("type");
    }
}

