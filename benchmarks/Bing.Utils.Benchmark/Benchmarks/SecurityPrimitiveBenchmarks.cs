using BenchmarkDotNet.Attributes;
using Bing.Security.Authentication;
using Bing.Security.Cryptography;
using Bing.Security.Encoding;
using Bing.Security.Gm;
using Bing.Security.Hashing;
using Bing.Security.Randomness;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 测量安全基础原语在不同内存负载下的吞吐和分配。
/// </summary>
[MemoryDiagnoser]
public class SecurityPrimitiveBenchmarks
{
    /// <summary>
    /// 本轮基准覆盖的输入长度，单位为字节。
    /// </summary>
    [Params(32, 256, 4 * 1024, 64 * 1024, 1024 * 1024)]
    public int PayloadSize { get; set; }

    /// <summary>
    /// 参与编码、摘要和加密的固定输入数据。
    /// </summary>
    private byte[] _payload = null!;

    /// <summary>
    /// AES-GCM 认证加密使用的 256 位密钥。
    /// </summary>
    private byte[] _aesKey = null!;

    /// <summary>
    /// HMAC-SHA256 使用的认证密钥。
    /// </summary>
    private byte[] _hmacKey = null!;

    /// <summary>
    /// SM4-GCM 使用的 128 位密钥。
    /// </summary>
    private byte[] _sm4Key = null!;

    /// <summary>
    /// 用于随机填充基准的预分配目标缓冲区。
    /// </summary>
    private byte[] _randomDestination = null!;

    /// <summary>
    /// 用于 Base64Url 解码基准的预编码文本。
    /// </summary>
    private string _base64Url = null!;

    /// <summary>
    /// 初始化每种负载长度对应的测试数据和密钥。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        _aesKey = SecurityRandom.GetBytes(32);
        _hmacKey = SecurityRandom.GetBytes(32);
        _sm4Key = SecurityRandom.GetBytes(Sm4GcmEncryption.KeySize);
        _randomDestination = new byte[PayloadSize];
        SecurityRandom.Fill(_payload);
        _base64Url = Base64UrlEncoding.Encode(_payload);
    }

    /// <summary>
    /// 清除基准阶段保留的敏感密钥和输入数据。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Array.Clear(_payload, 0, _payload.Length);
        Array.Clear(_aesKey, 0, _aesKey.Length);
        Array.Clear(_hmacKey, 0, _hmacKey.Length);
        Array.Clear(_sm4Key, 0, _sm4Key.Length);
        Array.Clear(_randomDestination, 0, _randomDestination.Length);
    }

    /// <summary>
    /// 测量密码学安全随机数填充。
    /// </summary>
    [Benchmark]
    public void SecurityRandomFill() => SecurityRandom.Fill(_randomDestination);

    /// <summary>
    /// 测量小写十六进制编码。
    /// </summary>
    /// <returns>编码后的十六进制文本。</returns>
    [Benchmark]
    public string HexEncode() => HexEncoding.Encode(_payload);

    /// <summary>
    /// 测量无填充 Base64Url 编码。
    /// </summary>
    /// <returns>编码后的 Base64Url 文本。</returns>
    [Benchmark]
    public string Base64UrlEncode() => Base64UrlEncoding.Encode(_payload);

    /// <summary>
    /// 测量无填充 Base64Url 解码。
    /// </summary>
    /// <returns>解码后的字节数组。</returns>
    [Benchmark]
    public byte[] Base64UrlDecode() => Base64UrlEncoding.Decode(_base64Url);

    /// <summary>
    /// 测量 SHA-256 摘要计算。
    /// </summary>
    /// <returns>SHA-256 摘要字节。</returns>
    [Benchmark]
    public byte[] Sha256() => Hashing.Compute(_payload);

    /// <summary>
    /// 测量 HMAC-SHA256 认证码计算。
    /// </summary>
    /// <returns>HMAC-SHA256 认证码字节。</returns>
    [Benchmark]
    public byte[] HmacSha256() => Hmac.Compute(_payload, _hmacKey);

    /// <summary>
    /// 测量 AES-256-GCM 认证加密。
    /// </summary>
    /// <returns>包含 Nonce、密文和认证标签的载荷。</returns>
    [Benchmark]
    public AesGcmPayload AesGcmEncrypt() => AesGcmEncryption.Encrypt(_payload, _aesKey);

    /// <summary>
    /// 测量 SM3 摘要计算。
    /// </summary>
    /// <returns>SM3 摘要字节。</returns>
    [Benchmark]
    public byte[] Sm3() => Bing.Security.Gm.Sm3.Compute(_payload);

    /// <summary>
    /// 测量 SM4-GCM 认证加密。
    /// </summary>
    /// <returns>包含 Nonce、密文和认证标签的载荷。</returns>
    [Benchmark]
    public Sm4GcmPayload Sm4GcmEncrypt() => Sm4GcmEncryption.Encrypt(_payload, _sm4Key);
}