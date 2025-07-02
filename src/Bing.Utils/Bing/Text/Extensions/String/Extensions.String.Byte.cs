
// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 将字符串转换为 UTF-8 编码的字节数组
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <returns>
    /// UTF-8 编码的字节数组。如果 <paramref name="value"/> 为 null，则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// "Hello".ToUtf8Bytes() => [72, 101, 108, 108, 111] // 对应 UTF-8 编码的字节
    /// </code>
    /// </example>
    public static byte[] ToUtf8Bytes(this string value) => value.ToBytes(Encoding.UTF8);

    /// <summary>
    /// 将字符串转换为 UTF-7 编码的字节数组
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <returns>
    /// UTF-7 编码的字节数组。如果 <paramref name="value"/> 为 null，则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// "Hello".ToUtf7Bytes() => 对应 UTF-7 编码的字节
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：UTF-7 编码在 .NET Core 3.0 及更高版本中已被标记为过时，不推荐在新代码中使用。
    /// </remarks>
    public static byte[] ToUtf7Bytes(this string value) => value.ToBytes(Encoding.UTF7);

    /// <summary>
    /// 将字符串转换为 UTF-32 编码的字节数组
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <returns>
    /// UTF-32 编码的字节数组。如果 <paramref name="value"/> 为 null，则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// "Hello".ToUtf32Bytes() => 对应 UTF-32 编码的字节（每个字符通常占用 4 个字节）
    /// </code>
    /// </example>
    public static byte[] ToUtf32Bytes(this string value) => value.ToBytes(Encoding.UTF32);

    /// <summary>
    /// 将字符串转换为 ASCII 编码的字节数组
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <returns>
    /// ASCII 编码的字节数组。如果 <paramref name="value"/> 为 null，则抛出 <see cref="ArgumentNullException"/> 异常。
    /// 注意：非 ASCII 字符将被转换为问号(?)。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// "Hello".ToASCIIBytes() => [72, 101, 108, 108, 111] // 对应 ASCII 编码的字节
    /// "你好".ToASCIIBytes() => [63, 63] // 非 ASCII 字符转换为问号(?)，对应 ASCII 码值 63
    /// </code>
    /// </example>
    // ReSharper disable once InconsistentNaming
    public static byte[] ToASCIIBytes(this string value) => value.ToBytes(Encoding.ASCII);

    /// <summary>
    /// 将字符串转换为 BigEndianUnicode (UTF-16BE) 编码的字节数组
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <returns>
    /// BigEndianUnicode 编码的字节数组。如果 <paramref name="value"/> 为 null，则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// "Hello".ToBigEndianUnicodeBytes() => 对应 UTF-16BE 编码的字节（每个字符通常占用 2 个字节，高字节在前）
    /// </code>
    /// </example>
    public static byte[] ToBigEndianUnicodeBytes(this string value) => value.ToBytes(Encoding.BigEndianUnicode);

    /// <summary>
    /// 将字符串转换为系统默认编码的字节数组
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <returns>
    /// 系统默认编码的字节数组。如果 <paramref name="value"/> 为 null，则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// "Hello".ToDefaultBytes() => 对应系统默认编码的字节
    /// </code>
    /// </example>
    /// <remarks>
    /// 注意：默认编码因操作系统和区域设置而异，不建议在需要跨平台兼容的应用程序中使用此方法。
    /// </remarks>
    public static byte[] ToDefaultBytes(this string value) => value.ToBytes(Encoding.Default);

    /// <summary>
    /// 将字符串转换为 Unicode (UTF-16LE) 编码的字节数组
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <returns>
    /// Unicode 编码的字节数组。如果 <paramref name="value"/> 为 null，则抛出 <see cref="ArgumentNullException"/> 异常。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// "Hello".ToUnicodeBytes() => 对应 UTF-16LE 编码的字节（每个字符通常占用 2 个字节，低字节在前）
    /// </code>
    /// </example>
    public static byte[] ToUnicodeBytes(this string value) => value.ToBytes(Encoding.Unicode);
}