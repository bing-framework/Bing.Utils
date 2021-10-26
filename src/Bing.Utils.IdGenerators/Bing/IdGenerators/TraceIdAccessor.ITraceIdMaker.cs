namespace Bing.IdGenerators
{
    /// <summary>
    /// Trace Id 生成器
    /// </summary>
    public interface ITraceIdMaker
    {
        /// <summary>
        /// 生成 Id
        /// </summary>
        string Create();
    }
}
