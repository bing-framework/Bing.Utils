namespace Bing.IdUtils
{
    /// <summary>
    /// Trace Id 生成器
    /// </summary>
    public interface ITraceIdMaker
    {
        /// <summary>
        /// 创建 Id
        /// </summary>
        string Create();
    }
}