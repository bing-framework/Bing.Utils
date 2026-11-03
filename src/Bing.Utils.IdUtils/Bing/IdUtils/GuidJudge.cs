namespace Bing.IdUtils;

/// <summary>
/// <see cref="Guid"/> 检查器
/// </summary>
public static class GuidJudge
{
    /// <summary>
    /// 判断 <see cref="Guid"/> 是否为空、null 或 Guid.Empty
    /// </summary>
    /// <param name="guid">Guid</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty(Guid guid) => guid == Guid.Empty;

    /// <summary>
    /// 判断 <see cref="Guid"/> 是否为空、null 或 Guid.Empty
    /// </summary>
    /// <param name="guid">Guid</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty(Guid? guid) => guid is null || guid == Guid.Empty;

    /// <summary>
    /// 判断 字符串是否有效<see cref="Guid"/>
    /// </summary>
    /// <param name="guidStr">Guid字符串</param>
    public static bool IsValid(string guidStr) =>
        !string.IsNullOrWhiteSpace(guidStr) && Guid.TryParse(guidStr, out _);
}
