namespace Bing.IdUtils
{
    /// <summary>
    /// Trace Id 访问器
    /// </summary>
    public sealed class TraceIdAccessor
    {
        /// <summary>
        /// 默认的 Trace Id 生成器
        /// </summary>
        private static readonly ITraceIdMaker DefaultMarker = new DefaultTraceIdMaker();

        /// <summary>
        /// 标识
        /// </summary>
        private readonly string _id;

        /// <summary>
        /// 初始化一个<see cref="TraceIdAccessor"/>类型的实例
        /// </summary>
        /// <param name="maker">Trace Id 生成器</param>
        public TraceIdAccessor(ITraceIdMaker maker) => _id = maker is null ? DefaultMarker.Create() : maker.Create();

        /// <summary>
        /// 获取一个 Trace Id
        /// </summary>
        public string GetTraceId() => _id;
    }
}
