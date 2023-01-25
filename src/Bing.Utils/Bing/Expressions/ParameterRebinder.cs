﻿using System.Linq.Expressions;

namespace Bing.Expressions;

/// <summary>
/// 参数重绑定操作
/// </summary>
public class ParameterRebinder : ExpressionVisitor
{
    /// <summary>
    /// 参数字典
    /// </summary>
    private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

    /// <summary>
    /// 初始化一个<see cref="ParameterRebinder"/>类型的实例
    /// </summary>
    /// <param name="map">参数字典</param>
    public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map) => _map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();

    /// <summary>
    /// 替换参数
    /// </summary>
    /// <param name="map">参数字典</param>
    /// <param name="exp">表达式</param>
    public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp) =>
        new ParameterRebinder(map).Visit(exp);

    /// <summary>
    /// 访问参数
    /// </summary>
    /// <param name="parameterExpression">参数表达式</param>
    protected override Expression VisitParameter(ParameterExpression parameterExpression)
    {
        if (_map.TryGetValue(parameterExpression, out var replacement))
            parameterExpression = replacement;
        return base.VisitParameter(parameterExpression);
    }
}