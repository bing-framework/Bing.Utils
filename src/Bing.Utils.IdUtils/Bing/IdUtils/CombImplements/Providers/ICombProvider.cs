namespace Bing.IdUtils.CombImplements.Providers;

/// <summary>
/// 提供 COMB <see cref="Guid"/> 的生成和时间提取功能。
/// </summary>
/// <remarks>
/// COMB 将时间信息编码到 GUID 中，可用于改善按该 GUID 建立索引时的数据局部性。
/// </remarks>
internal interface ICombProvider
{
    /// <summary>
    /// 使用随机 GUID 和当前时间创建 COMB GUID。
    /// </summary>
    /// <returns>新生成的 COMB GUID。</returns>
    Guid Create();

    /// <summary>
    /// 使用指定 GUID 和当前时间创建 COMB GUID。
    /// </summary>
    /// <param name="value">用作 COMB 基础数据的 GUID。</param>
    /// <returns>新生成的 COMB GUID。</returns>
    Guid Create(Guid value);

    /// <summary>
    /// 使用随机 GUID 和指定时间创建 COMB GUID。
    /// </summary>
    /// <param name="timestamp">写入 COMB GUID 的日期时间。</param>
    /// <returns>新生成的 COMB GUID。</returns>
    Guid Create(DateTime timestamp);

    /// <summary>
    /// 使用指定 GUID 和日期时间创建 COMB GUID。
    /// </summary>
    /// <param name="value">用作 COMB 基础数据的 GUID。</param>
    /// <param name="timestamp">写入 COMB GUID 的日期时间。</param>
    /// <returns>新生成的 COMB GUID。</returns>
    Guid Create(Guid value, DateTime timestamp);

    /// <summary>
    /// 从 COMB GUID 中提取编码的日期时间。
    /// </summary>
    /// <param name="value">包含编码日期时间的 COMB GUID。</param>
    /// <returns>从 GUID 中提取的日期时间。</returns>
    DateTime GetTimeStamp(Guid value);
}
