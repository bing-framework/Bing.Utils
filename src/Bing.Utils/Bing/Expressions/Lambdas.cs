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
    // 弱键绑定表达式生命周期；仅复用解释委托，不缓存执行结果或延长闭包生命周期。
    private static readonly ConditionalWeakTable<Expression, ValueEvaluator> ValueEvaluators = new();

    /// <summary>按需编译的缓存条目，避免每次登记表达式额外创建工厂闭包和委托。</summary>
    private sealed class ValueEvaluator
    {
        private Func<object> _evaluator;
        private System.Runtime.ExceptionServices.ExceptionDispatchInfo _compilationError;
        private bool _compiling;

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
    /// 获取成员名称，范例：t => t.A.Name，返回 A.Name
    /// </summary>
    /// <param name="expression">表达式，范例：t => t.Name</param>
    public static string GetName(Expression expression)
    {
        var memberExpression = GetMemberExpression(expression);
        return GetMemberName(memberExpression);
    }

    /// <summary>
    /// 获取成员名称
    /// </summary>
    /// <param name="memberExpression">表达式</param>
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
    /// 获取名称列表，范例：t => new object[] {t.A.B,t.C}，返回 A.B,C
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="expression">属性集合表达式，范例：t => new object[]{t.A,t.B}</param>
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
    /// 获取最后以及成员名称，范例：t => t.Name，返回 Name
    /// </summary>
    /// <param name="expression">表达式，范例：t => t.Name</param>
    /// <param name="right">取表达式右侧，(l,r)=> l.LId == r.RId，设置为true，返回 RID</param>
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
    /// 获取最后一级成员名称列表，范例：t => new object[] {t.A.B,t.C}，返回 B,C
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="expression">属性集合表达式，范例：t => new object[] {t.A,t.B}</param>
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
    /// 获取查询表达式中的值，范例：t => t.Name == "A"，返回 A。
    /// </summary>
    /// <param name="expression">表达式，范例：t => t.Name == "A"</param>
    /// <remarks>
    /// 参数无关的值子表达式会按表达式语义计算。直接布尔查询成员及其一元否定保留历史查询值 true/false；无法从参数依赖表达式中提取值时抛出 <see cref="NotSupportedException"/>。
    /// </remarks>
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

    /// <summary>按调用签名定位搜索的两个业务操作数，排除比较器等辅助参数。</summary>
    private static bool TryGetSearchOperands(MethodCallExpression call, out Expression source, out Expression value)
    {
        source = null;
        value = null;
        if (call.Object != null && call.Arguments.Count > 0 &&
            (IsStringSearchMethod(call.Method) || string.Equals(call.Method.Name, "Contains", StringComparison.Ordinal)))
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

    private static bool IsStringSearchMethod(MethodInfo method)
    {
        return method.DeclaringType == typeof(string) &&
               (string.Equals(method.Name, nameof(string.Contains), StringComparison.Ordinal) ||
                string.Equals(method.Name, nameof(string.StartsWith), StringComparison.Ordinal) ||
                string.Equals(method.Name, nameof(string.EndsWith), StringComparison.Ordinal));
    }

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

    private static ValueEvaluator CreateValueEvaluator(Expression expression) => new();

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

    /// <summary>仅对可直接读取的参数使用反射调用，避免解释器每次分配执行帧。</summary>
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

    /// <summary>识别可直接读取的闭包、静态字段和引用类型属性链，不缓存捕获值。</summary>
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

    private static NotSupportedException CreateUnsupportedValueException(Expression expression,
        Exception innerException = null)
    {
        var nodeType = expression?.NodeType.ToString() ?? "null";
        return new NotSupportedException($"不支持从该表达式中提取查询值，节点类型：{nodeType}。", innerException);
    }

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

    private sealed class FreeParameterVisitor : ExpressionVisitor
    {
        private Dictionary<ParameterExpression, int> _boundParameters;

        public bool HasFreeParameter { get; private set; }

        public override Expression Visit(Expression node) => HasFreeParameter ? node : base.Visit(node);

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_boundParameters == null || !_boundParameters.ContainsKey(node))
                HasFreeParameter = true;
            return node;
        }

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

        private void AddBoundParameters(IEnumerable<ParameterExpression> parameters)
        {
            foreach (var parameter in parameters)
            {
                _boundParameters ??= new Dictionary<ParameterExpression, int>();
                _boundParameters.TryGetValue(parameter, out var count);
                _boundParameters[parameter] = count + 1;
            }
        }

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
    /// 获取查询操作符，范例：t => t.Name == "A"，返回 Operator.Equal
    /// </summary>
    /// <param name="expression">表达式，范例：t => t.Name == "A"</param>
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
    /// 获取方法调用表达式的值
    /// </summary>
    /// <param name="expression">表达式</param>
    private static Operator? GetMethodCallExpressionOperator(Expression expression)
    {
        var name = ((MethodCallExpression)expression).Method.Name;
        if (string.Equals(name, "Contains", StringComparison.OrdinalIgnoreCase))
            return Operator.Contains;
        if (string.Equals(name, "EndsWith", StringComparison.OrdinalIgnoreCase))
            return Operator.Ends;
        if (string.Equals(name, "StartsWith", StringComparison.OrdinalIgnoreCase))
            return Operator.Starts;
        return null;
    }

    #endregion

    #region GetParameter(获取参数)

    /// <summary>
    /// 获取参数，范例：t.Name，返回 t
    /// </summary>
    /// <param name="expression">表达式，范例：t.Name</param>
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
    /// 获取分组的谓词表达式。外层列表由 OrElse 连接，内层列表由 AndAlso 连接。
    /// 含有混合逻辑运算的子树会保留为叶表达式，避免分配展开并保持短路求值顺序。
    /// </summary>
    /// <param name="expression">谓词表达式</param>
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
    public static int GetConditionCount(LambdaExpression expression)
    {
        if (expression == null)
            return 0;
        return CountPredicateConditions(expression.Body);
    }

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

    private static bool IsLogicalExpression(Expression expression)
    {
        while (expression is UnaryExpression unaryExpression &&
               (unaryExpression.NodeType == ExpressionType.Convert ||
                unaryExpression.NodeType == ExpressionType.ConvertChecked ||
                (unaryExpression.NodeType == ExpressionType.Not && unaryExpression.Type == typeof(bool) && unaryExpression.Method == null)))
            expression = unaryExpression.Operand;
        return IsBuiltInBooleanOperator(expression);
    }

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
    public static TAttribute GetAttribute<TEntity, TProperty, TAttribute>(Expression<Func<TEntity, TProperty>> propertyExpression)
        where TAttribute : Attribute =>
        GetAttribute<TAttribute>(propertyExpression);

    /// <summary>
    /// 获取特性
    /// </summary>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <typeparam name="TAttribute">特性类型</typeparam>
    /// <param name="propertyExpression">属性表达式</param>
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
    public static ParameterExpression CreateParameter<T>() => Expression.Parameter(typeof(T), "t");

    #endregion

    #region Equal(等于表达式)

    /// <summary>
    /// 创建等于运算Lambda表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">值</param>
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
