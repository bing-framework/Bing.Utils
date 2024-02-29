namespace Bing.IdUtils.GuidImplements;

/// <summary>
/// 基于 Equifax 风格实现的 GUID 提供程序
/// </summary>
internal static class EquifaxStyleProvider
{
    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid Create() =>
        Create(DateTime.UtcNow);

    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="mode">不重复模式</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid Create(NoRepeatMode mode) =>
        Create(mode == NoRepeatMode.On ? NoRepeatTimeStampManager.GetFactory().GetUtcTimeStamp() : DateTime.UtcNow);

    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="secureTimestamp">安全时间戳</param>
    public static Guid Create(DateTime secureTimestamp) =>
        new($"00000000-0000-0000-0000-00"
            + $"{secureTimestamp.Month:00}"
            + $"{secureTimestamp.Day:00}"
            + $"{secureTimestamp.Year % 100:00}"
            + $"{secureTimestamp.Hour:00}"
            + $"{secureTimestamp.Minute:00}");
}