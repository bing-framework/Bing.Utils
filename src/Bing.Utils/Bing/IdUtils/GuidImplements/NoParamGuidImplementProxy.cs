namespace Bing.IdUtils.GuidImplements;

/// <summary>
/// 无参GUID 实现的代理
/// </summary>
internal static class NoParamGuidImplementProxy
{
    /// <summary>
    /// 基础风格的 GUID
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid Basic() => GuidProvider.Create(GuidStyle.BasicStyle);

    /// <summary>
    /// 字符串序列风格的 GUID
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid SequentialAsString() => GuidProvider.Create(GuidStyle.SequentialAsStringStyle);

    /// <summary>
    /// 二进制序列风格的 GUID
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid SequentialAsBinary() => GuidProvider.Create(GuidStyle.SequentialAsBinaryStyle);

    /// <summary>
    /// 顺序风格的 GUID
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid SequentialAsEnd() => GuidProvider.Create(GuidStyle.SequentialAsEndStyle);

    /// <summary>
    /// Equifax 风格的 GUID
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid Equifax() => GuidProvider.Create(GuidStyle.EquifaxStyle);
}