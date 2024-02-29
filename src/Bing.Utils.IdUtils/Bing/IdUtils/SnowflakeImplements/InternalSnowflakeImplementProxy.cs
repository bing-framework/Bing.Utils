using Bing.IdUtils.SnowflakeImplements.Providers;

namespace Bing.IdUtils.SnowflakeImplements;

/// <summary>
/// 内部基于雪花ID实现的代理
/// </summary>
internal static class InternalSnowflakeImplementProxy
{
    /// <summary>
    /// 基于 Twitter 算法
    /// </summary>
    /// <remarks>默认机器ID：1</remarks>
    public static ISnowflakeProvider Twitter = new TwitterSnowflakeProvider(1,1);

    /// <summary>
    /// 基于 Seata 算法
    /// </summary>
    /// <remarks>默认机器ID：1</remarks>
    public static ISnowflakeProvider Seata = new SeataSnowflakeProvider(1);
}