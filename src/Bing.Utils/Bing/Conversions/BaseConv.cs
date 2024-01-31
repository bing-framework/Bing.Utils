using Bing.Text.Internals;

namespace Bing.Conversions;

/// <summary>
/// BASE 转换工具
/// </summary>
public static class BaseConv
{
    /// <summary>
    /// 默认的空白 <see cref="Base32"/> 实例。
    /// </summary>
    private static readonly Base32 _defaultBlankBase32 = new();

    /// <summary>
    /// 默认的空白 <see cref="ZBase32"/> 实例。
    /// </summary>
    private static readonly ZBase32 _defaultBlankZBase32 = new();

    /// <summary>
    /// 默认的空白 <see cref="Base64"/> 实例。
    /// </summary>
    private static readonly Base64 _defaultBlankBase64 = new();

    /// <summary>
    /// 默认的空白 <see cref="Base91"/> 实例。
    /// </summary>
    private static readonly Base91 _defaultBlankBase91 = new();

    /// <summary>
    /// 默认的空白 <see cref="Base256"/> 实例。
    /// </summary>
    private static readonly Base256 _defaultBlankBase256 = new();

    #region Base32

    /// <summary>
    /// 将字节数组转换为 <see cref="Base32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase32(byte[] data) => _defaultBlankBase32.Encode(data);

    /// <summary>
    /// 将 <see cref="Base32"/> 编码的字符串转换为字节数组。
    /// </summary>
    /// <param name="data">要转换的 <see cref="Base32"/> 编码字符串。</param>
    /// <returns>转换得到的字节数组。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] FromBase32(string data) => _defaultBlankBase32.Decode(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase32String(string data, Encoding encoding = null)
    {
        var base32 = new Base32(encoding: encoding);
        return base32.EncodeString(data);
    }

    /// <summary>
    /// 将 <see cref="Base32"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base32"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromBase32String(string data, Encoding encoding = null)
    {
        var base32 = new Base32(encoding: encoding);
        return base32.DecodeToString(data);
    }

    #endregion

    #region ZBase32

    /// <summary>
    /// 将字节数组转换为 <see cref="ZBase32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="ZBase32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToZBase32(byte[] data) => _defaultBlankZBase32.Encode(data);

    /// <summary>
    /// 将 <see cref="ZBase32"/> 编码的字符串转换为字节数组。
    /// </summary>
    /// <param name="data">要转换的 <see cref="ZBase32"/> 编码字符串。</param>
    /// <returns>转换得到的字节数组。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] FromZBase32(string data) => _defaultBlankZBase32.Decode(data);

    /// <summary>
    /// 将字节串转换为 <see cref="ZBase32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="ZBase32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToZBase32String(string data, Encoding encoding = null)
    {
        var base32 = new ZBase32(encoding: encoding);
        return base32.EncodeString(data);
    }

    /// <summary>
    /// 将 <see cref="ZBase32"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="ZBase32"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromZBase32String(string data, Encoding encoding = null)
    {
        var base32 = new ZBase32(encoding: encoding);
        return base32.DecodeToString(data);
    }

    #endregion
    
    #region Base64

    /// <summary>
    /// 将字节数组转换为 <see cref="Base64"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base64"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase64(byte[] data) => _defaultBlankBase64.Encode(data);

    /// <summary>
    /// 将 <see cref="Base64"/> 编码的字符串转换为字节数组。
    /// </summary>
    /// <param name="data">要转换的 <see cref="Base64"/> 编码字符串。</param>
    /// <returns>转换得到的字节数组。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] FromBase64(string data) => _defaultBlankBase64.Decode(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base64"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base64"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase64String(string data, Encoding encoding = null)
    {
        var base64 = new Base64(encoding: encoding);
        return base64.EncodeString(data);
    }

    /// <summary>
    /// 将 <see cref="Base64"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base64"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromBase64String(string data, Encoding encoding = null)
    {
        var base64 = new Base64(encoding: encoding);
        return base64.DecodeToString(data);
    }

    #endregion

    #region Base91

    /// <summary>
    /// 将字节数组转换为 <see cref="Base91"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base91"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase91(byte[] data) => _defaultBlankBase91.Encode(data);

    /// <summary>
    /// 将 <see cref="Base91"/> 编码的字符串转换为字节数组。
    /// </summary>
    /// <param name="data">要转换的 <see cref="Base91"/> 编码字符串。</param>
    /// <returns>转换得到的字节数组。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] FromBase91(string data) => _defaultBlankBase91.Decode(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base91"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base91"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase91String(string data, Encoding encoding = null)
    {
        var base91 = new Base91(encoding: encoding);
        return base91.EncodeString(data);
    }

    /// <summary>
    /// 将 <see cref="Base91"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base91"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromBase91String(string data, Encoding encoding = null)
    {
        var base91 = new Base91(encoding: encoding);
        return base91.DecodeToString(data);
    }

    #endregion

    #region Base256

    /// <summary>
    /// 将字节数组转换为 <see cref="Base256"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base256"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase256(byte[] data) => _defaultBlankBase256.Encode(data);

    /// <summary>
    /// 将 <see cref="Base256"/> 编码的字符串转换为字节数组。
    /// </summary>
    /// <param name="data">要转换的 <see cref="Base256"/> 编码字符串。</param>
    /// <returns>转换得到的字节数组。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] FromBase256(string data) => _defaultBlankBase256.Decode(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base256"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base256"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase256String(string data, Encoding encoding = null)
    {
        var base256 = new Base256(encoding: encoding);
        return base256.EncodeString(data);
    }

    /// <summary>
    /// 将 <see cref="Base256"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base256"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromBase256String(string data, Encoding encoding = null)
    {
        var base256 = new Base256(encoding: encoding);
        return base256.DecodeToString(data);
    }

    #endregion
}