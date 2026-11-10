using System.Linq.Expressions;

// ReSharper disable once CheckNamespace
namespace Bing.Expressions;
/// <summary>
/// 系统扩展 - Lambda表达式扩展
/// </summary>
public static partial class LambdaExtensions
{
    #region Property(属性表达式)
    /// <summary>
    /// 创建属性表达式
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <param name="propertyName">属性名，支持多级属性名，与句点分隔，范例：Customer.Name</param>
    /// <exception cref="ArgumentNullException">表达式或属性路径为 null。</exception>
    /// <exception cref="ArgumentException">路径包含空名称或属性不存在。</exception>
    /// <returns>指定成员路径对应的属性表达式。</returns>
    public static Expression Property(this Expression expression, string propertyName)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));
        if (propertyName == null)
            throw new ArgumentNullException(nameof(propertyName));
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("属性路径不能包含空名称。", nameof(propertyName));
        if (propertyName.IndexOf('.') < 0)
            return Expression.Property(expression, propertyName);
        var propertyNameList = propertyName.Split('.');
        if (propertyNameList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("属性路径不能包含空名称。", nameof(propertyName));
        Expression result = null;
        for (var i = 0; i < propertyNameList.Length; i++)
        {
            if (i == 0)
            {
                result = Expression.Property(expression, propertyNameList[0]);
                continue;
            }
            result = result.Property(propertyNameList[i]);
        }
        return result;
    }
    /// <summary>
    /// 创建属性表达式
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <param name="member">属性</param>
    /// <returns>指定成员路径对应的属性表达式。</returns>
    public static Expression Property(this Expression expression, MemberInfo member) => Expression.MakeMemberAccess(expression, member);

    #endregion

    #region And(与表达式)
    /// <summary>
    /// 与操作表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>按短路与组合的表达式。</returns>
    public static Expression And(this Expression left, Expression right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;
        return Expression.AndAlso(left, right);
    }
    /// <summary>
    /// 与操作表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>按短路与组合的表达式。</returns>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;
        return left.Compose(right, Expression.AndAlso);
    }

    #endregion

    #region AndIf(与表达式)
    /// <summary>
    /// 与操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路与表达式，否则返回原表达式。</returns>
    public static Expression AndIf(this Expression left, bool condition, Expression right) => condition ? left.And(right) : left;
    /// <summary>
    /// 与操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路与表达式，否则返回原表达式。</returns>
    public static Expression AndIf(this Expression left, Func<bool> condition, Expression right) => condition() ? left.And(right) : left;
    /// <summary>
    /// 与操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路与表达式，否则返回原表达式。</returns>
    public static Expression<Func<T, bool>> AndIf<T>(this Expression<Func<T, bool>> left, bool condition, Expression<Func<T, bool>> right) =>
        condition ? left.And(right) : left;
    /// <summary>
    /// 与操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路与表达式，否则返回原表达式。</returns>
    public static Expression<Func<T, bool>> AndIf<T>(this Expression<Func<T, bool>> left, Func<bool> condition, Expression<Func<T, bool>> right) =>
        condition() ? left.And(right) : left;

    #endregion

    #region Or(或表达式)
    /// <summary>
    /// 或操作表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>按短路或组合的表达式。</returns>
    public static Expression Or(this Expression left, Expression right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;
        return Expression.OrElse(left, right);
    }
    /// <summary>
    /// 或操作表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>按短路或组合的表达式。</returns>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;
        return left.Compose(right, Expression.OrElse);
    }

    #endregion

    #region OrIf(或表达式)
    /// <summary>
    /// 或操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路或表达式，否则返回原表达式。</returns>
    public static Expression OrIf(this Expression left, bool condition, Expression right) => condition ? left.Or(right) : left;
    /// <summary>
    /// 或操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路或表达式，否则返回原表达式。</returns>
    public static Expression OrIf(this Expression left, Func<bool> condition, Expression right) => condition() ? left.Or(right) : left;
    /// <summary>
    /// 或操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路或表达式，否则返回原表达式。</returns>
    public static Expression<Func<T, bool>> OrIf<T>(this Expression<Func<T, bool>> left, bool condition, Expression<Func<T, bool>> right) =>
        condition ? left.Or(right) : left;
    /// <summary>
    /// 或操作表达式。如果给定条件为真，则拼接表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="left">左操作数</param>
    /// <param name="condition">给定条件</param>
    /// <param name="right">右操作数</param>
    /// <returns>满足条件时组合的短路或表达式，否则返回原表达式。</returns>
    public static Expression<Func<T, bool>> OrIf<T>(this Expression<Func<T, bool>> left, Func<bool> condition, Expression<Func<T, bool>> right) =>
        condition() ? left.Or(right) : left;

    #endregion

    #region Value(获取Lambda表达式的值)
    /// <summary>
    /// 获取Lambda表达式的值
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="expression">表达式</param>
    /// <returns>从表达式中提取的查询值，可以为 null。</returns>
    public static object Value<T>(this Expression<Func<T, bool>> expression) => Lambdas.GetValue(expression);

    #endregion

    #region Equal(等于表达式)
    /// <summary>
    /// 创建等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>判断相等的表达式。</returns>
    public static Expression Equal(this Expression left, Expression right) => Expression.Equal(left, right);
    /// <summary>
    /// 创建等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <returns>判断相等的表达式。</returns>
    public static Expression Equal(this Expression left, object value) => left.Equal(Lambdas.Constant(value, left));

    #endregion

    #region NotEqual(不等于表达式)
    /// <summary>
    /// 创建不等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>判断不相等的表达式。</returns>
    public static Expression NotEqual(this Expression left, Expression right) => Expression.NotEqual(left, right);
    /// <summary>
    /// 创建不等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <returns>判断不相等的表达式。</returns>
    public static Expression NotEqual(this Expression left, object value) => left.NotEqual(Lambdas.Constant(value, left));

    #endregion

    #region Greater(大于表达式)
    /// <summary>
    /// 创建大于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>判断大于的表达式。</returns>
    public static Expression Greater(this Expression left, Expression right) => Expression.GreaterThan(left, right);
    /// <summary>
    /// 创建大于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <returns>判断大于的表达式。</returns>
    public static Expression Greater(this Expression left, object value) => left.Greater(Lambdas.Constant(value, left));

    #endregion

    #region GreaterEqual(大于等于表达式)
    /// <summary>
    /// 创建大于等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>判断大于等于的表达式。</returns>
    public static Expression GreaterEqual(this Expression left, Expression right) => Expression.GreaterThanOrEqual(left, right);
    /// <summary>
    /// 创建大于等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <returns>判断大于等于的表达式。</returns>
    public static Expression GreaterEqual(this Expression left, object value) => left.GreaterEqual(Lambdas.Constant(value, left));

    #endregion

    #region Less(小于表达式)
    /// <summary>
    /// 创建小于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>判断小于的表达式。</returns>
    public static Expression Less(this Expression left, Expression right) => Expression.LessThan(left, right);
    /// <summary>
    /// 创建小于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <returns>判断小于的表达式。</returns>
    public static Expression Less(this Expression left, object value) => left.Less(Lambdas.Constant(value, left));

    #endregion

    #region LessEqual(小于等于表达式)
    /// <summary>
    /// 创建小于等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>判断小于等于的表达式。</returns>
    public static Expression LessEqual(this Expression left, Expression right) => Expression.LessThanOrEqual(left, right);
    /// <summary>
    /// 创建小于等于运算表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <returns>判断小于等于的表达式。</returns>
    public static Expression LessEqual(this Expression left, object value) => left.LessEqual(Lambdas.Constant(value, left));

    #endregion

    #region StartsWith(头匹配)
    /// <summary>
    /// 创建前缀匹配表达式
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <exception cref="ArgumentException">表达式类型不支持相应的字符串匹配方法。</exception>
    /// <returns>判断字符串前缀匹配的表达式。</returns>
    public static Expression StartsWith(this Expression left, object value) => CallStringMethod(left, "StartsWith", value);

    #endregion

    #region EndsWith(尾匹配)
    /// <summary>
    /// 创建后缀匹配表达式。
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <exception cref="ArgumentException">表达式类型不支持相应的字符串匹配方法。</exception>
    /// <returns>判断字符串后缀匹配的表达式。</returns>
    public static Expression EndsWith(this Expression left, object value) => CallStringMethod(left, "EndsWith", value);

    #endregion

    #region Contains(模糊匹配)
    /// <summary>
    /// 创建字符串包含匹配表达式。
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="value">值</param>
    /// <exception cref="ArgumentException">表达式类型不支持相应的字符串匹配方法。</exception>
    /// <returns>判断包含关系的表达式。</returns>
    public static Expression Contains(this Expression left, object value) => CallStringMethod(left, "Contains", value);
    /// <summary>
    /// 创建字符串实例方法调用表达式，并在方法不存在时报告参数错误。
    /// </summary>
    /// <param name="left">调用方法的实例表达式。</param>
    /// <param name="methodName">公共实例方法名。</param>
    /// <param name="value">待检查的参数值。</param>
    /// <returns>该操作得到的结果。</returns>
    private static Expression CallStringMethod(Expression left, string methodName, object value) =>
        left.Call(methodName, new[] { typeof(string) }, value)
        ?? throw new ArgumentException($"类型 '{left.Type}' 不支持方法 '{methodName}(string)'。", nameof(left));

    #endregion

    #region Operation(操作)
    /// <summary>
    /// 按指定运算符创建查询表达式。
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="operator">运算符</param>
    /// <param name="value">值</param>
    /// <returns>与指定查询运算符对应的表达式。</returns>
    public static Expression Operation(this Expression left, Operator @operator, object value)
    {
        return @operator switch
        {
            Operator.Equal => left.Equal(value),
            Operator.NotEqual => left.NotEqual(value),
            Operator.Greater => left.Greater(value),
            Operator.GreaterEqual => left.GreaterEqual(value),
            Operator.Less => left.Less(value),
            Operator.LessEqual => left.LessEqual(value),
            Operator.Starts => left.StartsWith(value),
            Operator.Ends => left.EndsWith(value),
            Operator.Contains => left.Contains(value),
            _ => throw new NotImplementedException()
        };
    }
    /// <summary>
    /// 按指定运算符创建查询表达式。
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="operator">运算符</param>
    /// <param name="value">值</param>
    /// <returns>与指定查询运算符对应的表达式。</returns>
    public static Expression Operation(this Expression left, Operator @operator, Expression value)
    {
        return @operator switch
        {
            Operator.Equal => left.Equal(value),
            Operator.NotEqual => left.NotEqual(value),
            Operator.Greater => left.Greater(value),
            Operator.GreaterEqual => left.GreaterEqual(value),
            Operator.Less => left.Less(value),
            Operator.LessEqual => left.LessEqual(value),
            _ => throw new NotImplementedException()
        };
    }

    #endregion

    #region Call(调用方法表达式)
    /// <summary>
    /// 创建公共实例方法调用表达式。
    /// </summary>
    /// <param name="instance">调用的实例</param>
    /// <param name="methodName">方法名</param>
    /// <param name="values">参数值列表</param>
    /// <remarks>按参数表达式类型选择重载，不推断开放泛型方法。</remarks>
    /// <returns>调用表达式；找不到匹配方法时返回 null。</returns>
    /// <exception cref="ArgumentException">参数表达式包含 null，或其类型不能用于所选方法。</exception>
    /// <exception cref="AmbiguousMatchException">存在多个无法区分的匹配重载。</exception>
    public static Expression Call(this Expression instance, string methodName, params Expression[] values)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (methodName == null)
            throw new ArgumentNullException(nameof(methodName));
        values ??= Array.Empty<Expression>();
        if (values.Any(value => value == null))
            throw new ArgumentException("参数表达式不能包含 null。", nameof(values));
        var methodInfo = FindCallMethod(instance.Type, methodName, values.Select(value => value.Type).ToArray());
        if (methodInfo == null)
            return null;
        return Expression.Call(instance, methodInfo, values);
    }
    /// <summary>
    /// 创建公共实例方法调用表达式。
    /// </summary>
    /// <param name="instance">调用的实例</param>
    /// <param name="methodName">方法名</param>
    /// <param name="values">参数值列表</param>
    /// <returns>调用表达式；找不到匹配方法时返回 null。</returns>
    /// <exception cref="AmbiguousMatchException">存在多个无法区分的匹配重载。</exception>
    public static Expression Call(this Expression instance, string methodName, params object[] values)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (methodName == null)
            throw new ArgumentNullException(nameof(methodName));
        values ??= Array.Empty<object>();
        var candidates = instance.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == methodName && !method.ContainsGenericParameters)
            .Where(method => AreCallArgumentsCompatible(method.GetParameters(), values))
            .ToArray();
        if (candidates.Length == 0)
            return null;
        var best = candidates.Where(candidate => !candidates.Any(other =>
            other != candidate && IsMoreSpecific(other, candidate))).ToArray();
        if (best.Length != 1)
            throw new AmbiguousMatchException($"方法 '{methodName}' 存在多个匹配的重载。");
        return Expression.Call(instance, best[0], CreateCallArguments(best[0].GetParameters(), values));
    }
    /// <summary>
    /// 创建公共实例方法调用表达式。
    /// </summary>
    /// <param name="instance">调用的实例</param>
    /// <param name="methodName">方法名</param>
    /// <param name="paramTypes">参数类型列表</param>
    /// <param name="values">参数值列表</param>
    /// <returns>调用表达式；找不到匹配方法时返回 null。</returns>
    /// <exception cref="ArgumentException">参数值的数量或类型与方法不兼容。</exception>
    /// <exception cref="AmbiguousMatchException">存在多个无法区分的匹配重载。</exception>
    public static Expression Call(this Expression instance, string methodName, Type[] paramTypes,
        params object[] values)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        if (methodName == null)
            throw new ArgumentNullException(nameof(methodName));
        if (paramTypes == null)
            throw new ArgumentNullException(nameof(paramTypes));
        values ??= Array.Empty<object>();
        if (paramTypes.Length != values.Length)
            throw new ArgumentException("参数类型和值的数量必须一致。", nameof(values));
        for (var i = 0; i < paramTypes.Length; i++)
        {
            if (paramTypes[i] == null)
                throw new ArgumentException("参数类型不能包含 null。", nameof(paramTypes));
            if (!IsCallArgumentCompatible(paramTypes[i], values[i]))
                throw new ArgumentException("参数值与指定的参数类型不兼容。", nameof(values));
        }
        var methodInfo = FindCallMethod(instance.Type, methodName, paramTypes);
        if (methodInfo == null)
            return null;
        var parameters = methodInfo.GetParameters();
        if (!AreCallArgumentsCompatible(parameters, values))
            throw new ArgumentException("参数值与方法参数类型不兼容。", nameof(values));
        return Expression.Call(instance, methodInfo, CreateCallArguments(parameters, values));
    }
    /// <summary>
    /// 按显式参数类型查找唯一的公共实例方法。
    /// </summary>
    /// <param name="instanceType">实例的运行时类型。</param>
    /// <param name="methodName">公共实例方法名。</param>
    /// <param name="paramTypes">用于选择重载的参数类型序列。</param>
    /// <returns>匹配的方法；无匹配项时返回 null。</returns>
    private static MethodInfo FindCallMethod(Type instanceType, string methodName, Type[] paramTypes)
    {
        var methods = instanceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == methodName && !method.ContainsGenericParameters)
            .ToArray();
        return methods.Length == 0 ? null : (MethodInfo)Type.DefaultBinder.SelectMethod(
            BindingFlags.Public | BindingFlags.Instance, methods, paramTypes, null);
    }
    /// <summary>
    /// 判断运行时参数是否与方法参数逐项兼容。
    /// </summary>
    /// <param name="parameters">目标方法的参数元数据。</param>
    /// <param name="values">调用时传入的参数值。</param>
    /// <returns>参数数量和每个参数值的类型均兼容时为 true，否则为 false。</returns>
    private static bool AreCallArgumentsCompatible(ParameterInfo[] parameters, object[] values)
    {
        if (parameters.Length != values.Length)
            return false;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!IsCallArgumentCompatible(parameters[i].ParameterType, values[i]))
                return false;
        }
        return true;
    }
    /// <summary>
    /// 判断单个运行时值能否传递给指定参数类型。
    /// </summary>
    /// <param name="type">待检查的类型。</param>
    /// <param name="value">待检查的参数值。</param>
    /// <returns>参数类型可接收该值且不是引用或指针类型时为 true，否则为 false。</returns>
    private static bool IsCallArgumentCompatible(Type type, object value) =>
        !type.IsByRef && !type.IsPointer && (value == null
            ? !type.IsValueType || Nullable.GetUnderlyingType(type) != null
            : type.IsInstanceOfType(value));
    /// <summary>
    /// 判断候选方法是否比另一候选方法更具体。
    /// </summary>
    /// <param name="candidateMethod">待比较的候选方法。</param>
    /// <param name="otherMethod">另一候选方法。</param>
    /// <returns>候选方法的参数或声明类型更具体时为 true，否则为 false。</returns>
    private static bool IsMoreSpecific(MethodInfo candidateMethod, MethodInfo otherMethod)
    {
        var candidate = candidateMethod.GetParameters();
        var other = otherMethod.GetParameters();
        var strictlyMoreSpecific = false;
        for (var i = 0; i < candidate.Length; i++)
        {
            var candidateType = candidate[i].ParameterType;
            var otherType = other[i].ParameterType;
            if (candidateType == otherType)
                continue;
            if (!otherType.IsAssignableFrom(candidateType))
                return false;
            strictlyMoreSpecific = true;
        }
        if (strictlyMoreSpecific)
            return true;
        var candidateDeclaringType = candidateMethod.DeclaringType;
        var otherDeclaringType = otherMethod.DeclaringType;
        return candidateDeclaringType != null && otherDeclaringType != null &&
               candidateDeclaringType != otherDeclaringType && otherDeclaringType.IsAssignableFrom(candidateDeclaringType);
    }
    /// <summary>
    /// 按方法参数类型创建调用参数表达式序列。
    /// </summary>
    /// <param name="parameters">目标方法的参数元数据。</param>
    /// <param name="values">调用时传入的参数值。</param>
    /// <returns>使用声明参数类型构造的常量表达式序列。</returns>
    private static IEnumerable<Expression> CreateCallArguments(ParameterInfo[] parameters, object[] values)
    {
        for (var i = 0; i < parameters.Length; i++)
            yield return Expression.Constant(values[i], parameters[i].ParameterType);
    }

    #endregion

    #region Compose(组合表达式)
    /// <summary>
    /// 组合表达式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="first">左操作数</param>
    /// <param name="second">右操作数</param>
    /// <param name="merge">合并操作</param>
    /// <returns>按指定合并规则组合的 Lambda 表达式。</returns>
    internal static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second,
        Func<Expression, Expression, Expression> merge)
    {
        var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] })
            .ToDictionary(p => p.s, p => p.f);
        var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);
        return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
    }

    #endregion

    #region ToLambda(创建Lambda表达式)
    /// <summary>
    /// 创建Lambda表达式
    /// </summary>
    /// <typeparam name="TDelegate">委托类型</typeparam>
    /// <param name="body">表达式</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>使用指定参数构造的 Lambda 表达式；主体为空时返回 null。</returns>
    public static Expression<TDelegate> ToLambda<TDelegate>(this Expression body, params ParameterExpression[] parameters)
    {
        if (body == null)
            return null;
        return Expression.Lambda<TDelegate>(body, parameters);
    }

    #endregion

    #region ToPredicate(创建谓词表达式)
    /// <summary>
    /// 创建谓词表达式
    /// </summary>
    /// <typeparam name="T">委托类型</typeparam>
    /// <param name="body">表达式</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>构造的谓词表达式；主体为空时返回 null。</returns>
    public static Expression<Func<T, bool>> ToPredicate<T>(this Expression body,
        params ParameterExpression[] parameters) =>
        ToLambda<Func<T, bool>>(body, parameters);

    #endregion
}
