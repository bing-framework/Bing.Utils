using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证链式与显式聚合异常查找的匹配及身份契约。
/// </summary>
public class ExceptionTraversalTests
{
    /// <summary>
    /// 验证 Exception 基类、派生类型和泛型扩展的公开匹配契约。
    /// </summary>
    [Fact]
    public void Unwrap_BaseTypeAndGenericExtensionAreSupported()
    {
        var inner = new ArgumentNullException("value");
        var root = new InvalidOperationException("outer", inner);
        root.Unwrap<Exception>().ShouldBeSameAs(root);
        ExceptionHelper.Unwrap<Exception>(root).ShouldBeSameAs(root);
        ExceptionHelperExtensions.Unwrap<Exception>(root).ShouldBeSameAs(root);
        root.Unwrap(typeof(Exception)).ShouldBeSameAs(root);
        root.Unwrap(typeof(Exception), false).ShouldBeNull();
        root.Unwrap(typeof(ArgumentException), true).ShouldBeSameAs(inner);
        root.Unwrap(typeof(ArgumentException), false).ShouldBeNull();
        root.Unwrap().ShouldBeSameAs(inner);
    }

    /// <summary>
    /// 验证聚合遍历显式启用且按原分支顺序深度优先匹配。
    /// </summary>
    [Fact]
    public void AggregateTraversal_IsOptInAndDepthFirst()
    {
        var first = new ArgumentException("first");
        var nested = new InvalidOperationException("nested");
        var last = new InvalidOperationException("last");
        var aggregate = new AggregateException(first, new Exception("wrapper", nested), last);
        aggregate.Unwrap<InvalidOperationException>().ShouldBeNull();
        aggregate.Unwrap<InvalidOperationException>(false).ShouldBeNull();
        aggregate.Unwrap<InvalidOperationException>(true).ShouldBeSameAs(nested);
        aggregate.Unwrap(typeof(InvalidOperationException), true, true).ShouldBeSameAs(nested);
        aggregate.Unwrap<AggregateException>(true).ShouldBeSameAs(aggregate);
        aggregate.Unwrap().ShouldBeSameAs(first);
        Try.LiftException<int>(aggregate).ExceptionAs<InvalidOperationException>(true).ShouldBeSameAs(nested);
        Try.Invoke(() => throw aggregate).ExceptionAs<InvalidOperationException>(true).ShouldBeSameAs(nested);
        Try.Invoke(() => throw aggregate).ExceptionAs<InvalidOperationException>().ShouldBeNull();
    }

    /// <summary>
    /// 验证操作失败可提取到原异常实例。
    /// </summary>
    [Fact]
    public void ActionExceptionAs_ReturnsOriginalException()
    {
        var original = new ArgumentNullException("value");
        var action = Try.Invoke(() => throw original);
        action.ExceptionAs<ArgumentException>().ShouldBeSameAs(original);
        action.ExceptionAs<TryInvokingException>().ShouldBeSameAs(action.Exception);
        action.ExceptionAs<InvalidOperationException>().ShouldBeNull();
        Try.Invoke(() => { }).ExceptionAs<Exception>(true).ShouldBeNull();
    }

    /// <summary>
    /// 验证引用去重不执行异常自定义相等逻辑。
    /// </summary>
    [Fact]
    public void Traversal_DoesNotUseExceptionEquality()
    {
        var shared = new HostileEqualityException();
        var found = new InvalidOperationException("target");
        var root = new AggregateException(shared, shared, new AggregateException(shared, found));
        root.Unwrap<InvalidOperationException>(true).ShouldBeSameAs(found);
    }

    /// <summary>
    /// 验证深层查找无需递归且能正确报告未命中。
    /// </summary>
    [Fact]
    public void TypedUnwrap_HandlesDeepChainsWithoutRecursion()
    {
        var target = new InvalidOperationException("leaf");
        Exception root = target;
        for (var index = 0; index < 20000; index++) root = new Exception("wrapper", root);
        root.Unwrap<InvalidOperationException>().ShouldBeSameAs(target);
        root.Unwrap<ArgumentException>().ShouldBeNull();
    }

    /// <summary>
    /// 验证异常查找对空参数及非异常类型的校验。
    /// </summary>
    [Fact]
    public void Traversal_ValidatesArguments()
    {
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(null, typeof(Exception), true, true));
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.Unwrap(new Exception(), null, true, true));
        Should.Throw<ArgumentException>(() => ExceptionHelper.Unwrap(new Exception(), typeof(string), true, true));
    }

    /// <summary>
    /// 检测遍历是否错误调用用户自定义相等逻辑。
    /// </summary>
    private sealed class HostileEqualityException : Exception
    {
        /// <inheritdoc />
        public override bool Equals(object obj) => throw new InvalidOperationException("Equals must not run");
        /// <inheritdoc />
        public override int GetHashCode() => throw new InvalidOperationException("GetHashCode must not run");
    }
}
