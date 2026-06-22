namespace Bing.Utils.Tests.Bing;

/// <summary>
/// <see cref="NullDisposable"/>、<see cref="DisposeAction"/>、<see cref="DisposeAction{T}"/> 单元测试
/// </summary>
public class DisposeAndNullTests
{
    // ─────────────────────────────────────────────────────────────────
    // NullDisposable
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void NullDisposable_Instance_IsNotNull()
    {
        NullDisposable.Instance.ShouldNotBeNull();
    }

    [Fact]
    public void NullDisposable_Instance_IsSingleton()
    {
        NullDisposable.Instance.ShouldBeSameAs(NullDisposable.Instance);
    }

    [Fact]
    public void NullDisposable_ImplementsIDisposable()
    {
        NullDisposable.Instance.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void NullDisposable_Dispose_DoesNotThrow()
    {
        Should.NotThrow(() => NullDisposable.Instance.Dispose());
    }

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

    [Fact]
    public void DisposeAction_Constructor_NullAction_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DisposeAction(null!));
    }

    [Fact]
    public void DisposeAction_Dispose_InvokesAction()
    {
        var called = false;
        var da = new DisposeAction(() => called = true);
        da.Dispose();
        called.ShouldBeTrue();
    }

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

    [Fact]
    public void DisposeAction_Dispose_CalledTwice_ActionCalledTwice()
    {
        var count = 0;
        var da = new DisposeAction(() => count++);
        da.Dispose();
        da.Dispose();
        count.ShouldBe(2);
    }

    [Fact]
    public void DisposeAction_ImplementsIDisposable()
    {
        var da = new DisposeAction(() => { });
        da.ShouldBeAssignableTo<IDisposable>();
    }

    // ─────────────────────────────────────────────────────────────────
    // DisposeAction<T>
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void DisposeActionT_Constructor_NullAction_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DisposeAction<string>(null!, "param"));
    }

    [Fact]
    public void DisposeActionT_Dispose_InvokesActionWithParameter()
    {
        string captured = null;
        var da = new DisposeAction<string>(p => captured = p, "hello");
        da.Dispose();
        captured.ShouldBe("hello");
    }

    [Fact]
    public void DisposeActionT_Dispose_NullParameter_DoesNotInvokeAction()
    {
        var called = false;
        var da = new DisposeAction<string>(_ => called = true, null);
        da.Dispose();
        called.ShouldBeFalse();
    }

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

    [Fact]
    public void DisposeActionT_ImplementsIDisposable()
    {
        var da = new DisposeAction<int>(_ => { }, 0);
        da.ShouldBeAssignableTo<IDisposable>();
    }
}
