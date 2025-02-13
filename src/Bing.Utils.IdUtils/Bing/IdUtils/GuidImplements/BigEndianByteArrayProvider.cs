using Bing.IdUtils.GuidImplements.Internals;

namespace Bing.IdUtils.GuidImplements;

/// <summary>
/// 基于大端字节数组实现的 GUID 提供程序
/// </summary>
internal static class BigEndianByteArrayProvider
{
    /// <summary>
    /// 创建一个 <see cref="Guid"/>
    /// </summary>
    /// <param name="bytes">字节数组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid Create(byte[] bytes) => new(GuidUtility.CopyWithEndianSwap(bytes));
}