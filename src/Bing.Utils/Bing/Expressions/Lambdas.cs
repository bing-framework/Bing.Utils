using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Bing.Extensions;
using Bing.Reflection;

namespace Bing.Expressions;
/// <summary>
/// Lambda表达式操作
/// </summary>
public static partial class Lambdas
{
    /// <summary>
    /// 随表达式回收的求值器缓存，不缓存执行结果。
    /// </summary>
    private static readonly ConditionalWeakTable<Expression, ValueEvaluator> ValueEvaluators = new();
    /// <summary>
    /// 随调用表达式回收的集合方法分类缓存。
    /// </summary>
    private static readonly ConditionalWeakTable<MethodCallExpression, CollectionContainsKind> CollectionContainsMethods = new();
    /// <summary>
    /// 当前运行时提供的只读集合接口类型；不支持时为 null。
    /// </summary>
    private static readonly Type ReadOnlySetInterface = typeof(IEnumerable<>).Assembly.GetType("System.Collections.Generic.IReadOnlySet`1");
    /// <summary>
    /// 集合 Contains 调用的分类结果。
    /// </summary>
    private sealed class CollectionContainsKind
    {
        /// <summary>
        /// 可按集合搜索处理的分类结果。
        /// </summary>
        internal static readonly CollectionContainsKind Supported = new(true);
        /// <summary>
        /// 需要按普通方法执行的分类结果。
        /// </summary>
        internal static readonly CollectionContainsKind Unsupported = new(false);
        /// <summary>
        /// 初始化一个 <see cref="CollectionContainsKind"/> 类型的实例。
        /// </summary>
        /// <param name="isSupported">为 true 时支持按集合搜索处理。</param>
        private CollectionContainsKind(bool isSupported) => IsSupported = isSupported;
        /// <summary>
        /// 获取是否支持集合搜索。
        /// </summary>
        internal bool IsSupported { get; }
    }
    /// <summary>
    /// 按需编译的表达式求值器缓存条目。
    /// </summary>
    private sealed class ValueEvaluator
    {
        /// <summary>
        /// 已成功编译、可重复执行的求值委托。
        /// </summary>
        private Func<object> _evaluator;
        /// <summary>
        /// 首次编译失败的异常信息，用于重现失败结果。
        /// </summary>
        private System.Runtime.ExceptionServices.ExceptionDispatchInfo _compilationError;
        /// <summary>
        /// 指示当前是否正在编译表达式求值器。
        /// </summary>
        private bool _compiling;
        /// <summary>
        /// 获取或初始化表达式的求值委托。
        /// </summary>
        /// <param name="expression">待编译的表达式。</param>
        /// <returns>每次执行时读取当前值的求值委托。</returns>
        internal Func<object> GetEvaluator(Expression expression)
        {
            var evaluator = System.Threading.Volatile.Read(ref _evaluator);
            if (evaluator != null)
                return evaluator;
            // 条目仅供内部使用；同一表达式首次编译串行，命中不进入锁。
            lock (this)
            {
                if (_evaluator != null)
                    return _evaluator;
                _compilationError?.Throw();
                if (_compiling)
                    throw new InvalidOperationException("表达式求值委托不支持递归初始化。");
                try
                {
                    _compiling = true;
                    evaluator = CompileValueEvaluator(expression);
                    System.Threading.Volatile.Write(ref _evaluator, evaluator);
                    return evaluator;
                }
                catch (Exception exception)
                {
                    _compilationError = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception);
                    throw;
                }
                finally
                {
                    _compiling = false;
                }
            }
        }
    }

    #region GetType(获取类型)
    /// <summary>
    /// 获取类型
    /// </summary>
    /// <param name="expression">表达式，范例：t => t.Name</param>
    /// <returns>表达式成员的类型；未找到成员时返回 null。</returns>
    public static Type GetType(Expression expression)
    {
        var memberExpression = GetMemberExpression(expression);
        return memberExpression?.Type;
    }

    #endregion

    #region GetMember(获取成员)
    /// <summary>
    /// 获取成员
    /// </summary>
    /// <param name="expression">表达式，范例：t => t.Name</param>
    /// <returns>表达式引用的成员；未找到成员时返回 null。</returns>
    public static MemberInfo GetMember(Expression expression)
    {
        var memberExpression = GetMemberExpression(expression);
        return memberExpression?.Member;
    }
    /// <summary>
    /// 获取成员表达式
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <param name="right">取表达式右侧，(l,r)=> l.LId == r.RId，设置为true，返回 RID</param>
    /// <returns>对应的成员表达式；无法识别时返回 null。</returns>
    public static MemberExpression GetMemberExpression(Expression expression, bool right = false)
    {
        if (expression == null)
            return null;
        switch (expression.NodeType)
        {
            case ExpressionType.Lambda:
                return GetMemberExpression(((LambdaExpression)expression).Body, right);

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Not:
                return GetMemberExpression(((UnaryExpression)expression).Operand, right);

            case ExpressionType.MemberAccess:
                return (MemberExpression)expression;

            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThanOrEqual:
                return GetMemberExpression(right
                    ? ((BinaryExpression)expression).Right
                    : ((BinaryExpression)expression).Left, right);

            case ExpressionType.Call:
                return GetMethodCallExpressionName(expression);
        }
        return null;
    }
    /// <summary>
    /// 获取方法调用表达式的成员名称
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns>表示查询成员的表达式；未找到时返回 null。</returns>
    private static MemberExpression GetMethodCallExpressionName(Expression expression)
    {
        var call = (MethodCallExpression)expression;
        if (TryGetSearchOperands(call, out var source, out var value))
        {
            // 实例集合保留原有的元素成员优先级；字符串及静态扩展调用优先接收者。
            var valueFirst = call.Object != null && Reflections.IsGenericCollection(source.Type);
            var first = valueFirst ? value : source;
            var second = valueFirst ? source : value;
            if (HasParameter(first))
            {
                var member = GetMemberExpression(first);
                if (member != null)
                    return member;
            }
            if (HasParameter(second))
            {
                var member = GetMemberExpression(second);
                if (member != null)
                    return member;
            }
        }
        if (call.Object != null)
            return GetMemberExpression(call.Object);
        // AsQueryable/Where 等扩展调用的接收者保存在参数中，沿查询相关操作数继续解析。
        foreach (var argument in call.Arguments)
        {
            if (!HasParameter(argument))
                continue;
            var member = GetMemberExpression(argument);
            if (member != null)
                return member;
        }
        return null;
    }

    #endregion

    #region GetName(获取成员名称)
    /// <summary>
    /// 获取成员名称。
    /// </summary>
    /// <remarks>范例：t => t.A.Name，返回 A.Name</remarks>
    /// <param name="expression">表达式，范例：t => t.Name</param>
    /// <returns>成员路径名称；无法识别时返回空字符串。</returns>
    public static string GetName(Expression expression)
    {
        var memberExpression = GetMemberExpression(expression);
        return GetMemberName(memberExpression);
    }
    /// <summary>
    /// 获取成员名称
    /// </summary>
    /// <param name="memberExpression">表达式</param>
    /// <returns>成员路径名称；表达式为空时返回空字符串。</returns>
    public static string GetMemberName(MemberExpression memberExpression)
    {
        if (memberExpression == null)
            return string.Empty;
        var result = memberExpression.ToString();
        return result.Substring(result.IndexOf(".", StringComparison.Ordinal) + 1);
    }

    #endregion

    #region GetNames(获取名称列表)
    /// <summary>
    /// 获取名称列表。
    /// </summary>
    /// <remarks>范例：t => new object[] {t.A.B,t.C}，返回 A.B,C</remarks>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="expression">属性集合表达式，范例：t => new object[]{t.A,t.B}</param>
    /// <returns>表达式引用的成员路径名称列表。</returns>
    public static List<string> GetNames<T>(Expression<Func<T, object[]>> expression)
    {
        var result = new List<string>();
        if (expression == null)
            return result;
        if (!(expression.Body is NewArrayExpression arrayExpression))
            return result;
        foreach (var each in arrayExpression.Expressions)
            AddName(result, each);
        return result;
    }
    /// <summary>
    /// 添加名称
    /// </summary>
    /// <param name="result">名称列表</param>
    /// <param name="expression">表达式</param>
    private static void AddName(List<string> result, Expression expression)
    {
        var name = GetName(expression);
        if (name.IsEmpty())
            return;
        result.Add(name);
    }

    #endregion

    #region GetLastName(获取最后一级成员名称)
    /// <summary>
    /// 获取最后一级成员名称。
    /// </summary>
    /// <remarks>范例：t => t.Name，返回 Name</remarks>
    /// <param name="expression">表达式，范例：t => t.Name</param>
    /// <param name="right">取表达式右侧，(l,r)=> l.LId == r.RId，设置为true，返回 RID</param>
    /// <returns>成员路径末级名称；表达式为空时返回空字符串。</returns>
    public static string GetLastName(Expression expression, bool right = false)
    {
        var memberExpression = GetMemberExpression(expression, right);
        if (memberExpression == null)
            return string.Empty;
        if (IsValueExpression(memberExpression))
            return string.Empty;
        string result = memberExpression.ToString();
        return result.Substring(result.LastIndexOf(".", StringComparison.Ordinal) + 1);
    }
    /// <summary>
    /// 是否值表达式
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns>表达式为常量或基于常量的成员访问链时为 true，否则为 false。</returns>
    private static bool IsValueExpression(Expression expression)
    {
        if (expression == null)
            return false;
        switch (expression.NodeType)
        {
            case ExpressionType.MemberAccess:
                return IsValueExpression(((MemberExpression)expression).Expression);

            case ExpressionType.Constant:
                return true;
        }
        return false;
    }

    #endregion

    #region GetLastNames(获取最后一级成员名称列表)
    /// <summary>
    /// 获取最后一级成员名称列表。
    /// </summary>
    /// <remarks>范例：t => new object[] {t.A.B,t.C}，返回 B,C</remarks>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="expression">属性集合表达式，范例：t => new object[] {t.A,t.B}</param>
    /// <returns>表达式引用的成员末级名称列表。</returns>
    public static List<string> GetLastNames<T>(Expression<Func<T, object[]>> expression)
    {
        var result = new List<string>();
        if (expression == null)
            return result;
        if (!(expression.Body is NewArrayExpression arrayExpression))
            return result;
        foreach (var each in arrayExpression.Expressions)
        {
            var name = GetLastName(each);
            if (string.IsNullOrWhiteSpace(name) == false)
                result.Add(name);
        }
        return result;
    }

    #endregion

    #region GetValue(获取值)
    /// <summary>
    /// 获取查询表达式中的值。
    /// </summary>
    /// <remarks>范例：t => t.Name == "A"，返回 A。</remarks>
    /// <param name="expression">表达式，范例：t => t.Name == "A"</param>
    /// <remarks>
    /// 参数无关的值子表达式会按表达式语义计算。直接布尔查询成员及其一元否定保留历史查询值 true/false；无法从参数依赖表达式中提取值时抛出 <see cref="NotSupportedException"/>。
    /// </remarks>
    /// <returns>提取或计算得到的查询值；空表达式或值为空时返回 null。</returns>
    public static object GetValue(Expression expression)
    {
        if (expression == null)
            return null;
        switch (expression.NodeType)
        {
            case ExpressionType.Lambda:
                return GetValue(((LambdaExpression)expression).Body);

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                var unaryExpression = (UnaryExpression)expression;
                return HasParameter(unaryExpression.Operand)
                    ? GetValue(unaryExpression.Operand)
                    : EvaluateValueExpression(expression);

            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
                return GetComparisonValue((BinaryExpression)expression);

            case ExpressionType.Call:
                return GetMethodCallExpressionValue(expression);

            case ExpressionType.MemberAccess:
                if (HasParameter(expression))
                {
                    if (expression.Type == typeof(bool))
                        return true;
                    throw CreateUnsupportedValueException(expression);
                }
                return EvaluateValueExpression(expression);

            case ExpressionType.Constant:
                return ((ConstantExpression)expression).Value;

            case ExpressionType.Not:
                var operand = ((UnaryExpression)expression).Operand;
                if (HasParameter(operand))
                {
                    if (expression.Type == typeof(bool) && operand is MemberExpression && operand.Type == typeof(bool))
                        return false;
                    throw CreateUnsupportedValueException(expression);
                }
                return EvaluateValueExpression(expression);
        }
        if (HasParameter(expression))
            throw CreateUnsupportedValueException(expression);
        return EvaluateValueExpression(expression);
    }
    /// <summary>
    /// 提取比较表达式的查询值。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>提取出的比较值；值表达式为空时返回 null。</returns>
    private static object GetComparisonValue(BinaryExpression expression)
    {
        var leftHasParameter = HasParameter(expression.Left);
        var rightHasParameter = HasParameter(expression.Right);
        if (leftHasParameter && rightHasParameter)
            throw CreateUnsupportedValueException(expression);
        if (leftHasParameter)
            return GetComparisonValue(expression.Left, expression.Right);
        if (rightHasParameter)
            return GetComparisonValue(expression.Right, expression.Left);

        // 维持旧行为：没有查询参数时，比较表达式取左侧值。
        return GetValue(expression.Left);
    }
    /// <summary>
    /// 提取比较表达式的查询值。
    /// </summary>
    /// <param name="queryExpression">查询成员表达式。</param>
    /// <param name="valueExpression">待求值的比较值表达式。</param>
    /// <returns>提取出的比较值；值表达式为空时返回 null。</returns>
    private static object GetComparisonValue(Expression queryExpression, Expression valueExpression)
    {
        var value = EvaluateValueExpression(valueExpression);
        if (value == null)
            return null;

        while (queryExpression is UnaryExpression conversion &&
               (conversion.NodeType == ExpressionType.Convert || conversion.NodeType == ExpressionType.ConvertChecked))
            queryExpression = conversion.Operand;
        var queryType = (queryExpression as MemberExpression)?.Type;
        var enumType = queryType == null ? null : Nullable.GetUnderlyingType(queryType) ?? queryType;
        if (enumType == null || !enumType.IsEnum)
            return value;

        var enumUnderlyingType = Enum.GetUnderlyingType(enumType);
        if (value.GetType() == enumType || value.GetType() != enumUnderlyingType ||
            !HasEnumUnderlyingConversion(valueExpression, enumType, enumUnderlyingType))
            return value;

        return Enum.ToObject(enumType, value);
    }
    /// <summary>
    /// 判断表达式是否包含枚举到其基础类型的转换。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <param name="enumType">枚举类型。</param>
    /// <param name="enumUnderlyingType">枚举的基础数值类型。</param>
    /// <returns>包含指定枚举到其基础类型的转换时为 true，否则为 false。</returns>
    private static bool HasEnumUnderlyingConversion(Expression expression, Type enumType, Type enumUnderlyingType)
    {
        while (expression is UnaryExpression unaryExpression &&
               (unaryExpression.NodeType == ExpressionType.Convert ||
                unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            var sourceType = Nullable.GetUnderlyingType(unaryExpression.Operand.Type) ?? unaryExpression.Operand.Type;
            var targetType = Nullable.GetUnderlyingType(unaryExpression.Type) ?? unaryExpression.Type;
            if (sourceType == enumType && targetType == enumUnderlyingType)
                return true;

            expression = unaryExpression.Operand;
        }

        return false;
    }
    /// <summary>
    /// 从方法调用表达式中提取查询值或集合值。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>方法调用对应的查询值或集合；求值结果可以为 null。</returns>
    private static object GetMethodCallExpressionValue(Expression expression)
    {
        var call = (MethodCallExpression)expression;
        if (TryGetSearchOperands(call, out var source, out var value))
        {
            // 保留查询值约定：实例方法优先搜索值，扩展方法优先集合；辅助参数不参与选择。
            var first = call.Object == null ? source : value;
            var second = call.Object == null ? value : source;
            if (!HasParameter(first))
                return EvaluateValueExpression(first);
            if (!HasParameter(second))
                return EvaluateValueExpression(second);
            throw CreateUnsupportedValueException(expression);
        }
        if (!HasParameter(expression))
            return EvaluateValueExpression(expression);
        throw CreateUnsupportedValueException(expression);
    }
    /// <summary>
    /// 提取搜索调用的集合与搜索值表达式。
    /// </summary>
    /// <param name="call">待分析的方法调用表达式。</param>
    /// <param name="source">提取出的集合或字符串来源；不支持的调用返回 null。</param>
    /// <param name="value">提取出的搜索值；不支持的调用返回 null。</param>
    /// <returns>成功识别搜索调用时为 true，否则为 false。</returns>
    private static bool TryGetSearchOperands(MethodCallExpression call, out Expression source, out Expression value)
    {
        source = null;
        value = null;
        if (call.Object != null && call.Arguments.Count > 0 &&
            (IsStringSearchMethod(call.Method) ||
             (string.Equals(call.Method.Name, "Contains", StringComparison.Ordinal) &&
              call.Method.ReturnType == typeof(bool) && call.Arguments.Count == 1 &&
              IsCollectionContains(call))))
        {
            source = call.Object;
            value = call.Arguments[0];
            return true;
        }
        if (call.Object == null && call.Arguments.Count >= 2 && call.Arguments.Count <= 3 &&
            (call.Method.DeclaringType == typeof(Enumerable) || call.Method.DeclaringType == typeof(Queryable) ||
             call.Method.DeclaringType == typeof(ParallelEnumerable)) &&
            string.Equals(call.Method.Name, nameof(Enumerable.Contains), StringComparison.Ordinal))
        {
            source = call.Arguments[0];
            value = call.Arguments[1];
            return true;
        }
        return false;
    }
    /// <summary>
    /// 判断方法调用是否属于支持的集合 Contains 形式。
    /// </summary>
    /// <param name="call">待分析的方法调用表达式。</param>
    /// <returns>属于支持的集合 Contains 调用时为 true，否则为 false。</returns>
    private static bool IsCollectionContains(MethodCallExpression call) =>
        CollectionContainsMethods.GetValue(call, ClassifyCollectionContains).IsSupported;
    /// <summary>
    /// 按声明类型和接口映射识别集合 Contains 形式。
    /// </summary>
    /// <param name="call">待分析的方法调用表达式。</param>
    /// <returns>表示调用是否支持集合搜索的共享分类结果。</returns>
    private static CollectionContainsKind ClassifyCollectionContains(MethodCallExpression call)
    {
        var method = call.Method;
        var declaringType = method.DeclaringType;
        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(call.Object.Type))
            return CollectionContainsKind.Unsupported;
        return IsCollectionContainsInterface(declaringType) || IsKnownCollectionType(declaringType) ||
               ImplementsCollectionContains(declaringType, method) ||
               (declaringType != call.Object.Type && ImplementsCollectionContains(call.Object.Type, method))
            ? CollectionContainsKind.Supported
            : CollectionContainsKind.Unsupported;
    }
    /// <summary>
    /// 判断类型是否为受支持的集合接口。
    /// </summary>
    /// <param name="type">待检查的类型。</param>
    /// <returns>类型为支持的集合接口时为 true，否则为 false。</returns>
    private static bool IsCollectionContainsInterface(Type type)
    {
        if (type == typeof(System.Collections.IList) || type == typeof(System.Collections.IDictionary))
            return true;
        if (type == null || !type.IsGenericType)
            return false;
        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(ICollection<>) || definition == ReadOnlySetInterface;
    }
    /// <summary>
    /// 判断类型是否为已知集合实现。
    /// </summary>
    /// <param name="type">待检查的类型。</param>
    /// <returns>类型为已知集合实现时为 true，否则为 false。</returns>
    private static bool IsKnownCollectionType(Type type)
    {
        if (type == typeof(System.Collections.Queue) || type == typeof(System.Collections.Stack))
            return true;
        if (type == null || !type.IsGenericType)
            return false;
        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(List<>) || definition == typeof(HashSet<>) ||
               definition == typeof(Queue<>) || definition == typeof(Stack<>);
    }
    /// <summary>
    /// 判断类型是否通过接口映射实现指定 Contains 方法。
    /// </summary>
    /// <param name="type">待检查的类型。</param>
    /// <param name="method">待匹配的方法。</param>
    /// <returns>指定方法实现集合接口的 Contains 契约时为 true，否则为 false。</returns>
    private static bool ImplementsCollectionContains(Type type, MethodInfo method)
    {
        if (type == null || type.IsInterface)
            return false;
        foreach (var collectionInterface in type.GetInterfaces())
        {
            if (!IsCollectionContainsInterface(collectionInterface))
                continue;
            var map = type.GetInterfaceMap(collectionInterface);
            for (var i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i].Name != "Contains")
                    continue;
                var target = map.TargetMethods[i];
                if (target.Equals(method) || (target.IsVirtual && method.IsVirtual &&
                                              target.GetBaseDefinition().Equals(method.GetBaseDefinition())))
                    return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 判断方法是否为字符串搜索方法。
    /// </summary>
    /// <param name="method">待匹配的方法。</param>
    /// <returns>方法为字符串包含、前缀或后缀匹配方法时为 true，否则为 false。</returns>
    private static bool IsStringSearchMethod(MethodInfo method)
    {
        return method.DeclaringType == typeof(string) &&
               (string.Equals(method.Name, nameof(string.Contains), StringComparison.Ordinal) ||
                string.Equals(method.Name, nameof(string.StartsWith), StringComparison.Ordinal) ||
                string.Equals(method.Name, nameof(string.EndsWith), StringComparison.Ordinal));
    }
    /// <summary>
    /// 计算不依赖查询参数的表达式值。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>表达式当前的求值结果，可以为 null。</returns>
    private static object EvaluateValueExpression(Expression expression)
    {
        if (expression == null)
            throw CreateUnsupportedValueException(null);
        if (expression is ConstantExpression constant)
            return constant.Value;
        // 先检查整条成员链，再执行 getter；复杂节点回退时不会重复触发用户代码。
        if (CanReadValueMemberChain(expression))
            return ReadValueMemberChain(expression);
        if (ValueEvaluators.TryGetValue(expression, out var existing))
            return existing.GetEvaluator(expression)();
        if (HasParameter(expression))
            throw CreateUnsupportedValueException(expression);
        if (expression.Type == typeof(void))
            throw CreateUnsupportedValueException(expression);

        return ValueEvaluators.GetValue(expression, CreateValueEvaluator).GetEvaluator(expression)();
    }
    /// <summary>
    /// 创建延迟初始化的求值器条目。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>尚未编译求值委托的缓存条目。</returns>
    private static ValueEvaluator CreateValueEvaluator(Expression expression) => new();
    /// <summary>
    /// 编译表达式的求值委托。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>执行时读取表达式当前值的委托。</returns>
    private static Func<object> CompileValueEvaluator(Expression expression)
    {
        try
        {
            var boxedExpression = Expression.Convert(expression, typeof(object));
            if (expression is MethodCallExpression call && CanInvokeValueMethod(call))
                return () => InvokeValueMethod(call);
            return Expression.Lambda<Func<object>>(boxedExpression).Compile(preferInterpretation: true);
        }
        catch (ArgumentException exception)
        {
            throw CreateUnsupportedValueException(expression, exception);
        }
        catch (NotSupportedException exception)
        {
            throw CreateUnsupportedValueException(expression, exception);
        }
        catch (InvalidOperationException exception)
        {
            throw CreateUnsupportedValueException(expression, exception);
        }

    }
    /// <summary>
    /// 判断方法调用能否使用直接反射求值。
    /// </summary>
    /// <param name="call">待分析的方法调用表达式。</param>
    /// <returns>接收者和参数均可直接读取且方法可安全反射调用时为 true，否则为 false。</returns>
    private static bool CanInvokeValueMethod(MethodCallExpression call)
    {
        var method = call.Method;
        if (method.ContainsGenericParameters || (method.CallingConvention & CallingConventions.VarArgs) != 0 ||
            !CanBoxMethodValue(method.ReturnType) ||
            (call.Object != null && call.Object.Type.IsValueType) || !CanReadValueMemberChain(call.Object))
            return false;
        foreach (var parameter in method.GetParameters())
        {
            // ref/out 的写回和值类型接收者的地址语义由解释器负责。
            // Invoke 会把 Missing 当成省略实参；此类签名保留表达式的原始传值语义。
            if (!CanBoxMethodValue(parameter.ParameterType) || parameter.ParameterType.IsAssignableFrom(typeof(Missing)))
                return false;
        }
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (!CanReadValueMemberChain(call.Arguments[i]))
                return false;
        }
        return true;
    }
    /// <summary>
    /// 判断类型是否可作为装箱方法值。
    /// </summary>
    /// <param name="type">待检查的类型。</param>
    /// <returns>类型可作为普通装箱值使用时为 true，否则为 false。</returns>
    private static bool CanBoxMethodValue(Type type)
    {
        if (type == typeof(void) || type.IsByRef || type.IsPointer || type.ContainsGenericParameters)
            return false;
#if NET6_0_OR_GREATER
        if (type.IsByRefLike)
            return false;
#endif
#if NET8_0_OR_GREATER
        if (type.IsFunctionPointer)
            return false;
#endif
        return true;
    }
    /// <summary>
    /// 执行可直接读取参数的方法调用。
    /// </summary>
    /// <param name="call">待分析的方法调用表达式。</param>
    /// <returns>方法执行的返回值，可以为 null。</returns>
    private static object InvokeValueMethod(MethodCallExpression call)
    {
        // 与方法调用表达式一致：先读取接收者，再从左到右读取参数，最后检查空接收者。
        var target = ReadValueMemberChain(call.Object);
        var arguments = call.Arguments.Count == 0 ? null : new object[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
            arguments[i] = ReadValueMemberChain(call.Arguments[i]);
        if (call.Object != null && target == null)
            throw new NullReferenceException();
        try
        {
            return call.Method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
    /// <summary>
    /// 判断成员访问链是否可直接读取。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>成员链仅包含可直接读取的节点时为 true，否则为 false。</returns>
    private static bool CanReadValueMemberChain(Expression expression)
    {
        if (expression == null || expression is ConstantExpression)
            return true;
        if (!(expression is MemberExpression member))
            return false;
        if (member.Type.IsByRef || member.Type.IsPointer || member.Type.ContainsGenericParameters ||
            member.Member.DeclaringType?.ContainsGenericParameters == true)
            return false;
#if NET6_0_OR_GREATER
        if (member.Type.IsByRefLike)
            return false;
#endif
#if NET8_0_OR_GREATER
        if (member.Type.IsFunctionPointer)
            return false;
#endif
        // Nullable 的装箱会丢失包装类型；结构体 getter 还可能修改其接收者，均由解释器处理。
        if (member.Expression != null && Nullable.GetUnderlyingType(member.Expression.Type) != null)
            return false;
        if (member.Member is PropertyInfo property)
        {
            if (property.GetMethod == null || property.GetIndexParameters().Length != 0 ||
                (member.Expression != null && member.Expression.Type.IsValueType))
                return false;
        }
        else if (!(member.Member is FieldInfo))
            return false;
        return CanReadValueMemberChain(member.Expression);
    }
    /// <summary>
    /// 读取成员访问链的当前值。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>成员链的当前值，可以为 null；空表达式返回 null。</returns>
    private static object ReadValueMemberChain(Expression expression)
    {
        if (expression == null)
            return null;
        if (expression is ConstantExpression constant)
            return constant.Value;
        var member = (MemberExpression)expression;
        var target = ReadValueMemberChain(member.Expression);
        if (member.Expression != null && target == null)
            throw new NullReferenceException();
        if (member.Member is FieldInfo field)
            return field.GetValue(target);
        try
        {
            return ((PropertyInfo)member.Member).GetValue(target);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            // 直接访问属性与表达式执行都应抛出 getter 原始异常。
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
    /// <summary>
    /// 创建表达式值无法提取的异常。
    /// </summary>
    /// <param name="expression">无法提取查询值的表达式，可以为 null。</param>
    /// <param name="innerException">导致求值失败的内部异常，可以为 null。</param>
    /// <returns>描述表达式无法提取查询值的异常。</returns>
    private static NotSupportedException CreateUnsupportedValueException(Expression expression,
        Exception innerException = null)
    {
        var nodeType = expression?.NodeType.ToString() ?? "null";
        return new NotSupportedException($"不支持从该表达式中提取查询值，节点类型：{nodeType}。", innerException);
    }
    /// <summary>
    /// 判断表达式是否包含自由参数。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>存在自由参数时为 true，否则为 false。</returns>
    private static bool HasParameter(Expression expression)
    {
        // 常见查询节点不创建访问器或作用域字典；含绑定作用域的节点仍交给完整访问器。
        switch (expression)
        {
            case null:
            case ConstantExpression _:
            case DefaultExpression _:
                return false;
            case ParameterExpression _:
                return true;
            case MemberExpression member:
                return HasParameter(member.Expression);
            case UnaryExpression unary:
                return HasParameter(unary.Operand);
            case BinaryExpression binary:
                return HasParameter(binary.Left) || HasParameter(binary.Right) || HasParameter(binary.Conversion);
            case MethodCallExpression call:
                if (HasParameter(call.Object))
                    return true;
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    if (HasParameter(call.Arguments[i]))
                        return true;
                }
                return false;
        }
        var visitor = new FreeParameterVisitor();
        visitor.Visit(expression);
        return visitor.HasFreeParameter;
    }
    /// <summary>
    /// 跟踪表达式中未绑定的自由参数。
    /// </summary>
    private sealed class FreeParameterVisitor : ExpressionVisitor
    {
        /// <summary>
        /// 当前作用域绑定参数及其嵌套引用次数。
        /// </summary>
        private Dictionary<ParameterExpression, int> _boundParameters;
        /// <summary>
        /// 获取是否已发现自由参数。
        /// </summary>
        public bool HasFreeParameter { get; private set; }
        /// <inheritdoc />
        public override Expression Visit(Expression node) => HasFreeParameter ? node : base.Visit(node);
        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_boundParameters == null || !_boundParameters.ContainsKey(node))
                HasFreeParameter = true;
            return node;
        }
        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            AddBoundParameters(node.Parameters);
            try
            {
                Visit(node.Body);
            }
            finally
            {
                RemoveBoundParameters(node.Parameters);
            }
            return node;
        }
        /// <inheritdoc />
        protected override Expression VisitBlock(BlockExpression node)
        {
            AddBoundParameters(node.Variables);
            try
            {
                Visit(node.Expressions);
            }
            finally
            {
                RemoveBoundParameters(node.Variables);
            }
            return node;
        }
        /// <inheritdoc />
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            if (node.Variable != null)
                AddBoundParameters(new[] { node.Variable });
            try
            {
                Visit(node.Filter);
                Visit(node.Body);
            }
            finally
            {
                if (node.Variable != null)
                    RemoveBoundParameters(new[] { node.Variable });
            }
            return node;
        }
        /// <summary>
        /// 登记当前作用域绑定的参数。
        /// </summary>
        /// <param name="parameters">当前作用域绑定的参数集合。</param>
        private void AddBoundParameters(IEnumerable<ParameterExpression> parameters)
        {
            foreach (var parameter in parameters)
            {
                _boundParameters ??= new Dictionary<ParameterExpression, int>();
                _boundParameters.TryGetValue(parameter, out var count);
                _boundParameters[parameter] = count + 1;
            }
        }
        /// <summary>
        /// 移除当前作用域绑定的参数。
        /// </summary>
        /// <param name="parameters">当前作用域绑定的参数集合。</param>
        private void RemoveBoundParameters(IEnumerable<ParameterExpression> parameters)
        {
            foreach (var parameter in parameters)
            {
                var count = _boundParameters[parameter];
                if (count == 1)
                    _boundParameters.Remove(parameter);
                else
                    _boundParameters[parameter] = count - 1;
            }
        }
    }

    #endregion

    #region GetOperator(获取查询操作符)
    /// <summary>
    /// 获取查询操作符。
    /// </summary>
    /// <remarks>范例：t => t.Name == "A"，返回 Operator.Equal</remarks>
    /// <param name="expression">表达式，范例：t => t.Name == "A"</param>
    /// <returns>识别出的查询运算符；无法识别时返回 null。</returns>
    public static Operator? GetOperator(Expression expression)
    {
        if (expression == null)
            return null;
        switch (expression.NodeType)
        {
            case ExpressionType.Lambda:
                return GetOperator(((LambdaExpression)expression).Body);

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                return GetOperator(((UnaryExpression)expression).Operand);

            case ExpressionType.Equal:
                return Operator.Equal;

            case ExpressionType.NotEqual:
                return Operator.NotEqual;

            case ExpressionType.GreaterThan:
                return Operator.Greater;

            case ExpressionType.LessThan:
                return Operator.Less;

            case ExpressionType.GreaterThanOrEqual:
                return Operator.GreaterEqual;

            case ExpressionType.LessThanOrEqual:
                return Operator.LessEqual;

            case ExpressionType.Call:
                return GetMethodCallExpressionOperator(expression);
        }
        return null;
    }
    /// <summary>
    /// 获取方法调用表达式的查询运算符。
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns>对应的查询运算符；不支持的方法返回 null。</returns>
    private static Operator? GetMethodCallExpressionOperator(Expression expression)
    {
        var call = (MethodCallExpression)expression;
        if (!TryGetSearchOperands(call, out _, out _))
            return null;
        var name = call.Method.Name;
        if (string.Equals(name, "Contains", StringComparison.Ordinal))
            return Operator.Contains;
        if (string.Equals(name, "EndsWith", StringComparison.Ordinal))
            return Operator.Ends;
        if (string.Equals(name, "StartsWith", StringComparison.Ordinal))
            return Operator.Starts;
        return null;
    }

    #endregion

    #region GetParameter(获取参数)
    /// <summary>
    /// 获取参数。
    /// </summary>
    /// <remarks>范例：t.Name，返回 t</remarks>
    /// <param name="expression">表达式，范例：t.Name</param>
    /// <returns>当前表达式使用的参数；未找到时返回 null。</returns>
    public static ParameterExpression GetParameter(Expression expression)
    {
        if (expression == null)
            return null;
        switch (expression.NodeType)
        {
            case ExpressionType.Lambda:
                return GetParameter(((LambdaExpression)expression).Body);

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                return GetParameter(((UnaryExpression)expression).Operand);

            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
                return GetParameter(((BinaryExpression)expression).Left);

            case ExpressionType.MemberAccess:
                return GetParameter(((MemberExpression)expression).Expression);

            case ExpressionType.Call:
                return GetParameter(((MethodCallExpression)expression).Object);

            case ExpressionType.Parameter:
                return (ParameterExpression)expression;
        }
        return null;
    }

    #endregion

    #region GetGroupPredicates(获取分组的谓词表达式)
    /// <summary>
    /// 获取分组的谓词表达式。
    /// </summary>
    /// <remarks>外层列表由 OrElse 连接，内层列表由 AndAlso 连接；混合逻辑子树保留为叶表达式，以保持短路求值顺序。</remarks>
    /// <param name="expression">谓词表达式</param>
    /// <returns>以或连接外层、以与连接内层的谓词分组列表。</returns>
    public static List<List<Expression>> GetGroupPredicates(Expression expression)
    {
        var result = new List<List<Expression>>();
        if (expression == null)
            return result;
        AddPredicates(expression, result, CreateGroup(result), false);
        return result;
    }
    /// <summary>
    /// 创建分组
    /// </summary>
    /// <param name="result">表达式结果</param>
    /// <returns>按逻辑关系组合后的表达式集合。</returns>
    private static List<Expression> CreateGroup(List<List<Expression>> result)
    {
        var group = new List<Expression>();
        result.Add(group);
        return group;
    }
    /// <summary>
    /// 添加通过Or分割的谓词表达式
    /// </summary>
    /// <param name="expression">谓词表达式</param>
    /// <param name="result">表达式结果</param>
    /// <param name="group">分组表达式</param>
    /// <param name="insideConjunction">是否位于合取条件内，用于保持混合逻辑的短路语义。</param>
    private static void AddPredicates(Expression expression, List<List<Expression>> result, List<Expression> group,
        bool insideConjunction)
    {
        switch (expression.NodeType)
        {
            case ExpressionType.Lambda:
                AddPredicates(((LambdaExpression)expression).Body, result, group, insideConjunction);
                break;

            case ExpressionType.OrElse when !insideConjunction && IsBuiltInBooleanOperator(expression):
                AddPredicates(((BinaryExpression)expression).Left, result, group, false);
                AddPredicates(((BinaryExpression)expression).Right, result, CreateGroup(result), false);
                break;

            case ExpressionType.AndAlso when IsBuiltInBooleanOperator(expression):
                AddPredicates(((BinaryExpression)expression).Left, result, group, true);
                AddPredicates(((BinaryExpression)expression).Right, result, group, true);
                break;

            default:
                group.Add(expression);
                break;
        }
    }

    #endregion

    #region GetConditionCount(获取查询条件个数)
    /// <summary>
    /// 获取查询条件个数
    /// </summary>
    /// <param name="expression">谓词表达式，范例1：t => t.Name == "A"，结果1。范例2：t => t.Name == "A" &amp;&amp; t.Age == 1，结果2。</param>
    /// <returns>谓词中的条件数量。</returns>
    public static int GetConditionCount(LambdaExpression expression)
    {
        if (expression == null)
            return 0;
        return CountPredicateConditions(expression.Body);
    }
    /// <summary>
    /// 统计谓词表达式中的条件数量。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>谓词中参与逻辑组合的条件数。</returns>
    private static int CountPredicateConditions(Expression expression)
    {
        if (expression is LambdaExpression lambdaExpression)
            return CountPredicateConditions(lambdaExpression.Body);

        if (expression is UnaryExpression negation && negation.NodeType == ExpressionType.Not &&
            negation.Type == typeof(bool) && negation.Method == null)
            return CountPredicateConditions(negation.Operand);

        if (expression is UnaryExpression unaryExpression &&
            (unaryExpression.NodeType == ExpressionType.Convert ||
             unaryExpression.NodeType == ExpressionType.ConvertChecked) &&
            IsLogicalExpression(unaryExpression.Operand))
            return CountPredicateConditions(unaryExpression.Operand);

        if (IsBuiltInBooleanOperator(expression))
        {
            var binaryExpression = (BinaryExpression)expression;
            return CountPredicateConditions(binaryExpression.Left) + CountPredicateConditions(binaryExpression.Right);
        }

        return 1;
    }
    /// <summary>
    /// 判断表达式是否为内置布尔逻辑组合。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>表达式为内置布尔逻辑组合时为 true，否则为 false。</returns>
    private static bool IsLogicalExpression(Expression expression)
    {
        while (expression is UnaryExpression unaryExpression &&
               (unaryExpression.NodeType == ExpressionType.Convert ||
                unaryExpression.NodeType == ExpressionType.ConvertChecked ||
                (unaryExpression.NodeType == ExpressionType.Not && unaryExpression.Type == typeof(bool) && unaryExpression.Method == null)))
            expression = unaryExpression.Operand;
        return IsBuiltInBooleanOperator(expression);
    }
    /// <summary>
    /// 判断表达式是否使用内置布尔运算符。
    /// </summary>
    /// <param name="expression">待检查或求值的表达式。</param>
    /// <returns>表达式使用未重载的内置布尔运算符时为 true，否则为 false。</returns>
    private static bool IsBuiltInBooleanOperator(Expression expression)
    {
        return expression is BinaryExpression binaryExpression &&
               binaryExpression.Method == null && binaryExpression.Type == typeof(bool) &&
               (binaryExpression.NodeType == ExpressionType.AndAlso ||
                binaryExpression.NodeType == ExpressionType.OrElse);
    }

    #endregion

    #region GetAttribute(获取特性)
    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="expression">属性表达式</param>
    /// <returns>成员上的指定特性；未声明时返回 null。</returns>
    public static TAttribute GetAttribute<TAttribute>(Expression expression)
        where TAttribute : Attribute
    {
        var memberInfo = GetMember(expression);
        return memberInfo.GetCustomAttribute<TAttribute>();
    }
    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="propertyExpression">属性表达式</param>
    /// <returns>成员上的指定特性；未声明时返回 null。</returns>
    public static TAttribute GetAttribute<TEntity, TProperty, TAttribute>(Expression<Func<TEntity, TProperty>> propertyExpression)
        where TAttribute : Attribute =>
        GetAttribute<TAttribute>(propertyExpression);
    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="propertyExpression">属性表达式</param>
    /// <returns>成员上的指定特性；未声明时返回 null。</returns>
    public static TAttribute GetAttribute<TProperty, TAttribute>(Expression<Func<TProperty>> propertyExpression)
        where TAttribute : Attribute =>
        GetAttribute<TAttribute>(propertyExpression);

    #endregion

    #region GetAttributes(获取特性列表)
    /// <summary>
    /// 获取特性列表
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="propertyExpression">属性表达式</param>
    /// <returns>成员上的指定类型特性集合。</returns>
    public static IEnumerable<TAttribute> GetAttributes<TEntity, TProperty, TAttribute>(Expression<Func<TEntity, TProperty>> propertyExpression)
        where TAttribute : Attribute
    {
        var memberInfo = GetMember(propertyExpression);
        return memberInfo.GetCustomAttributes<TAttribute>();
    }

    #endregion

    #region Constant(获取常量表达式)
    /// <summary>
    /// 获取常量表达式
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="expression">表达式</param>
    /// <returns>具有目标成员类型的常量表达式。</returns>
    public static ConstantExpression Constant(object value, Expression expression = null)
    {
        var type = GetType(expression);
        return type == null ? Expression.Constant(value) : Expression.Constant(value, type);
    }

    #endregion

    #region CreateParameter(创建参数表达式)
    /// <summary>
    /// 创建参数表达式
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <returns>表示指定类型对象的参数表达式。</returns>
    public static ParameterExpression CreateParameter<T>() => Expression.Parameter(typeof(T), "t");

    #endregion

    #region Equal(等于表达式)
    /// <summary>
    /// 创建等于运算Lambda表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断相等的表达式。</returns>
    public static Expression<Func<T, bool>> Equal<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .Equal(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region NotEqual(不等于表达式)
    /// <summary>
    /// 创建不等于运算Lambda表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断不相等的表达式。</returns>
    public static Expression<Func<T, bool>> NotEqual<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .NotEqual(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region Greater(大于表达式)
    /// <summary>
    /// 创建大于运算Lambda表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断大于的表达式。</returns>
    public static Expression<Func<T, bool>> Greater<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .Greater(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region GreaterEqual(大于等于表达式)
    /// <summary>
    /// 创建大于等于运算Lambda表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断大于等于的表达式。</returns>
    public static Expression<Func<T, bool>> GreaterEqual<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .GreaterEqual(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region Less(小于表达式)
    /// <summary>
    /// 创建小于运算Lambda表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断小于的表达式。</returns>
    public static Expression<Func<T, bool>> Less<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .Less(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region LessEqual(小于等于表达式)
    /// <summary>
    /// 创建小于等于运算Lambda表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断小于等于的表达式。</returns>
    public static Expression<Func<T, bool>> LessEqual<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .LessEqual(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region Starts(调用StartsWith方法)
    /// <summary>
    /// 调用StartsWith方法
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断字符串前缀匹配的 Lambda 表达式。</returns>
    public static Expression<Func<T, bool>> Starts<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .StartsWith(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region Ends(调用EndsWith方法)
    /// <summary>
    /// 调用EndsWith方法
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断字符串后缀匹配的 Lambda 表达式。</returns>
    public static Expression<Func<T, bool>> Ends<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .EndsWith(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region Contains(调用Contains方法)
    /// <summary>
    /// 调用Contains方法
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <returns>判断包含关系的表达式。</returns>
    public static Expression<Func<T, bool>> Contains<T>(string propertyName, object value)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .Contains(value)
            .ToPredicate<T>(parameter);
    }

    #endregion

    #region ParsePredicate(解析为谓词表达式)
    /// <summary>
    /// 解析为谓词表达式
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
    /// <param name="operator">运算符</param>
    /// <returns>按成员路径、运算符和值构建的谓词表达式。</returns>
    public static Expression<Func<T, bool>> ParsePredicate<T>(string propertyName, object value, Operator @operator)
    {
        var parameter = CreateParameter<T>();
        return parameter
            .Property(propertyName)
            .Operation(@operator, value)
            .ToPredicate<T>(parameter);
    }

    #endregion
}
