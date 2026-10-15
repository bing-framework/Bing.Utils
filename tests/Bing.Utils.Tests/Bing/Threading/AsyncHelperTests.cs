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

    /// <summary>
    /// 提供任务返回类型识别测试使用的方法集合。
    /// </summary>
    private static class TestMethods
    {
        /// <summary>
        /// 返回已完成的无结果任务。
        /// </summary>
        /// <returns>已完成的无结果任务。</returns>
        public static Task TaskAsync() => Task.CompletedTask;
        /// <summary>
        /// 返回结果为零的已完成任务。
        /// </summary>
        /// <returns>结果为零的已完成任务。</returns>
        public static Task<int> TaskOfIntAsync() => Task.FromResult(0);
        /// <summary>
        /// 返回结果为空文本的已完成任务。
        /// </summary>
        /// <returns>结果为空文本的已完成任务。</returns>
        public static Task<string> TaskOfStringAsync() => Task.FromResult(string.Empty);
        /// <summary>
        /// 提供无返回值的同步方法样例。
        /// </summary>
        public static void VoidMethod() { }
        /// <summary>
        /// 返回同步整数样例值。
        /// </summary>
        /// <returns>整数零。</returns>
        public static int IntMethod() => 0;
        /// <summary>
        /// 返回同步文本样例值。
        /// </summary>
        /// <returns>空文本。</returns>
        public static string StringMethod() => string.Empty;
    }

    /// <summary>
    /// 获取指定名称的公开静态样例方法。
    /// </summary>
    /// <param name="name">样例方法名称。</param>
    /// <returns>匹配名称的方法信息。</returns>
    private static MethodInfo GetMethod(string name) =>
        typeof(TestMethods).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

    // ─────────────────────────────────────────────────────────────────
    // IsAsync
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证返回 Task 的方法被识别为异步方法。
    /// </summary>
    [Fact]
    public void IsAsync_TaskMethod_ReturnsTrue()
    {
        GetMethod(nameof(TestMethods.TaskAsync)).IsAsync().ShouldBeTrue();
    }

    /// <summary>
    /// 验证返回整数任务的方法被识别为异步方法。
    /// </summary>
    [Fact]
    public void IsAsync_TaskOfIntMethod_ReturnsTrue()
    {
        GetMethod(nameof(TestMethods.TaskOfIntAsync)).IsAsync().ShouldBeTrue();
    }

    /// <summary>
    /// 验证返回文本任务的方法被识别为异步方法。
    /// </summary>
    [Fact]
    public void IsAsync_TaskOfStringMethod_ReturnsTrue()
    {
        GetMethod(nameof(TestMethods.TaskOfStringAsync)).IsAsync().ShouldBeTrue();
    }

    /// <summary>
    /// 验证无返回值的同步方法不被识别为异步方法。
    /// </summary>
    [Fact]
    public void IsAsync_VoidMethod_ReturnsFalse()
    {
        GetMethod(nameof(TestMethods.VoidMethod)).IsAsync().ShouldBeFalse();
    }

    /// <summary>
    /// 验证返回整数的同步方法不被识别为异步方法。
    /// </summary>
    [Fact]
    public void IsAsync_IntMethod_ReturnsFalse()
    {
        GetMethod(nameof(TestMethods.IntMethod)).IsAsync().ShouldBeFalse();
    }

    /// <summary>
    /// 验证返回文本的同步方法不被识别为异步方法。
    /// </summary>
    [Fact]
    public void IsAsync_StringMethod_ReturnsFalse()
    {
        GetMethod(nameof(TestMethods.StringMethod)).IsAsync().ShouldBeFalse();
    }

    /// <summary>
    /// 验证异步方法识别拒绝空方法信息。
    /// </summary>
    [Fact]
    public void IsAsync_NullMethod_ThrowsArgumentNullException()
    {
        MethodInfo m = null!;
        Should.Throw<ArgumentNullException>(() => m.IsAsync());
    }

    // ─────────────────────────────────────────────────────────────────
    // IsTaskOrTaskOfT
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证非泛型 Task 被识别为任务类型。
    /// </summary>
    [Fact]
    public void IsTaskOrTaskOfT_Task_ReturnsTrue()
    {
        typeof(Task).IsTaskOrTaskOfT().ShouldBeTrue();
    }

    /// <summary>
    /// 验证整数 Task 被识别为任务类型。
    /// </summary>
    [Fact]
    public void IsTaskOrTaskOfT_TaskOfInt_ReturnsTrue()
    {
        typeof(Task<int>).IsTaskOrTaskOfT().ShouldBeTrue();
    }

    /// <summary>
    /// 验证文本 Task 被识别为任务类型。
    /// </summary>
    [Fact]
    public void IsTaskOrTaskOfT_TaskOfString_ReturnsTrue()
    {
        typeof(Task<string>).IsTaskOrTaskOfT().ShouldBeTrue();
    }

    /// <summary>
    /// 验证字符串不被识别为任务类型。
    /// </summary>
    [Fact]
    public void IsTaskOrTaskOfT_String_ReturnsFalse()
    {
        typeof(string).IsTaskOrTaskOfT().ShouldBeFalse();
    }

    /// <summary>
    /// 验证整数不被识别为任务类型。
    /// </summary>
    [Fact]
    public void IsTaskOrTaskOfT_Int_ReturnsFalse()
    {
        typeof(int).IsTaskOrTaskOfT().ShouldBeFalse();
    }

    /// <summary>
    /// 验证整数 ValueTask 不被识别为 Task 类型。
    /// </summary>
    [Fact]
    public void IsTaskOrTaskOfT_ValueTaskOfInt_ReturnsFalse()
    {
        typeof(ValueTask<int>).IsTaskOrTaskOfT().ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // IsTaskOfT
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证文本任务被识别为泛型 Task。
    /// </summary>
    [Fact]
    public void IsTaskOfT_TaskOfString_ReturnsTrue()
    {
        typeof(Task<string>).IsTaskOfT().ShouldBeTrue();
    }

    /// <summary>
    /// 验证整数任务被识别为泛型 Task。
    /// </summary>
    [Fact]
    public void IsTaskOfT_TaskOfInt_ReturnsTrue()
    {
        typeof(Task<int>).IsTaskOfT().ShouldBeTrue();
    }

    /// <summary>
    /// 验证非泛型任务不被识别为泛型 Task。
    /// </summary>
    [Fact]
    public void IsTaskOfT_Task_ReturnsFalse()
    {
        typeof(Task).IsTaskOfT().ShouldBeFalse();
    }

    /// <summary>
    /// 验证无返回类型不被识别为泛型 Task。
    /// </summary>
    [Fact]
    public void IsTaskOfT_Void_ReturnsFalse()
    {
        typeof(void).IsTaskOfT().ShouldBeFalse();
    }

    /// <summary>
    /// 验证字符串不被识别为泛型 Task。
    /// </summary>
    [Fact]
    public void IsTaskOfT_String_ReturnsFalse()
    {
        typeof(string).IsTaskOfT().ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // UnwrapTask
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证无结果任务解包为无返回类型。
    /// </summary>
    [Fact]
    public void UnwrapTask_Task_ReturnsVoidType()
    {
        AsyncHelper.UnwrapTask(typeof(Task)).ShouldBe(typeof(void));
    }

    /// <summary>
    /// 验证整数任务解包为整数类型。
    /// </summary>
    [Fact]
    public void UnwrapTask_TaskOfInt_ReturnsIntType()
    {
        AsyncHelper.UnwrapTask(typeof(Task<int>)).ShouldBe(typeof(int));
    }

    /// <summary>
    /// 验证文本任务解包为字符串类型。
    /// </summary>
    [Fact]
    public void UnwrapTask_TaskOfString_ReturnsStringType()
    {
        AsyncHelper.UnwrapTask(typeof(Task<string>)).ShouldBe(typeof(string));
    }

    /// <summary>
    /// 验证字符串类型解包后保持不变。
    /// </summary>
    [Fact]
    public void UnwrapTask_String_ReturnsSameType()
    {
        AsyncHelper.UnwrapTask(typeof(string)).ShouldBe(typeof(string));
    }

    /// <summary>
    /// 验证整数类型解包后保持不变。
    /// </summary>
    [Fact]
    public void UnwrapTask_Int_ReturnsSameType()
    {
        AsyncHelper.UnwrapTask(typeof(int)).ShouldBe(typeof(int));
    }

    /// <summary>
    /// 验证任务类型解包拒绝空类型。
    /// </summary>
    [Fact]
    public void UnwrapTask_Null_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => AsyncHelper.UnwrapTask(null!));
    }
}
