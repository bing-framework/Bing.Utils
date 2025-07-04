// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 字节数组(<see cref="byte"/>[]) 扩展
/// </summary>
public static class ByteArrayExtensions
{
    #region To

    /// <summary>
    /// 将字节数组转换为32位整数
    /// </summary>
    /// <param name="bytes">要转换的字节数组</param>
    /// <param name="startIndex">起始索引位置，默认为0</param>
    /// <returns>转换后的32位整数</returns>
    /// <exception cref="ArgumentNullException">字节数组为null时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">起始索引无效时抛出</exception>
    public static int ToInt(this byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0)
            return 0;
        if (startIndex < 0 || startIndex >= bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "起始索引超出数组范围");
        // 计算可用字节数
        var availableBytes = bytes.Length - startIndex;
        if (availableBytes < 4)
            return 0;
        return BitConverter.ToInt32(bytes, startIndex);
    }

    /// <summary>
    /// 将字节数组转换为64位整数
    /// </summary>
    /// <param name="bytes">要转换的字节数组</param>
    /// <param name="startIndex">起始索引位置，默认为0</param>
    /// <returns>转换后的64位整数</returns>
    /// <exception cref="ArgumentNullException">字节数组为null时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">起始索引无效时抛出</exception>
    public static long ToLong(this byte[] bytes, int startIndex = 0)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0)
            return 0;
        if (startIndex < 0 || startIndex >= bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "起始索引超出数组范围");
        // 计算可用字节数
        var availableBytes = bytes.Length - startIndex;
        if (availableBytes < 8)
            return 0;
        return BitConverter.ToInt64(bytes, startIndex);
    }

    /// <summary>
    /// 将字节数组转换为16进制字符串表示形式
    /// </summary>
    /// <param name="value">要转换的字节数组</param>
    /// <returns>16进制字符串，每个字节由两个字符表示，字节间以空格分隔</returns>
    /// <exception cref="ArgumentNullException">当输入字节数组为 null 时抛出</exception>
    public static string ToHexString(this byte[] value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (value.Length == 0)
            return string.Empty;
        var sb = new StringBuilder(value.Length * 3 - 1);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0)
                sb.Append(' ');
            sb.Append(value[i].ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 将字节数组转换为Base64编码字符串
    /// </summary>
    /// <param name="value">要转换的字节数组</param>
    /// <returns>Base64编码的字符串</returns>
    /// <exception cref="ArgumentNullException">当输入字节数组为 null 时抛出</exception>
    public static string ToBase64String(this byte[] value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return Convert.ToBase64String(value);
    }

    /// <summary>
    /// 将字节数组转换为DateTime对象
    /// </summary>
    /// <param name="bytes">包含DateTime二进制表示的字节数组</param>
    /// <param name="startIndex">字节数组中的起始位置，默认为0</param>
    /// <returns>转换后的DateTime对象</returns>
    /// <remarks>
    /// 此方法使用 <see cref="BitConverter.ToInt64(byte[],int)"/> 将字节数组转换为长整数，
    /// 然后使用 <see cref="DateTime.FromBinary"/> 将长整数转换为DateTime。
    /// 字节数组应当至少有8个字节（从startIndex开始）。
    /// </remarks>
    /// <exception cref="ArgumentNullException">字节数组为null时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">startIndex无效时抛出</exception>
    /// <exception cref="ArgumentException">字节数组长度不足以从startIndex读取完整数据时抛出</exception>
    /// <example>
    /// <code>
    /// // 将DateTime转换为字节数组
    /// var date = new DateTime(2023, 1, 1);
    /// byte[] bytes = BitConverter.GetBytes(date.ToBinary());
    /// 
    /// // 将字节数组转换回DateTime
    /// var restoredDate = bytes.ToDateTime();
    /// Console.WriteLine(restoredDate); // 输出: 2023-01-01 00:00:00
    /// </code>
    /// </example>
    public static DateTime ToDateTime(this byte[] bytes, int startIndex = 0) =>
        DateTime.FromBinary(BitConverter.ToInt64(bytes, startIndex));

    #endregion

    #region Copy

    /// <summary>
    /// 创建二维字节数组的深拷贝
    /// </summary>
    /// <param name="bytes">要复制的二维字节数组</param>
    /// <returns>原二维数组的深拷贝</returns>
    /// <exception cref="ArgumentNullException">当输入数组为 null 时抛出</exception>
    public static byte[,] Copy(this byte[,] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        int width = bytes.GetLength(0), height = bytes.GetLength(1);
        var newBytes = new byte[width, height];
        Array.Copy(bytes, newBytes, bytes.Length);
        return newBytes;
    }

    #endregion
}