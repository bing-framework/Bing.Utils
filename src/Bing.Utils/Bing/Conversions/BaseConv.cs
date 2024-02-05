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

    #region Base64Url

    /// <summary>
    /// 将字符串转换为 <see cref="Base64"/> URL安全格式。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的Base64 URL安全字符串。</returns>
    /// <remarks>
    /// 此方法首先将字符串数据编码为字节数组，然后将该字节数组转换为标准的Base64字符串。
    /// 转换过程中，将Base64字符串中的某些字符替换为URL安全的字符：
    /// - '+' 替换为 '-'
    /// - '/' 替换为 '_'
    /// 此外，从结果字符串中移除了所有的'='填充字符，以使字符串更适合URL使用。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToBase64UrlString(string data, Encoding encoding = null)
    {
        var base64 = new Base64(encoding: encoding);
        return base64.EncodeString(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// 将 <see cref="Base64"/> URL安全格式的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要转换的 <see cref="Base64"/> URL安全格式字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    /// <remarks>
    /// 此方法首先将Base64 URL安全格式字符串中的URL安全字符：
    /// - '-' 替换为 '+'
    /// - '_' 替换为 '/'
    /// 然后，根据需要添加'='填充字符，以确保字符串的长度是4的倍数，满足标准Base64编码的要求。
    /// 之后，使用指定的编码（或默认的UTF-8编码）将经过处理的Base64字符串解码回原始字符串数据。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FromBase64UrlString(string data, Encoding encoding = null)
    {
        data = data.Replace('-', '+').Replace('_', '/');
        data = data.PadRight(data.Length + (4 - data.Length % 4) % 4, '=');
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

/// <summary>
/// BASE 转换工具(<see cref="BaseConv"/>) 扩展
/// </summary>
public static class BaseConvExtensions
{
    #region Base32

    /// <summary>
    /// 将字节数组转换为 <see cref="Base32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase32String(this byte[] data) => BaseConv.ToBase32(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase32String(this string data, Encoding encoding = null) => BaseConv.ToBase32String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base32"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base32"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase32String(this string data, Encoding encoding = null) => BaseConv.FromBase32String(data, encoding);

    #endregion

    #region ZBase32

    /// <summary>
    /// 将字节数组转换为 <see cref="ZBase32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="ZBase32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToZBase32String(this byte[] data) => BaseConv.ToZBase32(data);

    /// <summary>
    /// 将字节串转换为 <see cref="ZBase32"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="ZBase32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToZBase32String(this string data, Encoding encoding = null) => BaseConv.ToZBase32String(data, encoding);

    /// <summary>
    /// 将 <see cref="ZBase32"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="ZBase32"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromZBase32String(this string data, Encoding encoding = null) => BaseConv.FromZBase32String(data, encoding);

    #endregion

    #region Base64

    /// <summary>
    /// 将字节数组转换为 <see cref="Base64"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base64"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase64String(this byte[] data) => BaseConv.ToBase64(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base64"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base64"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase64String(this string data, Encoding encoding = null) => BaseConv.ToBase64String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base64"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base64"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase64String(this string data, Encoding encoding = null) => BaseConv.FromBase64String(data, encoding);

    #endregion

    #region Base64Url

    /// <summary>
    /// 将字符串转换为 <see cref="Base64"/> URL安全格式。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的Base64 URL安全字符串。</returns>
    /// <remarks>
    /// 此方法首先将字符串数据编码为字节数组，然后将该字节数组转换为标准的Base64字符串。
    /// 转换过程中，将Base64字符串中的某些字符替换为URL安全的字符：
    /// - '+' 替换为 '-'
    /// - '/' 替换为 '_'
    /// 此外，从结果字符串中移除了所有的'='填充字符，以使字符串更适合URL使用。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase64UrlString(this string data, Encoding encoding = null) => BaseConv.ToBase64UrlString(data, encoding);

    /// <summary>
    /// 将 <see cref="Base64"/> URL安全格式的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要转换的 <see cref="Base64"/> URL安全格式字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    /// <remarks>
    /// 此方法首先将Base64 URL安全格式字符串中的URL安全字符：
    /// - '-' 替换为 '+'
    /// - '_' 替换为 '/'
    /// 然后，根据需要添加'='填充字符，以确保字符串的长度是4的倍数，满足标准Base64编码的要求。
    /// 之后，使用指定的编码（或默认的UTF-8编码）将经过处理的Base64字符串解码回原始字符串数据。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase64UrlString(this string data, Encoding encoding = null) => BaseConv.FromBase64UrlString(data, encoding);

    #endregion

    #region Base91

    /// <summary>
    /// 将字节数组转换为 <see cref="Base91"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base91"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase91String(this byte[] data) => BaseConv.ToBase91(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base91"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base91"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase91String(this string data, Encoding encoding = null) => BaseConv.ToBase91String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base91"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base91"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase91String(this string data, Encoding encoding = null) => BaseConv.FromBase91String(data, encoding);

    #endregion

    #region Base256

    /// <summary>
    /// 将字节数组转换为 <see cref="Base256"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base256"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase256String(this byte[] data) => BaseConv.ToBase256(data);

    /// <summary>
    /// 将字节串转换为 <see cref="Base256"/> 编码的字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base256"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase256String(this string data, Encoding encoding = null) => BaseConv.ToBase256String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base256"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的  <see cref="Base256"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase256String(this string data, Encoding encoding = null) => BaseConv.FromBase256String(data, encoding);

    #endregion
}