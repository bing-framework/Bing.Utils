using System.Linq.Expressions;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Bing.Expressions;

/// <summary>覆盖搜索操作数识别及解释委托复用的语义与生命周期。</summary>
public class LambdaReviewRegressionTests
{
    [Fact]
    public void Contains_MemberNames_SupportArrayInlineCollectionAndString()
    {
        int[] ids = { 1, 2 };
        var list = ids.ToList();
        var queryable = ids.AsQueryable();
        Expression<Func<Model, bool>>[] predicates =
        {
            x => ids.Contains(x.Number),
            x => list.Contains(x.Number),
            x => new[] { 1, 2 }.Contains(x.Number),
            x => new List<int> { 1, 2 }.Contains(x.Number),
            x => queryable.Contains(x.Number),
            x => ids.Contains(x.Number, EqualityComparer<int>.Default)
        };
        foreach (var expression in predicates)
        {
            Lambdas.GetName(expression).ShouldBe("Number");
            Lambdas.GetLastName(expression).ShouldBe("Number");
            Lambdas.GetMember(expression).Name.ShouldBe("Number");
        }
        Expression<Func<Model, bool>> text = x => "alphabet".Contains(x.Name);
        Lambdas.GetName(text).ShouldBe("Name");
        Lambdas.GetValue(text).ShouldBe("alphabet");
        string[] first = { "a" }, second = { "b" };
        Expression<Func<Model, bool>> conditional = x => (x.Number > 0 ? first : second).Contains(x.Name);
        Lambdas.GetName(conditional).ShouldBe("Name");
        Lambdas.GetLastName(conditional).ShouldBe("Name");
        Expression<Func<Model, bool>> dependentList = x => x.Names.Contains(x.Name);
        Lambdas.GetName(dependentList).ShouldBe("Name");
        Lambdas.GetLastName(dependentList).ShouldBe("Name");
        var value = "a";
        Expression<Func<Model, bool>> collection = x => x.Values.Contains(value);
        Lambdas.GetName(collection).ShouldBe("Values");
        Expression<Func<Model, bool>>[] chained =
        {
            x => x.Values.AsQueryable().Contains("a"),
            x => x.Values.Where(v => v.Length > 0).Contains("a")
        };
        foreach (var expression in chained)
        {
            Lambdas.GetName(expression).ShouldBe("Values");
            Lambdas.GetLastName(expression).ShouldBe("Values");
            Lambdas.GetValue(expression).ShouldBe("a");
        }
    }

    [Fact]
    public void Contains_OnlySelectsBusinessOperands()
    {
        Expression<Func<Model, bool>> dependent = x => Enumerable.Contains(x.Values, x.Name, StringComparer.Ordinal);
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(dependent));
        Expression<Func<Model, bool>> independentValue = x => Enumerable.Contains(x.Values, "a", StringComparer.Ordinal);
        Lambdas.GetValue(independentValue).ShouldBe("a");
        string[] values = { "a", "b" };
        Expression<Func<Model, bool>> independentSource = x => values.Contains(x.Name, StringComparer.Ordinal);
        Lambdas.GetValue(independentSource).ShouldBeSameAs(values);
        Expression<Func<Model, bool>> queryable = x => x.Values.AsQueryable().Contains(x.Name, StringComparer.Ordinal);
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(queryable));
    }

    [Fact]
    public void Evaluator_ReusesCodeButReadsLiveStateAndPropagatesExceptions()
    {
        var holder = new ValueHolder { Value = 2 };
        Expression<Func<Model, bool>> expression = x => x.Number == holder.Read();
        Lambdas.GetValue(expression).ShouldBe(2);
        holder.Value = 9;
        Lambdas.GetValue(expression).ShouldBe(9);
        holder.Reads.ShouldBe(2);
        holder.Failure = new InvalidOperationException("original");
        Should.Throw<InvalidOperationException>(() => Lambdas.GetValue(expression)).ShouldBeSameAs(holder.Failure);
        holder.Failure = null;
        Lambdas.GetValue(expression).ShouldBe(9);
        holder.Reads.ShouldBe(4);
    }

    [Fact]
    public void Evaluator_CompilationFailureRemainsDeterministic()
    {
        // 未定义的跳转目标无法编译；重复访问失败条目不能死锁或影响其他表达式。
        var invalid = Expression.Goto(Expression.Label(typeof(int)), Expression.Constant(1), typeof(int));
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(invalid));
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(invalid));
        Lambdas.GetValue(Expression.Add(Expression.Constant(1), Expression.Constant(2))).ShouldBe(3);
    }

    [Fact]
    public void Evaluator_ConcurrentCallsExecuteOncePerCall()
    {
        var holder = new ValueHolder { Value = 7 };
        Expression<Func<Model, bool>> expression = x => x.Number == holder.Read();
        var results = new object[256];
        Parallel.For(0, results.Length, i => results[i] = Lambdas.GetValue(expression));
        results.All(value => Equals(value, 7)).ShouldBeTrue();
        holder.Reads.ShouldBe(results.Length);
    }

    [Fact]
    public void Evaluator_WeakKeyReleasesClosureAndExpression()
    {
        var references = CreateWeakEvaluator();
        Collect(references);
        references.All(reference => !reference.IsAlive).ShouldBeTrue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateWeakEvaluator()
    {
        var holder = new ValueHolder();
        Expression<Func<Model, bool>> expression = x => x.Number == holder.Read();
        Lambdas.GetValue(expression);
        return new[] { new WeakReference(holder), new WeakReference(expression.Body) };
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void Evaluator_WeakKeyReleasesCollectibleType()
    {
        var references = CreateCollectibleEvaluator();
        Collect(references);
        references.All(reference => !reference.IsAlive).ShouldBeTrue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateCollectibleEvaluator()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ValueEvaluator_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Derived", TypeAttributes.Public, typeof(ValueHolder));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        var type = builder.CreateTypeInfo().AsType();
        var instance = Activator.CreateInstance(type);
        var expression = Expression.Call(Expression.Constant(instance, type), typeof(ValueHolder).GetMethod(nameof(ValueHolder.Read)));
        Lambdas.GetValue(expression);
        return new[] { new WeakReference(type.Assembly), new WeakReference(instance), new WeakReference(expression) };
    }
#endif

    private static void Collect(WeakReference[] references)
    {
        for (var i = 0; i < 20 && references.Any(reference => reference.IsAlive); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    public class Model
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string[] Values { get; set; }
        public List<string> Names { get; set; }
    }

    public class ValueHolder
    {
        public int Value;
        public int Reads;
        public Exception Failure;
        public int Read()
        {
            Interlocked.Increment(ref Reads);
            if (Failure != null)
                throw Failure;
            return Value;
        }
    }
}
