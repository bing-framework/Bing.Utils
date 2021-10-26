using System;

namespace Bing.IdGenerators
{
    /// <summary>
    /// 默认的 Trace Id 生成器
    /// </summary>
    public class DefaultTraceIdMaker : ITraceIdMaker
    {
        /// <summary>
        /// 生成 Id
        /// </summary>
        public string Create()
        {
            var now = DateTime.Now;
            return $"{now:yyyyMMddHHmmddffff}{RandomIdGenerator.Create(7)}{now.Ticks}";
        }
    }
}
