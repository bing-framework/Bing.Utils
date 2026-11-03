using System.Threading;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Bing.Exceptions;

namespace Bing.Utils.Tests.Bing.Exceptions;

/// <summary>
/// 验证各参数重载转发顺序及批量执行的失败隔离。
/// </summary>
public class TryOverloadTests
{
    /// <summary>
    /// 获取全部现有工厂家族的参数个数与成功/失败组合。
    /// </summary>
    public static IEnumerable<object[]> FactoryCases()
    {
        foreach (var family in new[] { "Create", "Invoke", "CreateFromTask", "InvokeAsync" })
            for (var arity = 0; arity <= 16; arity++)
                foreach (var fail in new[] { false, true })
                    yield return new object[] { family, arity, fail };
    }

    /// <summary>
    /// 验证各工厂参数按顺序传递一次，并保留原故障。
    /// </summary>
    [Theory]
    [MemberData(nameof(FactoryCases))]
    public async Task Factories_ForwardArgumentsOnceAndPreserveFault(string family, int arity, bool fail)
    {
        // 旧同步桥接明确不支持 xUnit 的同步上下文，使用默认上下文验证其支持路径。
        await Task.Run(async () =>
        {
            var original = new InvalidOperationException("factory");
            var calls = 0;
            var expected = Enumerable.Range(1, arity).ToArray();
            Func<int[], int> record = values =>
            {
                calls++;
                values.ShouldBe(expected);
                if (fail) throw original;
                return 123;
            };
            var operation = CreateDelegate(arity, record, family == "CreateFromTask", family == "Invoke" || family == "InvokeAsync");
            var method = typeof(Try).GetMethods().Single(m => m.Name == family &&
                m.GetGenericArguments().Length == arity + (family == "Create" || family == "CreateFromTask" ? 1 : 0));
            if (method.IsGenericMethodDefinition)
                method = method.MakeGenericMethod(Enumerable.Repeat(typeof(int), method.GetGenericArguments().Length).ToArray());
            var arguments = new List<object> { family == "InvokeAsync" ? WrapTask(operation) : operation };
            arguments.AddRange(expected.Cast<object>());
            arguments.Add("cause");
            if (family == "CreateFromTask") arguments.Add(CancellationToken.None);
            var result = method.Invoke(null, arguments.ToArray());
            if (result is Task<TryAction> pending) result = await pending;
            calls.ShouldBe(1);
            if (result is Try<int> value)
            {
                value.IsFailure.ShouldBe(fail);
                if (fail)
                {
                    value.Exception.InnerException.ShouldBeSameAs(original);
                    value.Exception.Cause.ShouldBe("cause");
                }
                else value.Value.ShouldBe(123);
            }
            else
            {
                var action = (TryAction)result;
                action.IsFailure.ShouldBe(fail);
                if (fail)
                {
                    action.Exception.InnerException.ShouldBeSameAs(original);
                    action.Cause.ShouldBe("cause");
                }
            }
        });
    }

    /// <summary>
    /// 获取全部批量重载的参数数量。
    /// </summary>
    public static IEnumerable<object[]> RangeArities() => Enumerable.Range(1, 16).Select(n => new object[] { n });

    /// <summary>
    /// 验证全部批量重载逐项隔离参数工厂和操作失败。
    /// </summary>
    /// <param name="arity">当前验证的操作参数数量。</param>
    [Theory]
    [MemberData(nameof(RangeArities))]
    public void Range_AllAritiesIsolateParameterAndActionFailures(int arity)
    {
        var original = new InvalidOperationException("item");
        var seen = new List<int>();
        var firstFactoryIndexes = new List<int>();
        Func<int[], int> record = values =>
        {
            seen.Add(values[0]);
            for (var i = 1; i < values.Length; i++)
                values[i].ShouldBe((values[0] - 1) * 100 + i);
            if (arity == 1 && values[0] == 2) throw original;
            return 0;
        };
        var operation = CreateDelegate(arity, record, false, true);
        var method = typeof(Try).GetMethods().Single(m => m.Name == "InvokeRange" && m.GetGenericArguments().Length == arity)
            .MakeGenericMethod(Enumerable.Repeat(typeof(int), arity).ToArray());
        var arguments = new List<object> { new[] { 1, 2, 3 }, operation };
        for (var position = 1; position < arity; position++)
        {
            var captured = position;
            arguments.Add((Func<int, int>)(index =>
            {
                if (captured == 1)
                {
                    firstFactoryIndexes.Add(index);
                    if (index == 1) throw original;
                }
                return index * 100 + captured;
            }));
        }
        arguments.Add("item-cause");
        var sequence = (IEnumerable<TryAction>)method.Invoke(null, arguments.ToArray());
        seen.ShouldBeEmpty();
        var results = sequence.ToArray();
        results.Length.ShouldBe(3);
        results[0].IsSuccess.ShouldBeTrue();
        results[1].IsFailure.ShouldBeTrue();
        results[1].Exception.InnerException.ShouldBeSameAs(original);
        results[1].Cause.ShouldBe("item-cause");
        results[2].IsSuccess.ShouldBeTrue();
        seen.ShouldBe(arity == 1 ? new[] { 1, 2, 3 } : new[] { 1, 3 });
        if (arity > 1) firstFactoryIndexes.ShouldBe(new[] { 0, 1, 2 });

        if (arity > 1)
        {
            var actionCalls = new List<int>();
            var failingItem = 1;
            Func<int[], int> actionRecord = values =>
            {
                actionCalls.Add(values[0]);
                for (var i = 1; i < values.Length; i++)
                    values[i].ShouldBe((values[0] - 1) * 100 + i);
                if (values[0] == failingItem) throw original;
                return 0;
            };
            var actionOperation = CreateDelegate(arity, actionRecord, false, true);
            var actionArguments = new List<object> { new[] { 1, 2, 3 }, actionOperation };
            for (var position = 1; position < arity; position++)
            {
                var captured = position;
                actionArguments.Add((Func<int, int>)(index => index * 100 + captured));
            }
            actionArguments.Add("action-cause");
            var actionSequence = (IEnumerable<TryAction>)method.Invoke(null, actionArguments.ToArray());
            for (failingItem = 1; failingItem <= 3; failingItem++)
            {
                actionCalls.Clear();
                var actionResults = actionSequence.ToArray();
                var failureIndex = failingItem - 1;
                actionResults.Length.ShouldBe(3);
                actionResults[failureIndex].IsFailure.ShouldBeTrue();
                actionResults[failureIndex].Exception.InnerException.ShouldBeSameAs(original);
                actionResults[failureIndex].Cause.ShouldBe("action-cause");
                actionResults.Where((_, index) => index != failureIndex).All(item => item.IsSuccess).ShouldBeTrue();
                actionCalls.ShouldBe(new[] { 1, 2, 3 });
            }
        }
    }

    /// <summary>
    /// 验证首项、中间项和尾项的操作失败后仍执行后续项。
    /// </summary>
    /// <param name="failingIndex">抛出异常的零基输入索引。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Range_ContinuesAfterActionFailureAtAnyPosition(int failingIndex)
    {
        var calls = new List<int>();
        var exception = new InvalidOperationException("action");
        var result = Try.InvokeRange(new[] { 1, 2, 3 }, (int item, int argument) =>
        {
            argument.ShouldBe(item - 1);
            calls.Add(item);
            if (item == failingIndex + 1) throw exception;
        }, index => index, "action-cause").ToArray();
        result.Length.ShouldBe(3);
        result[failingIndex].IsFailure.ShouldBeTrue();
        result[failingIndex].Exception.InnerException.ShouldBeSameAs(exception);
        result[failingIndex].Cause.ShouldBe("action-cause");
        result.Where((_, index) => index != failingIndex).All(item => item.IsSuccess).ShouldBeTrue();
        calls.ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 验证首项、中间项和尾项参数工厂失败时保留后续结果。
    /// </summary>
    /// <param name="failingIndex">发生工厂故障的零基项索引。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Range_ContinuesAfterFactoryFailureAtAnyPosition(int failingIndex)
    {
        var calls = new List<int>();
        var exception = new InvalidOperationException("factory");
        var results = Try.InvokeRange(new[] { 1, 2, 3 }, (int item, int argument) =>
        {
            argument.ShouldBe(item - 1);
            calls.Add(item);
        }, index => index == failingIndex ? throw exception : index, "factory-cause").ToArray();
        results.Length.ShouldBe(3);
        results[failingIndex].Exception.InnerException.ShouldBeSameAs(exception);
        results[failingIndex].Cause.ShouldBe("factory-cause");
        results.Where((_, index) => index != failingIndex).All(result => result.IsSuccess).ShouldBeTrue();
        calls.ShouldBe(new[] { 1, 2, 3 }.Where(item => item != failingIndex + 1));
    }

    /// <summary>
    /// 验证批量配置在枚举前校验，空集合不执行操作。
    /// </summary>
    [Fact]
    public void Range_ValidatesConfigurationBeforeEnumeration()
    {
        Should.Throw<ArgumentNullException>(() => Try.InvokeRange<int>(null, _ => { }));
        Should.Throw<ArgumentNullException>(() => Try.InvokeRange(new[] { 1 }, (Action<int>)null));
        Should.Throw<ArgumentNullException>(() => Try.InvokeRange(new[] { 1 }, (int a, int b) => { }, null));
        Try.InvokeRange(Array.Empty<int>(), _ => throw new Exception()).ShouldBeEmpty();
    }

    /// <summary>
    /// 验证批量调用保持延迟执行并正确释放源枚举器。
    /// </summary>
    [Fact]
    public void Range_IsLazyAndDisposesTheSourceOnStopOrFailure()
    {
        var source = new ObservableEnumerable(false);
        var calls = 0;
        var sequence = Try.InvokeRange(source, _ => calls++);
        source.Moves.ShouldBe(0);
        using (var iterator = sequence.GetEnumerator())
        {
            iterator.MoveNext().ShouldBeTrue();
            calls.ShouldBe(1);
        }
        source.Disposed.ShouldBeTrue();
        source.Moves.ShouldBe(1);
        var failing = new ObservableEnumerable(true);
        var error = Should.Throw<InvalidOperationException>(() => Try.InvokeRange(failing, _ => { }).ToArray());
        error.ShouldBeSameAs(failing.Error);
        failing.Disposed.ShouldBeTrue();
    }

    /// <summary>
    /// 创建捕获实参顺序的强类型委托，避免每个参数个数复制一组测试。
    /// </summary>
    /// <param name="arity">输入参数数量。</param>
    /// <param name="record">验证调用参数并生成返回值的回调。</param>
    /// <param name="taskResult">是否将值包装为已完成任务。</param>
    /// <param name="action">是否丢弃返回值以构造同步操作。</param>
    /// <returns>对应参数个数的 Action 或 Func。</returns>
    private static Delegate CreateDelegate(int arity, Func<int[], int> record, bool taskResult, bool action)
    {
        var parameters = Enumerable.Range(0, arity).Select(i => Expression.Parameter(typeof(int), "arg" + i)).ToArray();
        Expression body = Expression.Invoke(Expression.Constant(record), Expression.NewArrayInit(typeof(int), parameters));
        if (taskResult)
            body = Expression.Call(typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(typeof(int)), body);
        if (action)
            body = Expression.Block(body, Expression.Empty());
        return Expression.Lambda(body, parameters).Compile();
    }

    /// <summary>
    /// 将任意强类型操作包装为对应 Task，以调用旧异步入口。
    /// </summary>
    /// <param name="operation">待包装的同步操作委托。</param>
    /// <returns>结果为该委托的已完成泛型任务。</returns>
    private static object WrapTask(Delegate operation) =>
        typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(operation.GetType()).Invoke(null, new object[] { operation });

    /// <summary>
    /// 记录源枚举推进和释放，支持在第二次推进时模拟源失败。
    /// </summary>
    private sealed class ObservableEnumerable : IEnumerable<int>
    {
        /// <summary>
        /// 是否在第二次推进时抛出源异常。
        /// </summary>
        private readonly bool _fail;
        /// <summary>
        /// 已推进的项数，用于验证延迟执行。
        /// </summary>
        internal int Moves;
        /// <summary>
        /// 源枚举器是否已释放。
        /// </summary>
        internal bool Disposed;
        /// <summary>
        /// 用于验证源故障身份的固定异常。
        /// </summary>
        internal readonly InvalidOperationException Error = new InvalidOperationException("source");
        /// <summary>
        /// 初始化可观测源枚举器。
        /// </summary>
        /// <param name="fail">是否在第二次推进时失败。</param>
        internal ObservableEnumerable(bool fail) => _fail = fail;
        /// <inheritdoc />
        public IEnumerator<int> GetEnumerator()
        {
            try
            {
                Moves++;
                yield return 1;
                Moves++;
                if (_fail) throw Error;
                yield return 2;
            }
            finally { Disposed = true; }
        }
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
