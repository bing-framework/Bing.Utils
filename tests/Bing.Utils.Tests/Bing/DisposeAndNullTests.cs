namespace Bing.Utils.Tests.Bing;

/// <summary>
/// <see cref="NullDisposable"/>、<see cref="DisposeAction"/>、<see cref="DisposeAction{T}"/> 单元测试
/// </summary>
public class DisposeAndNullTests
{
    // ─────────────────────────────────────────────────────────────────
    // NullDisposable
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证空操作释放对象的单例非空。
    /// </summary>
    [Fact]
    public void NullDisposable_Instance_IsNotNull()
    {
        NullDisposable.Instance.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证空操作释放对象始终返回同一实例。
    /// </summary>
    [Fact]
    public void NullDisposable_Instance_IsSingleton()
    {
        NullDisposable.Instance.ShouldBeSameAs(NullDisposable.Instance);
    }

    /// <summary>
    /// 验证空操作释放对象实现释放接口。
    /// </summary>
    [Fact]
    public void NullDisposable_ImplementsIDisposable()
    {
        NullDisposable.Instance.ShouldBeAssignableTo<IDisposable>();
    }

    /// <summary>
    /// 验证空操作释放不会抛出异常。
    /// </summary>
    [Fact]
    public void NullDisposable_Dispose_DoesNotThrow()
    {
        Should.NotThrow(() => NullDisposable.Instance.Dispose());
    }

    /// <summary>
    /// 验证空操作释放可重复调用。
    /// </summary>
    [Fact]
    public void NullDisposable_Dispose_CanBeCalledMultipleTimes()
    {
        Should.NotThrow(() =>
        {
            NullDisposable.Instance.Dispose();
            NullDisposable.Instance.Dispose();
            NullDisposable.Instance.Dispose();
        });
    }

    /// <summary>
    /// 验证空操作释放对象可用于作用域释放。
    /// </summary>
    [Fact]
    public void NullDisposable_UsedInUsingStatement_DoesNotThrow()
    {
        Should.NotThrow(() =>
        {
            using var d = NullDisposable.Instance;
            // 不执行任何实际操作
        });
    }

    // ─────────────────────────────────────────────────────────────────
    // DisposeAction
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证释放回调对象拒绝空回调。
    /// </summary>
    [Fact]
    public void DisposeAction_Constructor_NullAction_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DisposeAction(null!));
    }

    /// <summary>
    /// 验证释放对象时执行回调。
    /// </summary>
    [Fact]
    public void DisposeAction_Dispose_InvokesAction()
    {
        var called = false;
        var da = new DisposeAction(() => called = true);
        da.Dispose();
        called.ShouldBeTrue();
    }

    /// <summary>
    /// 验证释放回调在退出作用域时执行。
    /// </summary>
    [Fact]
    public void DisposeAction_UsedInUsingStatement_ActionCalledOnExit()
    {
        var cleaned = false;
        using (new DisposeAction(() => cleaned = true))
        {
            cleaned.ShouldBeFalse(); // 尚未退出 using 块
        }
        cleaned.ShouldBeTrue();
    }

    /// <summary>
    /// 验证重复释放会重复执行回调。
    /// </summary>
    [Fact]
    public void DisposeAction_Dispose_CalledTwice_ActionCalledTwice()
    {
        var count = 0;
        var da = new DisposeAction(() => count++);
        da.Dispose();
        da.Dispose();
        count.ShouldBe(2);
    }

    /// <summary>
    /// 验证释放回调对象实现释放接口。
    /// </summary>
    [Fact]
    public void DisposeAction_ImplementsIDisposable()
    {
        var da = new DisposeAction(() => { });
        da.ShouldBeAssignableTo<IDisposable>();
    }

    // ─────────────────────────────────────────────────────────────────
    // DisposeAction<T>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证泛型释放回调对象拒绝空回调。
    /// </summary>
    [Fact]
    public void DisposeActionT_Constructor_NullAction_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DisposeAction<string>(null!, "param"));
    }

    /// <summary>
    /// 验证泛型释放将保存的参数传给回调。
    /// </summary>
    [Fact]
    public void DisposeActionT_Dispose_InvokesActionWithParameter()
    {
        string captured = null;
        var da = new DisposeAction<string>(p => captured = p, "hello");
        da.Dispose();
        captured.ShouldBe("hello");
    }

    /// <summary>
    /// 验证参数为空时不执行泛型释放回调。
    /// </summary>
    [Fact]
    public void DisposeActionT_Dispose_NullParameter_DoesNotInvokeAction()
    {
        var called = false;
        var da = new DisposeAction<string>(_ => called = true, null);
        da.Dispose();
        called.ShouldBeFalse();
    }

    /// <summary>
    /// 验证泛型释放回调在退出作用域时执行。
    /// </summary>
    [Fact]
    public void DisposeActionT_UsedInUsingStatement_ActionCalledOnExit()
    {
        var sum = 0;
        using (new DisposeAction<int>(x => sum = x, 42))
        {
            sum.ShouldBe(0); // 尚未退出 using 块
        }
        sum.ShouldBe(42);
    }

    /// <summary>
    /// 验证泛型释放回调对象实现释放接口。
    /// </summary>
    [Fact]
    public void DisposeActionT_ImplementsIDisposable()
    {
        var da = new DisposeAction<int>(_ => { }, 0);
        da.ShouldBeAssignableTo<IDisposable>();
    }
}
