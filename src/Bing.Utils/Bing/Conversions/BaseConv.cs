using Bing.Text.Internals;

namespace Bing.Conversions;

/// <summary>
/// BASE 转换工具
/// </summary>
public static class BaseConv
{
    /// <summary>
    /// 供字节编码和解码复用的默认 <see cref="Base32"/> 实例。
    /// </summary>
    private static readonly Base32 _defaultBlankBase32 = new();

    /// <summary>
    /// 供字节编码和解码复用的默认 <see cref="ZBase32"/> 实例。
    /// </summary>
    private static readonly ZBase32 _defaultBlankZBase32 = new();

    /// <summary>
    /// 供字节编码和解码复用的默认 <see cref="Base64"/> 实例。
    /// </summary>
    private static readonly Base64 _defaultBlankBase64 = new();

    /// <summary>
    /// 供字节编码和解码复用的默认 <see cref="Base91"/> 实例。
    /// </summary>
    private static readonly Base91 _defaultBlankBase91 = new();

    /// <summary>
    /// 供字节编码和解码复用的默认 <see cref="Base256"/> 实例。
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
    /// <exception cref="FormatException">非空输入的字符、长度、填充或末尾未使用位不符合 Base32 格式。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] FromBase32(string data) => _defaultBlankBase32.Decode(data);

    /// <summary>
    /// 将字符串转换为 <see cref="Base32"/> 编码的字符串。
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
    /// <param name="data">要解码的 <see cref="Base32"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    /// <exception cref="FormatException">移除换行后的编码不符合 Base32 格式。</exception>
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
    /// 将字符串转换为 <see cref="ZBase32"/> 编码的字符串。
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
    /// <param name="data">要解码的 <see cref="ZBase32"/> 编码字符串。</param>
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
    /// 将字符串转换为 <see cref="Base64"/> 编码的字符串。
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
    /// <param name="data">要解码的 <see cref="Base64"/> 编码字符串。</param>
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
    /// 将字符串编码为 Base64 URL 安全文本。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的Base64 URL安全字符串。</returns>
    /// <remarks>
    /// 将标准 Base64 中的加号和斜杠替换为连字符和下划线，并移除末尾填充等号。
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
    /// 还原标准 Base64 字符并补齐填充，再按指定编码解码；默认使用 UTF-8。
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
    /// 将字符串转换为 <see cref="Base91"/> 编码的字符串。
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
    /// <param name="data">要解码的 <see cref="Base91"/> 编码字符串。</param>
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
    /// 将字符串转换为 <see cref="Base256"/> 编码的字符串。
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
    /// <param name="data">要解码的 <see cref="Base256"/> 编码字符串。</param>
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
    /// 将数据编码为 Base32 字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase32String(this byte[] data) => BaseConv.ToBase32(data);

    /// <summary>
    /// 将数据编码为 Base32 字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase32String(this string data, Encoding encoding = null) => BaseConv.ToBase32String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base32"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的 <see cref="Base32"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase32String(this string data, Encoding encoding = null) => BaseConv.FromBase32String(data, encoding);

    #endregion

    #region ZBase32

    /// <summary>
    /// 将数据编码为 ZBase32 字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="ZBase32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToZBase32String(this byte[] data) => BaseConv.ToZBase32(data);

    /// <summary>
    /// 将数据编码为 ZBase32 字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="ZBase32"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToZBase32String(this string data, Encoding encoding = null) => BaseConv.ToZBase32String(data, encoding);

    /// <summary>
    /// 将 <see cref="ZBase32"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的 <see cref="ZBase32"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromZBase32String(this string data, Encoding encoding = null) => BaseConv.FromZBase32String(data, encoding);

    #endregion

    #region Base64

    /// <summary>
    /// 将数据编码为 Base64 字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base64"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase64String(this byte[] data) => BaseConv.ToBase64(data);

    /// <summary>
    /// 将数据编码为 Base64 字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base64"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase64String(this string data, Encoding encoding = null) => BaseConv.ToBase64String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base64"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的 <see cref="Base64"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase64String(this string data, Encoding encoding = null) => BaseConv.FromBase64String(data, encoding);

    #endregion

    #region Base64Url

    /// <summary>
    /// 将字符串编码为 Base64 URL 安全文本。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的Base64 URL安全字符串。</returns>
    /// <remarks>
    /// 将标准 Base64 中的加号和斜杠替换为连字符和下划线，并移除末尾填充等号。
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
    /// 还原标准 Base64 字符并补齐填充，再按指定编码解码；默认使用 UTF-8。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase64UrlString(this string data, Encoding encoding = null) => BaseConv.FromBase64UrlString(data, encoding);

    #endregion

    #region Base91

    /// <summary>
    /// 将数据编码为 Base91 字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base91"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase91String(this byte[] data) => BaseConv.ToBase91(data);

    /// <summary>
    /// 将数据编码为 Base91 字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base91"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase91String(this string data, Encoding encoding = null) => BaseConv.ToBase91String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base91"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的 <see cref="Base91"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase91String(this string data, Encoding encoding = null) => BaseConv.FromBase91String(data, encoding);

    #endregion

    #region Base256

    /// <summary>
    /// 将数据编码为 Base256 字符串。
    /// </summary>
    /// <param name="data">要转换的字节数组。</param>
    /// <returns>转换后的 <see cref="Base256"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase256String(this byte[] data) => BaseConv.ToBase256(data);

    /// <summary>
    /// 将数据编码为 Base256 字符串。
    /// </summary>
    /// <param name="data">要转换的原始字符串。</param>
    /// <param name="encoding">用于字符串编码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>转换后的 <see cref="Base256"/> 编码字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastToBase256String(this string data, Encoding encoding = null) => BaseConv.ToBase256String(data, encoding);

    /// <summary>
    /// 将 <see cref="Base256"/> 编码的字符串转换为原始字符串。
    /// </summary>
    /// <param name="data">要解码的 <see cref="Base256"/> 编码字符串。</param>
    /// <param name="encoding">用于字符串解码的编码方式（如果为 null，则使用默认编码）。</param>
    /// <returns>解码后的原始字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CastFromBase256String(this string data, Encoding encoding = null) => BaseConv.FromBase256String(data, encoding);

    #endregion
}
