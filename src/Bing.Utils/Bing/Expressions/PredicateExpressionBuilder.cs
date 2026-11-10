using System.Linq.Expressions;

namespace Bing.Expressions;
/// <summary>
/// 谓词表达式生成器
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public class PredicateExpressionBuilder<TEntity>
{
    /// <summary>
    /// 构建器所有条件共用的实体参数表达式。
    /// </summary>
    private readonly ParameterExpression _parameter;
    /// <summary>
    /// 当前累积的谓词主体；尚未添加条件或清空后为 null。
    /// </summary>
    private Expression _result;
    /// <summary>
    /// 初始化一个 <see cref="PredicateExpressionBuilder{TEntity}"/> 类型的实例。
    /// </summary>
    public PredicateExpressionBuilder() => _parameter = Expression.Parameter(typeof(TEntity), "t");
    /// <summary>
    /// 获取参数表达式
    /// </summary>
    /// <returns>构建器所有条件共用的实体参数表达式。</returns>
    public ParameterExpression GetParameter() => _parameter;
    /// <summary>
    /// 添加表达式
    /// </summary>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <param name="propertyExpression">属性表达式</param>
    /// <param name="operator">运算符</param>
    /// <param name="value">值</param>
    public void Append<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, Operator @operator, object value) =>
        _result = _result.And(BindMember(propertyExpression)
            .Operation(@operator, value));
    /// <summary>
    /// 添加表达式
    /// </summary>
    /// <typeparam name="TProperty">属性类型</typeparam>
    /// <param name="propertyExpression">属性表达式</param>
    /// <param name="operator">运算符</param>
    /// <param name="value">值</param>
    public void Append<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, Operator @operator,
        Expression value)
    {
        var member = BindMember(propertyExpression);
        var map = new Dictionary<ParameterExpression, ParameterExpression> { [propertyExpression.Parameters[0]] = _parameter };
        var boundValue = ParameterRebinder.ReplaceParameters(map, value);
        _result = _result.And(member.Operation(@operator, boundValue));
    }
    /// <summary>
    /// 将实体成员路径绑定到构建器的统一参数。
    /// </summary>
    /// <typeparam name="TProperty">成员类型。</typeparam>
    /// <param name="propertyExpression">从实体参数开始的成员路径。</param>
    /// <returns>使用构建器参数的成员表达式。</returns>
    private Expression BindMember<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        if (propertyExpression == null)
            throw new ArgumentNullException(nameof(propertyExpression));
        Expression body = propertyExpression.Body;
        if (body is UnaryExpression boxing && boxing.NodeType == ExpressionType.Convert &&
            boxing.Type == typeof(object) && boxing.Method == null)
            body = boxing.Operand;
        if (!(body is MemberExpression member))
            throw new ArgumentException("表达式必须是从实体参数开始的成员路径。", nameof(propertyExpression));

        Expression root = member;
        while (true)
        {
            if (root is MemberExpression access)
                root = access.Expression;
            else if (root is UnaryExpression conversion && conversion.Method == null &&
                     (conversion.NodeType == ExpressionType.Convert || conversion.NodeType == ExpressionType.ConvertChecked ||
                      conversion.NodeType == ExpressionType.TypeAs) &&
                     !conversion.Type.IsValueType && !conversion.Operand.Type.IsValueType)
                root = conversion.Operand;
            else
                break;
        }
        if (root != propertyExpression.Parameters[0])
            throw new ArgumentException("表达式必须是从实体参数开始的成员路径。", nameof(propertyExpression));
        return ParameterRebinder.ReplaceParameters(
            new Dictionary<ParameterExpression, ParameterExpression> { [propertyExpression.Parameters[0]] = _parameter }, member);
    }
    /// <summary>
    /// 添加表达式
    /// </summary>
    /// <param name="property">属性名</param>
    /// <param name="operator">运算符</param>
    /// <param name="value">值</param>
    public void Append(string property, Operator @operator, object value) => _result = _result.And(_parameter.Property(property).Operation(@operator, value));
    /// <summary>
    /// 添加表达式
    /// </summary>
    /// <param name="property">属性名</param>
    /// <param name="operator">运算符</param>
    /// <param name="value">值</param>
    public void Append(string property, Operator @operator, Expression value) => _result = _result.And(_parameter.Property(property).Operation(@operator, value));
    /// <summary>
    /// 清空
    /// </summary>
    public void Clear() => _result = null;
    /// <summary>
    /// 转换为Lambda表达式
    /// </summary>
    /// <returns>当前累积的谓词表达式；尚未添加条件或清空后返回 null。</returns>
    public Expression<Func<TEntity, bool>> ToLambda() => _result.ToLambda<Func<TEntity, bool>>(_parameter);
}
