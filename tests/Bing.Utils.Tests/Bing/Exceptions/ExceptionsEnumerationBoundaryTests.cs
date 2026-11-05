using System.Collections;
using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证批量调用源枚举器的失败传播、释放和重新枚举契约。
/// </summary>
public class ExceptionsEnumerationBoundaryTests
{
    /// <summary>
    /// 获取枚举器时抛出的源异常直接传播，不执行任何操作。
    /// </summary>
    [Fact]
    public void Range_GetEnumeratorThrows_PropagatesWithoutInvokingAction()
    {
        var source = new GetEnumeratorFailureEnumerable();
        var actionCalls = 0;

        var error = Should.Throw<InvalidOperationException>(() =>
            Try.InvokeRange(source, _ => actionCalls++).ToArray());

        error.ShouldBeSameAs(source.Error);
        actionCalls.ShouldBe(0);
    }

    /// <summary>
    /// 读取 Current 失败时传播原异常并释放已经获取的枚举器。
    /// </summary>
    [Fact]
    public void Range_CurrentThrows_PropagatesAndDisposesEnumerator()
    {
        var source = new CurrentFailureEnumerable();
        var actionCalls = 0;

        var error = Should.Throw<InvalidOperationException>(() =>
            Try.InvokeRange(source, _ => actionCalls++).ToArray());

        error.ShouldBeSameAs(source.Enumerator.Error);
        source.Enumerator.DisposeCalls.ShouldBe(1);
        actionCalls.ShouldBe(0);
    }

    /// <summary>
    /// 重复枚举重新执行操作，并在每轮从零开始生成参数索引。
    /// </summary>
    [Fact]
    public void Range_Reenumeration_RepeatsOperationsAndResetsIndexes()
    {
        var observed = new List<string>();
        var sequence = Try.InvokeRange(new[] { "A", "B" },
            (string item, int index) => observed.Add($"{item}:{index}"),
            index => index);

        sequence.Select(result => result.IsSuccess).ShouldBe(new[] { true, true });
        sequence.Select(result => result.IsSuccess).ShouldBe(new[] { true, true });

        observed.ShouldBe(new[] { "A:0", "B:1", "A:0", "B:1" });
    }

    /// <summary>
    /// 详情格式化的空选项明确报告参数名。
    /// </summary>
    [Fact]
    public void GetExceptionDetail_NullOptions_ThrowsArgumentNullException()
    {
        var error = Should.Throw<ArgumentNullException>(() =>
            ExceptionHelper.GetExceptionDetail(new InvalidOperationException("failure"), null));

        error.ParamName.ShouldBe("options");
    }

    private sealed class GetEnumeratorFailureEnumerable : IEnumerable<int>
    {
        internal InvalidOperationException Error { get; } = new InvalidOperationException("GetEnumerator failed");

        public IEnumerator<int> GetEnumerator() => throw Error;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class CurrentFailureEnumerable : IEnumerable<int>
    {
        internal CurrentFailureEnumerator Enumerator { get; } = new CurrentFailureEnumerator();

        public IEnumerator<int> GetEnumerator() => Enumerator;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class CurrentFailureEnumerator : IEnumerator<int>
    {
        internal InvalidOperationException Error { get; } = new InvalidOperationException("Current failed");
        internal int DisposeCalls { get; private set; }

        public int Current => throw Error;

        object IEnumerator.Current => Current;

        public bool MoveNext() => true;

        public void Reset() => throw new NotSupportedException();

        public void Dispose() => DisposeCalls++;
    }
}
