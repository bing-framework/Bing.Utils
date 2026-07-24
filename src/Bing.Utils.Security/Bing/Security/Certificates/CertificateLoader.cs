#if NET6_0_OR_GREATER
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bing.Security.Certificates;

/// <summary>
/// 提供 DER、CER、PEM、PFX 和 P12 X.509 证书加载操作。
/// </summary>
public static class CertificateLoader
{
    /// <summary>
    /// 从文件加载 X.509 证书，不将私钥持久化到系统密钥存储。
    /// </summary>
    /// <param name="path">证书文件路径。</param>
    /// <param name="password">PFX 或 P12 私钥密码；不会包含在异常消息中。</param>
    /// <returns>调用方负责释放的证书实例。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> 为空或证书格式无效时抛出。</exception>
    public static X509Certificate2 Load(string path, string password = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("证书文件路径不能为空。", nameof(path));

        try
        {
            return Load(File.ReadAllBytes(path), password);
        }
        catch (CryptographicException exception)
        {
            throw new ArgumentException("无法加载 X.509 证书。", nameof(path), exception);
        }
    }

    /// <summary>
    /// 从 DER、CER、PEM、PFX 或 P12 数据加载 X.509 证书，不将私钥持久化到系统密钥存储。
    /// </summary>
    /// <param name="data">证书原始数据。</param>
    /// <param name="password">PFX 或 P12 私钥密码；不会包含在异常消息中。</param>
    /// <returns>调用方负责释放的证书实例。</returns>
    /// <exception cref="ArgumentException"><paramref name="data"/> 为空或证书格式无效时抛出。</exception>
    public static X509Certificate2 Load(ReadOnlySpan<byte> data, string password = null)
    {
        if (data.IsEmpty)
            throw new ArgumentException("证书数据不能为空。", nameof(data));

        var certificateData = data.ToArray();
        try
        {
            if (IsPem(certificateData))
                return X509Certificate2.CreateFromPem(System.Text.Encoding.ASCII.GetString(certificateData));

            return new X509Certificate2(certificateData, password, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (CryptographicException exception)
        {
            throw new ArgumentException("证书数据无效、密码错误或格式不受支持。", nameof(data), exception);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(certificateData);
        }
    }

    /// <summary>
    /// 判断数据是否包含 PEM 证书标签。
    /// </summary>
    /// <param name="data">证书数据。</param>
    /// <returns>数据包含 PEM 证书标签时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    private static bool IsPem(byte[] data)
    {
        var text = System.Text.Encoding.ASCII.GetString(data);
        return text.IndexOf("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal) >= 0 &&
               text.IndexOf("-----END CERTIFICATE-----", StringComparison.Ordinal) >= 0;
    }
}
#endif