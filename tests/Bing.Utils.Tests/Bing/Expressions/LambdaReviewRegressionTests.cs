using System.Collections;
using System.Collections.Immutable;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Linq.Expressions;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Bing.Expressions;

/// <summary>
/// 覆盖搜索操作数识别及解释委托复用的语义与生命周期。
/// </summary>
public class LambdaReviewRegressionTests
{
    /// <summary>
    /// 验证 Contains 表达式的成员识别和搜索值提取。
    /// </summary>
    [Fact]
    public void Contains_MemberNames_SupportArrayInlineCollectionAndString()
    {
        int[] ids = { 1, 2 };
        var list = ids.ToList();
        var hashSet = new HashSet<int>(ids);
        var queryable = ids.AsQueryable();
        Expression<Func<Model, bool>>[] predicates =
        {
            x => ids.Contains(x.Number),
            x => list.Contains(x.Number),
            x => hashSet.Contains(x.Number),
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
            Lambdas.GetOperator(expression).ShouldBe(Operator.Contains);
        }
        Expression<Func<Model, bool>> text = x => "alphabet".Contains(x.Name);
        Lambdas.GetName(text).ShouldBe("Name");
        Lambdas.GetValue(text).ShouldBe("alphabet");
        Lambdas.GetOperator(text).ShouldBe(Operator.Contains);
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

    /// <summary>
    /// 验证标准 Contains 方法提取集合或搜索值。
    /// </summary>
    [Fact]
    public void GetValue_StandardContainsMethods_ExtractCollectionOrSearchValue()
    {
        int[] ids = { 1, 2 };
        var list = ids.ToList();
        var hashSet = new HashSet<int>(ids);
        Expression<Func<Model, bool>> arrayContains = model => ids.Contains(model.Number);
        Expression<Func<Model, bool>> listContains = model => list.Contains(model.Number);
        Expression<Func<Model, bool>> hashSetContains = model => hashSet.Contains(model.Number);
        Expression<Func<Model, bool>> enumerableContains = model => Enumerable.Contains(ids, model.Number);
        Expression<Func<Model, bool>> stringContains = model => "alphabet".Contains(model.Name);

        Lambdas.GetValue(arrayContains).ShouldBe(ids);
        Lambdas.GetValue(listContains).ShouldBe(list);
        Lambdas.GetValue(hashSetContains).ShouldBe(hashSet);
        Lambdas.GetValue(enumerableContains).ShouldBe(ids);
        Lambdas.GetValue(stringContains).ShouldBe("alphabet");

        Lambdas.GetOperator(arrayContains).ShouldBe(Operator.Contains);
        Lambdas.GetOperator(listContains).ShouldBe(Operator.Contains);
        Lambdas.GetOperator(hashSetContains).ShouldBe(Operator.Contains);
        Lambdas.GetOperator(enumerableContains).ShouldBe(Operator.Contains);
        Lambdas.GetOperator(stringContains).ShouldBe(Operator.Contains);
    }

    /// <summary>
    /// 验证 Contains 只提取与业务操作数无关的集合或值。
    /// </summary>
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

    /// <summary>
    /// 验证自定义 Contains 方法按方法语义执行。
    /// </summary>
    [Fact]
    public void GetValue_CustomContainsMethods_ExecutesTheMethod()
    {
        var methods = new CustomContainsMethods();
        Expression<Func<int>> numericResult = () => methods.Contains(3);
        Expression<Func<bool>> booleanResult = () => methods.Contains("needle");

        Lambdas.GetValue(numericResult).ShouldBe(6);
        methods.NumericCalls.ShouldBe(1);
        Lambdas.GetValue(booleanResult).ShouldBe(true);
        methods.StringCalls.ShouldBe(1);
        Lambdas.GetOperator(booleanResult).ShouldBeNull();
    }

    /// <summary>
    /// 验证可枚举对象的自定义 Contains 方法按方法语义执行。
    /// </summary>
    [Fact]
    public void GetValue_EnumerableCustomContainsMethods_ExecuteTheMethodInsteadOfSearchExtraction()
    {
        var enumerable = new EnumerableContainsMethods();
        Expression<Func<bool>> enumerableResult = () => enumerable.Contains("needle");

        Lambdas.GetValue(enumerableResult).ShouldBe(true);
        enumerable.ContainsCalls.ShouldBe(1);
        Lambdas.GetOperator(enumerableResult).ShouldBeNull();

        var list = new CustomContainsList { 1, 2 };
        Expression<Func<bool>> hiddenListResult = () => list.Contains(7);

        Lambdas.GetValue(hiddenListResult).ShouldBe(true);
        list.ContainsCalls.ShouldBe(1);
        Lambdas.GetOperator(hiddenListResult).ShouldBeNull();
    }

    /// <summary>
    /// 验证集合接口、队列和栈的 Contains 调用被识别。
    /// </summary>
    [Fact]
    public void Contains_RecognizesCollectionInterfacesQueueAndStack()
    {
        ICollection<int> collection = new List<int> { 1, 2 };
        IList<int> list = new List<int> { 1, 2 };
        var queue = new Queue<int>(new[] { 1, 2 });
        var stack = new Stack<int>(new[] { 1, 2 });
        var concreteCollection = new System.Collections.ObjectModel.Collection<int> { 1, 2 };
        IList legacyList = new ArrayList { 1, 2 };
        IDictionary dictionary = new Hashtable { [1] = "one" };
        Expression<Func<Model, bool>>[] expressions =
        {
            model => collection.Contains(model.Number),
            model => list.Contains(model.Number),
            model => queue.Contains(model.Number),
            model => stack.Contains(model.Number),
            model => concreteCollection.Contains(model.Number),
            model => legacyList.Contains(model.Number),
            model => dictionary.Contains(model.Number)
        };
        object[] sources = { collection, list, queue, stack, concreteCollection, legacyList, dictionary };

        for (var i = 0; i < expressions.Length; i++)
        {
            Lambdas.GetOperator(expressions[i]).ShouldBe(Operator.Contains);
            Lambdas.GetValue(expressions[i]).ShouldBeSameAs(sources[i]);
        }
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// 验证只读集合接口的 Contains 调用被识别。
    /// </summary>
    [Fact]
    public void Contains_RecognizesReadOnlySetInterface()
    {
        IReadOnlySet<int> set = new HashSet<int> { 1, 2 };
        Expression<Func<Model, bool>> expression = model => set.Contains(model.Number);

        Lambdas.GetOperator(expression).ShouldBe(Operator.Contains);
        Lambdas.GetValue(expression).ShouldBeSameAs(set);
    }
#endif

    /// <summary>
    /// 验证不可变哈希集合的 Contains 调用提取原集合。
    /// </summary>
    [Fact]
    public void Contains_RecognizesImmutableHashSetAndExtractsCollection()
    {
        var source = ImmutableHashSet.Create(1, 2);
        Expression<Func<Model, bool>> expression = model => source.Contains(model.Number);

        AssertCollectionSearch(expression, source);
    }

    /// <summary>
    /// 验证不可变数组的 Contains 调用提取原集合。
    /// </summary>
    [Fact]
    public void Contains_RecognizesImmutableArrayAndExtractsCollection()
    {
        var source = ImmutableArray.Create(1, 2);
        Expression<Func<Model, bool>> expression = model => source.Contains(model.Number);

        AssertCollectionSearch(expression, source);
    }

    /// <summary>
    /// 验证有序集合的 Contains 调用提取原集合。
    /// </summary>
    [Fact]
    public void Contains_RecognizesSortedSetAndExtractsCollection()
    {
        var source = new SortedSet<int> { 1, 2 };
        Expression<Func<Model, bool>> expression = model => source.Contains(model.Number);

        AssertCollectionSearch(expression, source);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// 验证冻结集合的 Contains 调用提取原集合。
    /// </summary>
    [Fact]
    public void Contains_RecognizesFrozenSetAndExtractsCollection()
    {
        var source = new[] { 1, 2 }.ToFrozenSet();
        Expression<Func<Model, bool>> expression = model => source.Contains(model.Number);

        AssertCollectionSearch(expression, source);
    }
#endif

    /// <summary>
    /// 验证依赖查询参数的自定义 Contains 调用不作为集合搜索处理。
    /// </summary>
    [Fact]
    public void GetValue_CustomContainsWithQueryParameter_IsNotTreatedAsCollectionSearch()
    {
        var methods = new CustomContainsMethods();
        Expression<Func<Model, bool>> expression = model => methods.Contains(model.Name);

        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(expression));
        Lambdas.GetOperator(expression).ShouldBeNull();
        methods.StringCalls.ShouldBe(0);
    }

    /// <summary>
    /// 验证表达式求值器复用编译结果、读取实时状态并传播原始异常。
    /// </summary>
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

    /// <summary>
    /// 验证表达式编译失败可重复且不会影响后续求值。
    /// </summary>
    [Fact]
    public void Evaluator_CompilationFailureRemainsDeterministic()
    {
        // 未定义的跳转目标无法编译；重复访问失败条目不能死锁或影响其他表达式。
        var invalid = Expression.Goto(Expression.Label(typeof(int)), Expression.Constant(1), typeof(int));
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(invalid));
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(invalid));
        Lambdas.GetValue(Expression.Add(Expression.Constant(1), Expression.Constant(2))).ShouldBe(3);
    }

    /// <summary>
    /// 验证并发求值时每次调用仍执行表达式。
    /// </summary>
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

    /// <summary>
    /// 验证求值器缓存弱引用不会阻止闭包和表达式回收。
    /// </summary>
    [Fact]
    public void Evaluator_WeakKeyReleasesClosureAndExpression()
    {
        var references = CreateWeakEvaluator();
        Collect(references);
        references.All(reference => !reference.IsAlive).ShouldBeTrue();
    }

    /// <summary>
    /// 验证 Contains 分类缓存不阻止闭包和表达式回收。
    /// </summary>
    [Fact]
    public void ContainsClassifier_WeakKeyReleasesClosureAndExpression()
    {
        var references = CreateWeakContainsClassifier();
        Collect(references);
        references.All(reference => !reference.IsAlive).ShouldBeTrue();
    }

    /// <summary>
    /// 验证并发调用保持集合搜索语义。
    /// </summary>
    [Fact]
    public void ContainsClassifier_ConcurrentCallsPreserveCollectionSearch()
    {
        var source = new System.Collections.ObjectModel.Collection<int> { 1, 2 };
        Expression<Func<Model, bool>> expression = model => source.Contains(model.Number);
        var results = new bool[256];

        Parallel.For(0, results.Length, index => results[index] =
            Lambdas.GetOperator(expression) == Operator.Contains && ReferenceEquals(Lambdas.GetValue(expression), source));

        results.All(result => result).ShouldBeTrue();
    }

    /// <summary>
    /// 创建用于检验分类缓存回收行为的弱引用。
    /// </summary>
    /// <returns>指向测试对象和表达式体的弱引用。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateWeakContainsClassifier()
    {
        var source = new EnumerableContainsMethods();
        Expression<Func<bool>> expression = () => source.Contains("needle");
        Lambdas.GetValue(expression).ShouldBe(true);
        return new[] { new WeakReference(source), new WeakReference(expression.Body) };
    }

    /// <summary>
    /// 创建用于检验求值器缓存回收行为的弱引用。
    /// </summary>
    /// <returns>指向闭包对象和表达式体的弱引用。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateWeakEvaluator()
    {
        var holder = new ValueHolder();
        Expression<Func<Model, bool>> expression = x => x.Number == holder.Read();
        Lambdas.GetValue(expression);
        return new[] { new WeakReference(holder), new WeakReference(expression.Body) };
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// 验证求值器缓存弱引用不会阻止可回收类型释放。
    /// </summary>
    [Fact]
    public void Evaluator_WeakKeyReleasesCollectibleType()
    {
        var references = CreateCollectibleEvaluator();
        Collect(references);
        references.All(reference => !reference.IsAlive).ShouldBeTrue();
    }

    /// <summary>
    /// 创建用于检验可回收类型释放行为的弱引用。
    /// </summary>
    /// <returns>指向程序集、实例和表达式的弱引用。</returns>
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

    /// <summary>
    /// 执行垃圾回收，直到弱引用目标释放或达到尝试次数上限。
    /// </summary>
    /// <param name="references">需要检查的弱引用集合。</param>
    private static void Collect(WeakReference[] references)
    {
        for (var i = 0; i < 20 && references.Any(reference => reference.IsAlive); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    /// <summary>
    /// 断言集合搜索的成员名、运算符和集合值。
    /// </summary>
    /// <param name="expression">待检查的搜索表达式。</param>
    /// <param name="source">表达式引用的集合实例。</param>
    private static void AssertCollectionSearch(Expression<Func<Model, bool>> expression, object source)
    {
        Lambdas.GetName(expression).ShouldBe("Number");
        Lambdas.GetOperator(expression).ShouldBe(Operator.Contains);
        Equals(Lambdas.GetValue(expression), source).ShouldBeTrue();
    }

    /// <summary>
    /// 表达式回归测试使用的查询模型。
    /// </summary>
    public class Model
    {
        /// <summary>
        /// 获取或设置数值字段。
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// 获取或设置名称字段。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置字符串数组。
        /// </summary>
        public string[] Values { get; set; }

        /// <summary>
        /// 获取或设置名称集合。
        /// </summary>
        public List<string> Names { get; set; }
    }

    /// <summary>
    /// 提供可观察调用次数和异常状态的测试值。
    /// </summary>
    public class ValueHolder
    {
        /// <summary>
        /// 保存读取方法返回的当前数值。
        /// </summary>
        public int Value;

        /// <summary>
        /// 记录读取方法的调用次数。
        /// </summary>
        public int Reads;

        /// <summary>
        /// 读取时需要抛出的异常；为 null 时正常返回数值。
        /// </summary>
        public Exception Failure;

        /// <summary>
        /// 记录调用次数后返回数值或抛出配置的异常。
        /// </summary>
        /// <returns>当前保存的数值。</returns>
        public int Read()
        {
            Interlocked.Increment(ref Reads);
            if (Failure != null)
                throw Failure;
            return Value;
        }
    }

    /// <summary>
    /// 提供具有自定义 Contains 名称的方法的测试对象。
    /// </summary>
    private sealed class CustomContainsMethods
    {
        /// <summary>
        /// 获取数值重载的调用次数。
        /// </summary>
        public int NumericCalls { get; private set; }

        /// <summary>
        /// 获取字符串重载的调用次数。
        /// </summary>
        public int StringCalls { get; private set; }

        /// <summary>
        /// 将数值参数乘以二并记录调用次数。
        /// </summary>
        /// <param name="value">待处理的数值。</param>
        /// <returns>数值参数的两倍。</returns>
        public int Contains(int value)
        {
            NumericCalls++;
            return value * 2;
        }

        /// <summary>
        /// 判断字符串是否等于预设文本并记录调用次数。
        /// </summary>
        /// <param name="value">待比较的字符串。</param>
        /// <returns>字符串等于预设文本时为 true，否则为 false。</returns>
        public bool Contains(string value)
        {
            StringCalls++;
            return value == "needle";
        }
    }

    /// <summary>
    /// 提供自定义 Contains 方法并实现泛型可枚举接口的测试对象。
    /// </summary>
    private sealed class EnumerableContainsMethods : IEnumerable<int>
    {
        /// <summary>
        /// 获取自定义 Contains 方法的调用次数。
        /// </summary>
        public int ContainsCalls { get; private set; }

        /// <summary>
        /// 判断字符串是否等于预设文本并记录调用次数。
        /// </summary>
        /// <param name="value">待比较的字符串。</param>
        /// <returns>字符串等于预设文本时为 true，否则为 false。</returns>
        public bool Contains(string value)
        {
            ContainsCalls++;
            return value == "needle";
        }

        /// <inheritdoc />
        public IEnumerator<int> GetEnumerator()
        {
            yield break;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// 隐藏列表原有 Contains 方法并记录自定义重载调用的测试集合。
    /// </summary>
    private sealed class CustomContainsList : List<int>
    {
        /// <summary>
        /// 获取自定义 Contains 方法的调用次数。
        /// </summary>
        public int ContainsCalls { get; private set; }

        /// <summary>
        /// 判断数值是否为预设值并记录调用次数。
        /// </summary>
        /// <param name="value">待比较的数值。</param>
        /// <returns>数值等于预设值时为 true，否则为 false。</returns>
        public new bool Contains(int value)
        {
            ContainsCalls++;
            return value == 7;
        }
    }
}
