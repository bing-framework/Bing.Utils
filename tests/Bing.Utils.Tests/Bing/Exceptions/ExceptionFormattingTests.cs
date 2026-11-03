using System.Runtime.CompilerServices;
using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证异常格式化在真实堆栈、聚合分支和资源预算下的行为。
/// </summary>
public class ExceptionFormattingTests
{
    /// <summary>
    /// 验证旧格式化入口也执行默认深度和输出预算。
    /// </summary>
    [Fact]
    public void LegacySignatures_UseFiniteDefaultBudgets()
    {
        Exception chain = new Exception("leaf");
        for (var i = 0; i < 1000; i++) chain = new Exception("wrapper", chain);
        var formatted = chain.FormatMessage();
        var detail = ExceptionHelper.GetExceptionDetail(chain);
        formatted.Length.ShouldBeLessThanOrEqualTo(32768);
        detail.Length.ShouldBeLessThanOrEqualTo(32768);
        formatted.ShouldContain("[已截断：深度上限]");
        detail.ShouldContain("[已截断：深度上限]");
    }

    /// <summary>
    /// 验证两个旧入口的默认字符预算会截断长消息。
    /// </summary>
    /// <param name="detail">是否调用详情入口。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacySignatures_TruncateLongMessagesAtDefaultLength(bool detail)
    {
        var error = new Exception(new string('x', 70000));
        var text = detail ? ExceptionHelper.GetExceptionDetail(error) : error.FormatMessage();
        text.Length.ShouldBeLessThanOrEqualTo(32768);
        text.ShouldContain("[已截断：输出长度上限]");
        text.ShouldNotContain("[已截断：深度上限]");
        text.ShouldNotContain("[已截断：异常节点数上限]");
    }

    /// <summary>
    /// 验证达到不同节点预算后，重复引用仍显示去重标记。
    /// </summary>
    /// <param name="detail">是否调用详情入口。</param>
    /// <param name="hasExtraNode">是否在重复引用后还有超过预算的不同节点。</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void NodeBudget_CountsDistinctInstancesBeforeReadingNewNodes(bool detail, bool hasExtraNode)
    {
        var shared = new Exception("shared");
        var branches = hasExtraNode
            ? new Exception[] { shared, shared, new MessageTrapException() }
            : new[] { shared, shared };
        var error = new AggregateMessageTrapException(branches);
        var options = new ExceptionFormatOptions { IncludeAggregateExceptions = true, MaxExceptionCount = 2 };
        var text = detail ? ExceptionHelper.GetExceptionDetail(error, options) : error.FormatMessage(options);
        text.ShouldContain("shared");
        text.ShouldContain("[已出现：相同异常实例]");
        text.Contains("[已截断：异常节点数上限]").ShouldBe(hasExtraNode);
        text.ShouldNotContain("[已截断：输出长度上限]");
    }

    /// <summary>
    /// 验证宽聚合先达到字符预算时不继续读取后续分支消息。
    /// </summary>
    /// <param name="detail">是否调用详情入口。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WideAggregate_StopsAtOutputLengthBeforeNodeLimit(bool detail)
    {
        var branches = Enumerable.Range(0, 128)
            .Select(i => new Exception(i + new string('m', 512)))
            .Concat(new[] { new MessageTrapException() }).ToArray();
        var error = new AggregateMessageTrapException(branches);
        var options = new ExceptionFormatOptions { IncludeAggregateExceptions = true, MaxOutputLength = 1024 };
        var text = detail ? ExceptionHelper.GetExceptionDetail(error, options) : error.FormatMessage(options);
        text.Length.ShouldBeLessThanOrEqualTo(1024);
        text.ShouldContain("[已截断：输出长度上限]");
        text.ShouldNotContain("[已截断：异常节点数上限]");
        text.ShouldNotContain("[已截断：深度上限]");
    }

    /// <summary>
    /// 验证真实深调用堆栈在两个入口均按字符预算截断。
    /// </summary>
    /// <param name="detail">是否调用详情入口。</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealDeepStack_IsTruncatedWithinConfiguredLength(bool detail)
    {
        var error = Should.Throw<InvalidOperationException>(() => ThrowDeep(128));
        var stack = error.StackTrace;
        stack.Length.ShouldBeGreaterThan(2048);
        var options = new ExceptionFormatOptions { MaxOutputLength = 1024 };
        var text = detail ? ExceptionHelper.GetExceptionDetail(error, options) : error.FormatMessage(options);
        text.Length.ShouldBeLessThanOrEqualTo(1024);
        text.ShouldContain("异常堆栈：");
        text.ShouldContain(stack.Substring(0, 64));
        text.ShouldContain("[已截断：输出长度上限]");
        text.ShouldNotContain("[已截断：深度上限]");
        text.ShouldNotContain(stack);
    }

    /// <summary>
    /// 验证详情分别保留来源、原始抛出方法及内部异常消息。
    /// </summary>
    [Fact]
    public void Details_PreserveSourceThrowingMethodAndInnerMessage()
    {
        var error = Should.Throw<InvalidOperationException>(ThrowDiagnostic);
        var text = ExceptionHelper.GetExceptionDetail(error);
        text.ShouldContain("异常源：diagnostic-source");
        text.ShouldContain("异常方法：" + nameof(ThrowDiagnostic));
        text.ShouldContain("异常消息：outer-diagnostic");
        text.ShouldContain("内部异常：");
        text.ShouldContain("异常消息：inner-diagnostic");
    }

    /// <summary>
    /// 验证长消息受输出预算约束并带有截断标记。
    /// </summary>
    [Theory]
    [InlineData(64)]
    [InlineData(128)]
    [InlineData(1024)]
    public void LongMessages_AreTruncatedWithinConfiguredLength(int limit)
    {
        var exception = new Exception(new string('x', 1000000));
        var options = new ExceptionFormatOptions { MaxOutputLength = limit };
        var message = exception.FormatMessage(options);
        message.Length.ShouldBeLessThanOrEqualTo(limit);
        message.ShouldContain("[已截断：输出长度上限]");
        ExceptionHelper.GetExceptionDetail(exception, options).Length.ShouldBeLessThanOrEqualTo(limit);
    }

    /// <summary>
    /// 验证聚合摘要和节点预算不会提前读取未访问的消息。
    /// </summary>
    [Fact]
    public void AggregateSummary_DoesNotEvaluateAggregateMessageOrUnvisitedChildren()
    {
        var unvisited = new MessageTrapException();
        var branches = new[] { new Exception("first") }.Concat(Enumerable.Repeat<Exception>(unvisited, 10000)).ToArray();
        var aggregate = new AggregateMessageTrapException(branches);
        var text = aggregate.FormatMessage(new ExceptionFormatOptions
        {
            IncludeAggregateExceptions = true,
            MaxExceptionCount = 2
        });
        text.ShouldContain("first");
        text.ShouldContain("[已截断：异常节点数上限]");
    }

    /// <summary>
    /// 验证聚合分支显式展开并标记重复异常实例。
    /// </summary>
    [Fact]
    public void AggregateBranches_AreOptInAndDuplicateInstancesAreMarked()
    {
        var repeated = new Exception("shared");
        var root = new AggregateException(repeated, repeated, new Exception("second-branch"));
        root.FormatMessage().ShouldNotContain("second-branch");
        var text = root.FormatMessage(new ExceptionFormatOptions { IncludeAggregateExceptions = true });
        text.ShouldContain("second-branch");
        text.ShouldContain("[已出现：相同异常实例]");
    }

    /// <summary>
    /// 验证深度限制和缩进限制可以独立配置。
    /// </summary>
    [Fact]
    public void DepthAndIndentLimits_AreIndependent()
    {
        var root = new Exception("outer", new Exception("middle", new Exception("hidden")));
        var text = root.FormatMessage(new ExceptionFormatOptions { MaxDepth = 2, MaxIndentLevel = 0 });
        text.ShouldContain("middle");
        text.ShouldNotContain("hidden");
        text.ShouldContain("[已截断：深度上限]");
        foreach (var line in text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            line.StartsWith(" ").ShouldBeFalse();
    }

    /// <summary>
    /// 验证异常属性读取期间修改配置不影响已取得的快照。
    /// </summary>
    [Fact]
    public void Settings_AreSnapshottedBeforeReadingExceptionProperties()
    {
        var options = new ExceptionFormatOptions { MaxOutputLength = 1024 };
        var exception = new CallbackMessageException(() => { options.MaxOutputLength = 64; return new string('m', 200); });
        exception.FormatMessage(options).Length.ShouldBeGreaterThan(64);
        options.MaxOutputLength.ShouldBe(64);
    }

    /// <summary>
    /// 验证详情输出真实字段且不借助无界 ToString。
    /// </summary>
    [Fact]
    public void Details_ShowTypeAndHResultWithoutCallingExceptionToString()
    {
        var exception = new NoToStringException();
        var detail = ExceptionHelper.GetExceptionDetail(new Exception("outer", exception));
        detail.ShouldContain(typeof(NoToStringException).FullName);
        detail.ShouldContain("HResult：");
        detail.ShouldContain("记录时间：");
        detail.ShouldNotContain("异常发生时间");
    }

    /// <summary>
    /// 验证真实堆栈内容受隐藏开关控制，独立方法字段仍保留。
    /// </summary>
    [Fact]
    public void StackVisibility_UsesARealThrownException()
    {
        var error = CaptureOriginal();
        error.StackTrace.ShouldContain(nameof(ThrowOriginal));
        error.FormatMessage().ShouldContain(error.StackTrace);
        error.FormatMessage(true).ShouldNotContain(error.StackTrace);
        error.FormatMessage(true).ShouldNotContain("异常堆栈");
        ExceptionHelper.GetExceptionDetail(error, new ExceptionFormatOptions { HideStackTrace = true })
            .ShouldNotContain(error.StackTrace);
    }

    /// <summary>
    /// 验证重抛保留异常实例和原始抛出位置。
    /// </summary>
    [Fact]
    public void Rethrow_PreservesIdentityAndOriginalLocation()
    {
        var first = CaptureOriginal();
        Should.Throw<InvalidOperationException>(() => first.ReThrow()).ShouldBeSameAs(first);
        first.StackTrace.ShouldContain(nameof(ThrowOriginal));
        var second = CaptureOriginal();
        Should.Throw<InvalidOperationException>(() => ExceptionHelper.PrepareForRethrow(second)).ShouldBeSameAs(second);
        second.StackTrace.ShouldContain(nameof(ThrowOriginal));
    }

    /// <summary>
    /// 验证空异常、空配置和非法预算的边界契约。
    /// </summary>
    [Fact]
    public void NullAndInvalidOptions_HaveExplicitContracts()
    {
        ((Exception)null).FormatMessage().ShouldBe(string.Empty);
        Should.Throw<ArgumentNullException>(() => ExceptionHelper.GetExceptionDetail(null));
        Should.Throw<ArgumentNullException>(() => new Exception().FormatMessage((ExceptionFormatOptions)null));
        var invalid = new[]
        {
            new ExceptionFormatOptions { MaxDepth = 0 },
            new ExceptionFormatOptions { MaxExceptionCount = 0 },
            new ExceptionFormatOptions { MaxOutputLength = 63 },
            new ExceptionFormatOptions { MaxIndentLevel = -1 }
        };
        foreach (var options in invalid)
            Should.Throw<ArgumentOutOfRangeException>(() => new Exception().FormatMessage(options));
    }

    /// <summary>
    /// 捕获真实的原始抛出位置。
    /// </summary>
    private static Exception CaptureOriginal()
    {
        try { ThrowOriginal(); }
        catch (Exception exception) { return exception; }
        throw new InvalidOperationException("unreachable");
    }

    /// <summary>
    /// 提供不会被内联移除的原始抛出方法。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOriginal() => throw new InvalidOperationException("original");

    /// <summary>
    /// 通过不可内联且保留返回路径的递归生成真实长堆栈。
    /// </summary>
    /// <param name="depth">剩余递归层数。</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDeep(int depth)
    {
        if (depth == 0) throw new InvalidOperationException("deep");
        try { ThrowDeep(depth - 1); }
        finally { GC.KeepAlive(depth); }
    }

    /// <summary>
    /// 提供可直接核对来源与内部异常的原始抛出位置。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDiagnostic() => throw new InvalidOperationException(
        "outer-diagnostic", new ArgumentException("inner-diagnostic")) { Source = "diagnostic-source" };

    /// <summary>
    /// 检测预算外异常消息是否被提前读取。
    /// </summary>
    private sealed class MessageTrapException : Exception
    {
        /// <inheritdoc />
        public override string Message => throw new InvalidOperationException("unvisited message");
    }

    /// <summary>
    /// 检测格式化是否读取会展开全部子消息的聚合属性。
    /// </summary>
    private sealed class AggregateMessageTrapException : AggregateException
    {
        /// <summary>
        /// 初始化用于按需读取测试的聚合分支。
        /// </summary>
        /// <param name="exceptions">按遍历顺序排列的子异常。</param>
        internal AggregateMessageTrapException(params Exception[] exceptions) : base(exceptions) { }
        /// <inheritdoc />
        public override string Message => throw new InvalidOperationException("aggregate message");
    }

    /// <summary>
    /// 检测格式化是否绕过预算调用异常整体字符串化。
    /// </summary>
    private sealed class NoToStringException : Exception
    {
        /// <inheritdoc />
        public override string ToString() => throw new InvalidOperationException("ToString must not run");
    }

    /// <summary>
    /// 在读取消息时触发配置变更以验证快照隔离。
    /// </summary>
    private sealed class CallbackMessageException : Exception
    {
        /// <summary>
        /// 读取消息时执行的测试回调。
        /// </summary>
        private readonly Func<string> _message;
        /// <summary>
        /// 初始化消息读取回调。
        /// </summary>
        /// <param name="message">产生消息并执行测试副作用的回调。</param>
        internal CallbackMessageException(Func<string> message) => _message = message;
        /// <inheritdoc />
        public override string Message => _message();
    }
}
