using Bing.IdUtils.SnowflakeImplements.Providers;

namespace Bing.IdUtils;

/// <summary>
/// 雪花算法Id 生成器
/// </summary>
public static class SnowflakeGenerator
{
    /// <summary>
    /// 生成雪花ID
    /// </summary>
    /// <remarks>基于 Twitter 算法的雪花ID</remarks>
    /// <param name="workerId">机器ID，取值范围: 0 ~ 31</param>
    /// <param name="dataCenterId">数据中心ID，取值范围: 0 ~ 31</param>
    /// <param name="sequence">毫秒内序列，取值范围: 0 ~ 4095</param>
    public static ISnowflakeId Create(long workerId, long dataCenterId, long sequence = 0L) => new TwitterSnowflakeProvider(workerId, dataCenterId, sequence);

    /// <summary>
    /// 生成雪花ID
    /// </summary>
    /// <remarks>基于 Seata 算法的雪花ID</remarks>
    /// <param name="workerId">机器ID，取值范围: 0 ~ 1023</param>
    public static ISnowflakeId Create(long workerId) => new SeataSnowflakeProvider(workerId);
}