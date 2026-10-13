namespace Bing.Utils.Parameters;

/// <summary>
/// 参数管理器
/// </summary>
public interface IParameterManager
{
    /// <summary>
    /// 获取值
    /// </summary>
    /// <param name="name">参数名</param>
    /// <returns>指定参数的值。</returns>
    object GetValue(string name);
}
