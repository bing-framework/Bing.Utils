using System;
using System.Runtime.CompilerServices;

namespace Bing.IdUtils.GuidImplements.Internals;

/// <summary>
/// GUID 工具
/// </summary>
internal static class GuidUtility
{
    /// <summary>
    /// 复制
    /// </summary>
    /// <param name="guid">GUID</param>
    public static byte[] Copy(byte[] guid)
    {
        var result = new byte[16];
        Array.Copy(guid, result, 16);
        return result;
    }

    /// <summary>
    /// 复制并交换字节。大字节转小字节
    /// </summary>
    /// <param name="guid">GUID</param>
    public static byte[] CopyWithEndianSwap(byte[] guid)
    {
        var result = new byte[16];
        result[0] = guid[3];
        result[1] = guid[2];
        result[2] = guid[1];
        result[3] = guid[0];
        result[4] = guid[5];
        result[5] = guid[4];
        result[6] = guid[7];
        result[7] = guid[6];
        Array.Copy(guid, 8, result, 8, 8);
        return result;
    }

    /// <summary>
    /// 字节交换。大字节转小字节
    /// </summary>
    /// <param name="guid">GUID</param>
    public static void EndianSwap(byte[] guid)
    {
        Swap(guid, 0, 3);
        Swap(guid, 1, 2);
        Swap(guid, 4, 5);
        Swap(guid, 6, 7);
    }

    /// <summary>
    /// 交换
    /// </summary>
    /// <param name="array">数组</param>
    /// <param name="index1">交换索引</param>
    /// <param name="index2">交换索引</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap(byte[] array, int index1, int index2) => (array[index1], array[index2]) = (array[index2], array[index1]);
}