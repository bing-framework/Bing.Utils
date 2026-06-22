using System.Reflection;
using System.Threading.Tasks;
using Bing.Reflection;

namespace Bing.Reflection.Tests;

// ─── 测试辅助类型 ─────────────────────────────────────────────────────────────

/// <summary>基类：含有虚方法和普通方法</summary>
public class MethodTestBase
{
    /// <summary>虚方法（public）</summary>
    public virtual string VirtualMethod() => "base";

    /// <summary>普通 public 方法（non-virtual）</summary>
    public string NormalMethod() => "normal";

    /// <summary>受保护的虚方法</summary>
    protected virtual void ProtectedVirtual() { }

    /// <summary>私有方法（invisible）</summary>
    private void PrivateMethod() { }
}

/// <summary>派生类：重写了基类的 VirtualMethod</summary>
public class MethodTestDerived : MethodTestBase
{
    /// <inheritdoc />
    public override string VirtualMethod() => "derived";
}

/// <summary>含有异步方法的测试类</summary>
public class AsyncMethodTestClass
{
    public Task AsyncTaskMethod() => Task.CompletedTask;
    public Task<int> AsyncTaskOfTMethod() => Task.FromResult(42);
    public ValueTask AsyncValueTaskMethod() => default;
    public ValueTask<string> AsyncValueTaskOfTMethod() => new("hello");
    public void SyncVoidMethod() { }
    public int SyncReturnMethod() => 0;
}

// ─── 测试类 ──────────────────────────────────────────────────────────────────

/// <summary>
/// 测试类：覆盖 <see cref="TypeVisit"/> Methods 相关行为（GetBaseMethod、IsVisible、
/// IsVisibleAndVirtual）以及 <see cref="TypeMetaVisitExtensions"/> 中的
/// IsAsyncMethod、IsOverridden。
/// </summary>
[Trait("Reflection", "TypeVisit.Methods")]
public class TypeVisitMethodsTest
{
    #region GetBaseMethod

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.GetBaseMethod"/> 在 `OverriddenMethod` 场景下，结果为 `ReturnsBaseClassMethod`。
    /// </summary>
    [Fact]
    public void GetBaseMethod_OverriddenMethod_ReturnsBaseClassMethod()
    {
        var overriddenMethod = typeof(MethodTestDerived).GetMethod(nameof(MethodTestDerived.VirtualMethod))!;
        var baseMethod = TypeVisit.GetBaseMethod(overriddenMethod);
        baseMethod.ShouldNotBeNull();
        baseMethod.DeclaringType.ShouldBe(typeof(MethodTestBase));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.GetBaseMethod"/> 在 `NonOverriddenMethod` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void GetBaseMethod_NonOverriddenMethod_ReturnsNull()
    {
        var method = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.NormalMethod))!;
        var baseMethod = TypeVisit.GetBaseMethod(method);
        baseMethod.ShouldBeNull();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.GetBaseMethod"/> 在 `ExtensionMethod` 场景下，结果为 `ConsistentWithStaticCall`。
    /// </summary>
    [Fact]
    public void GetBaseMethod_ExtensionMethod_ConsistentWithStaticCall()
    {
        var method = typeof(MethodTestDerived).GetMethod(nameof(MethodTestDerived.VirtualMethod))!;
        var staticResult = TypeVisit.GetBaseMethod(method);
        var extensionResult = method.GetBaseMethod();
        extensionResult.ShouldBe(staticResult);
    }

    #endregion

    #region IsVisible

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.IsVisible"/> 在 `PublicMethod` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsVisible_PublicMethod_ReturnsTrue()
    {
        var method = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.NormalMethod))!;
        TypeVisit.IsVisible(method).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.IsVisible"/> 在 `PrivateMethod` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsVisible_PrivateMethod_ReturnsFalse()
    {
        var method = typeof(MethodTestBase).GetMethod("PrivateMethod",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        TypeVisit.IsVisible(method).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.IsVisible"/> 在 `NullMethod` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void IsVisible_NullMethod_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisible(null!));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsVisible"/> 在 `ExtensionMethod` 场景下，结果为 `ConsistentWithStaticCall`。
    /// </summary>
    [Fact]
    public void IsVisible_ExtensionMethod_ConsistentWithStaticCall()
    {
        var method = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.VirtualMethod))!;
        TypeVisit.IsVisible(method).ShouldBe(method.IsVisible());
    }

    #endregion

    #region IsVisibleAndVirtual

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.IsVisibleAndVirtual"/> 在 `PublicVirtualMethod` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_PublicVirtualMethod_ReturnsTrue()
    {
        var method = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.VirtualMethod))!;
        TypeVisit.IsVisibleAndVirtual(method).ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.IsVisibleAndVirtual"/> 在 `NonVirtualMethod` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_NonVirtualMethod_ReturnsFalse()
    {
        var method = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.NormalMethod))!;
        TypeVisit.IsVisibleAndVirtual(method).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeVisit.IsVisibleAndVirtual"/> 在 `NullMethod` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_NullMethod_ThrowsArgumentNullException()
    {
        // 显式指定 MethodInfo 类型，避免与 PropertyInfo 重载歧义
        Should.Throw<ArgumentNullException>(() => TypeVisit.IsVisibleAndVirtual((MethodInfo)null!));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsVisibleAndVirtual"/> 在 `ExtensionOnPublicVirtual` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsVisibleAndVirtual_ExtensionMethod_ConsistentWithStaticCall()
    {
        MethodInfo method = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.VirtualMethod))!;
        // 扩展方法应与静态调用结果一致
        method.IsVisibleAndVirtual().ShouldBeTrue();
        MethodInfo normalMethod = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.NormalMethod))!;
        normalMethod.IsVisibleAndVirtual().ShouldBeFalse();
    }

    #endregion

    #region IsAsyncMethod

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsAsyncMethod"/> 在 `TaskMethod` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsAsyncMethod_TaskMethod_ReturnsTrue()
    {
        var method = typeof(AsyncMethodTestClass).GetMethod(nameof(AsyncMethodTestClass.AsyncTaskMethod))!;
        method.IsAsyncMethod().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsAsyncMethod"/> 在 `TaskOfTMethod` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsAsyncMethod_TaskOfTMethod_ReturnsTrue()
    {
        var method = typeof(AsyncMethodTestClass).GetMethod(nameof(AsyncMethodTestClass.AsyncTaskOfTMethod))!;
        method.IsAsyncMethod().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsAsyncMethod"/> 在 `ValueTaskMethod` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsAsyncMethod_ValueTaskMethod_ReturnsTrue()
    {
        var method = typeof(AsyncMethodTestClass).GetMethod(nameof(AsyncMethodTestClass.AsyncValueTaskMethod))!;
        method.IsAsyncMethod().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsAsyncMethod"/> 在 `SyncVoidMethod` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsAsyncMethod_SyncVoidMethod_ReturnsFalse()
    {
        var method = typeof(AsyncMethodTestClass).GetMethod(nameof(AsyncMethodTestClass.SyncVoidMethod))!;
        method.IsAsyncMethod().ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsAsyncMethod"/> 在 `SyncReturnMethod` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsAsyncMethod_SyncReturnMethod_ReturnsFalse()
    {
        var method = typeof(AsyncMethodTestClass).GetMethod(nameof(AsyncMethodTestClass.SyncReturnMethod))!;
        method.IsAsyncMethod().ShouldBeFalse();
    }

    #endregion

    #region IsOverridden

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsOverridden"/> 在 `OverriddenMethod` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsOverridden_OverriddenMethod_ReturnsTrue()
    {
        var method = typeof(MethodTestDerived).GetMethod(nameof(MethodTestDerived.VirtualMethod))!;
        method.IsOverridden().ShouldBeTrue();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsOverridden"/> 在 `BaseClassMethod` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsOverridden_BaseClassMethod_ReturnsFalse()
    {
        var method = typeof(MethodTestBase).GetMethod(nameof(MethodTestBase.VirtualMethod))!;
        method.IsOverridden().ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 <see cref="TypeMetaVisitExtensions.IsOverridden"/> 在 `NonOverriddenDerivedMethod` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void IsOverridden_NonOverriddenDerivedMethod_ReturnsFalse()
    {
        var method = typeof(MethodTestDerived).GetMethod(nameof(MethodTestDerived.NormalMethod))!;
        method.IsOverridden().ShouldBeFalse();
    }

    #endregion
}
