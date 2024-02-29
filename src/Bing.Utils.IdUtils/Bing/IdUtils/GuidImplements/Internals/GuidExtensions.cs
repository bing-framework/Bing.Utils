namespace Bing.IdUtils.GuidImplements.Internals;

/// <summary>
/// Guid 扩展
/// </summary>
internal static class GuidExtensions
{
    /// <summary>
    /// 转换为大端字节数组
    /// </summary>
    /// <param name="guid">Guid</param>
    public static byte[] ToBigEndianByteArray(this in Guid guid)
    {
        var result = guid.ToByteArray();
        GuidUtility.EndianSwap(result);
        return result;
    }
}