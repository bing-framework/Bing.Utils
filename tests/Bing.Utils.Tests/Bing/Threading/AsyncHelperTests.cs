using Bing.Threading;

namespace Bing.Utils.Tests.Bing.Threading;

/// <summary>
/// <see cref="AsyncHelper"/> 扩展方法单元测试
/// </summary>
public class AsyncHelperTests
{
    // ─────────────────────────────────────────────────────────────────
    // 辅助测试方法载体
    // ─────────────────────────────────────────────────────────────────

    private static class TestMethods
    {
        public static Task TaskAsync() => Task.CompletedTask;
        public static Task<int> TaskOfIntAsync() => Task.FromResult(0);
        public static Task<string> TaskOfStringAsync() => Task.FromResult(string.Empty);
        public static void VoidMethod() { }
        public static int IntMethod() => 0;
        public static string StringMethod() => string.Empty;
    }

    private static MethodInfo GetMethod(string name) =>
        typeof(TestMethods).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

    // ─────────────────────────────────────────────────────────────────
    // IsAsync
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsAsync_TaskMethod_ReturnsTrue()
    {
        GetMethod(nameof(TestMethods.TaskAsync)).IsAsync().ShouldBeTrue();
    }

    [Fact]
    public void IsAsync_TaskOfIntMethod_ReturnsTrue()
    {
        GetMethod(nameof(TestMethods.TaskOfIntAsync)).IsAsync().ShouldBeTrue();
    }

    [Fact]
    public void IsAsync_TaskOfStringMethod_ReturnsTrue()
    {
        GetMethod(nameof(TestMethods.TaskOfStringAsync)).IsAsync().ShouldBeTrue();
    }

    [Fact]
    public void IsAsync_VoidMethod_ReturnsFalse()
    {
        GetMethod(nameof(TestMethods.VoidMethod)).IsAsync().ShouldBeFalse();
    }

    [Fact]
    public void IsAsync_IntMethod_ReturnsFalse()
    {
        GetMethod(nameof(TestMethods.IntMethod)).IsAsync().ShouldBeFalse();
    }

    [Fact]
    public void IsAsync_StringMethod_ReturnsFalse()
    {
        GetMethod(nameof(TestMethods.StringMethod)).IsAsync().ShouldBeFalse();
    }

    [Fact]
    public void IsAsync_NullMethod_ThrowsArgumentNullException()
    {
        MethodInfo m = null!;
        Should.Throw<ArgumentNullException>(() => m.IsAsync());
    }

    // ─────────────────────────────────────────────────────────────────
    // IsTaskOrTaskOfT
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsTaskOrTaskOfT_Task_ReturnsTrue()
    {
        typeof(Task).IsTaskOrTaskOfT().ShouldBeTrue();
    }

    [Fact]
    public void IsTaskOrTaskOfT_TaskOfInt_ReturnsTrue()
    {
        typeof(Task<int>).IsTaskOrTaskOfT().ShouldBeTrue();
    }

    [Fact]
    public void IsTaskOrTaskOfT_TaskOfString_ReturnsTrue()
    {
        typeof(Task<string>).IsTaskOrTaskOfT().ShouldBeTrue();
    }

    [Fact]
    public void IsTaskOrTaskOfT_String_ReturnsFalse()
    {
        typeof(string).IsTaskOrTaskOfT().ShouldBeFalse();
    }

    [Fact]
    public void IsTaskOrTaskOfT_Int_ReturnsFalse()
    {
        typeof(int).IsTaskOrTaskOfT().ShouldBeFalse();
    }

    [Fact]
    public void IsTaskOrTaskOfT_ValueTaskOfInt_ReturnsFalse()
    {
        typeof(ValueTask<int>).IsTaskOrTaskOfT().ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // IsTaskOfT
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IsTaskOfT_TaskOfString_ReturnsTrue()
    {
        typeof(Task<string>).IsTaskOfT().ShouldBeTrue();
    }

    [Fact]
    public void IsTaskOfT_TaskOfInt_ReturnsTrue()
    {
        typeof(Task<int>).IsTaskOfT().ShouldBeTrue();
    }

    [Fact]
    public void IsTaskOfT_Task_ReturnsFalse()
    {
        typeof(Task).IsTaskOfT().ShouldBeFalse();
    }

    [Fact]
    public void IsTaskOfT_Void_ReturnsFalse()
    {
        typeof(void).IsTaskOfT().ShouldBeFalse();
    }

    [Fact]
    public void IsTaskOfT_String_ReturnsFalse()
    {
        typeof(string).IsTaskOfT().ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // UnwrapTask
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void UnwrapTask_Task_ReturnsVoidType()
    {
        AsyncHelper.UnwrapTask(typeof(Task)).ShouldBe(typeof(void));
    }

    [Fact]
    public void UnwrapTask_TaskOfInt_ReturnsIntType()
    {
        AsyncHelper.UnwrapTask(typeof(Task<int>)).ShouldBe(typeof(int));
    }

    [Fact]
    public void UnwrapTask_TaskOfString_ReturnsStringType()
    {
        AsyncHelper.UnwrapTask(typeof(Task<string>)).ShouldBe(typeof(string));
    }

    [Fact]
    public void UnwrapTask_String_ReturnsSameType()
    {
        AsyncHelper.UnwrapTask(typeof(string)).ShouldBe(typeof(string));
    }

    [Fact]
    public void UnwrapTask_Int_ReturnsSameType()
    {
        AsyncHelper.UnwrapTask(typeof(int)).ShouldBe(typeof(int));
    }

    [Fact]
    public void UnwrapTask_Null_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => AsyncHelper.UnwrapTask(null!));
    }
}
