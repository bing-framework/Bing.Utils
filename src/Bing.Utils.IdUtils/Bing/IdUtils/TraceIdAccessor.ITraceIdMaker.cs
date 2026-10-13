namespace Bing.IdUtils;

/// <summary>
/// Trace Id 生成器
/// </summary>
public interface ITraceIdMaker
{
    /// <summary>
    /// 创建 Id
    /// </summary>
    /// <returns>新生成的 Trace Id。</returns>
    string Create();
}
