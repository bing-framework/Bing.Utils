using System;

namespace Bing.IdUtils;

/// <summary>
/// 默认的 Trace Id 生成器
/// </summary>
public class DefaultTraceIdMaker : ITraceIdMaker
{
    /// <summary>
    /// 创建 Id
    /// </summary>
    public string Create()
    {
        var now = DateTime.Now;
        return $"{now:yyyyMMddHHmmddffff}{RandomIdGenerator.Create(7)}{now.Ticks}";
    }
}