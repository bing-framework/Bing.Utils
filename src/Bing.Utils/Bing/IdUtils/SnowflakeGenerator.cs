namespace Bing.IdUtils;

/// <summary>
/// 雪花算法Id 生成器
/// </summary>
public static class SnowflakeGenerator
{
    /// <summary>
    /// 生成
    /// </summary>
    /// <param name="workerId">机器ID</param>
    /// <param name="dataCenterId">数据标识ID</param>
    /// <param name="sequence">序列号标识</param>
    public static SnowflakeIdWorker Create(long workerId, long dataCenterId, long sequence = 0L) => new(workerId, dataCenterId, sequence);
}