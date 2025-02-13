using AspectCore.Extensions.Reflection;

namespace Bing.Dynamic;

/// <summary>
/// 动态类型抽象基类
/// </summary>
public abstract class DynamicBase
{
    /// <summary>
    /// 类型
    /// </summary>
    private readonly Type _type;

    /// <summary>
    /// 初始化一个<see cref="DynamicBase"/>类型的实例
    /// </summary>
    protected DynamicBase() => _type = GetType();

    /// <summary>
    /// 获取指定属性名称的值
    /// </summary>
    /// <param name="name">属性名称</param>
    public object GetPropertyValue(string name) => _type.GetProperty(name).GetReflector().GetValue(this);

    /// <summary>
    /// 设置指定属性名称的值
    /// </summary>
    /// <param name="name">属性名称</param>
    /// <param name="value">值</param>
    public void SetPropertyValue(string name, object value) => _type.GetProperty(name).GetReflector().SetValue(this, value);
}