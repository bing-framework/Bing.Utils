namespace Bing.IdUtils.CombImplements.Providers;

/// <summary>
/// 基于 COMB 算法的 GUID 提供程序
/// </summary>
/// <remarks>
/// COMB 型可以理解为一种改进的 GUID 。由于 GUID 数据毫无规律可言造成索引效率低下，影响了系统的性能，因此 COMB 将 GUID 和系统时间进行结合，以使索引和检索上有更优的性能。
/// </remarks>
internal interface ICombProvider
{
    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个随机的 <see cref="Guid"/> 和当前的 UTC 时间戳组成。
    /// </summary>
    Guid Create();

    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个指定的 <see cref="Guid"/> 和当前的 UTC 时间戳组成。
    /// </summary>
    /// <param name="value">值</param>
    Guid Create(Guid value);

    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个随机的 <see cref="Guid"/> 和提供的时间戳组成。
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    Guid Create(DateTime timestamp);

    /// <summary>
    /// 创建一个 COMB 型的 <see cref="Guid"/>。由一个指定的 <see cref="Guid"/> 和提供的时间戳组成。
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="timestamp">时间戳</param>
    Guid Create(Guid value, DateTime timestamp);

    /// <summary>
    /// 从提供的 COMB GUID 中获取时间戳。
    /// </summary>
    /// <param name="value">COMB GUID</param>
    DateTime GetTimeStamp(Guid value);
}