using BenchmarkDotNet.Attributes;
using Bing.Security.Gm;
using Bing.Security.Randomness;

namespace Bing.Utils.Benchmark.Benchmarks;

/// <summary>
/// 测量 SM2 常用消息负载下的加密、解密、签名和验签开销。
/// </summary>
[MemoryDiagnoser]
public class Sm2Benchmarks
{
    /// <summary>
    /// 非对称密码操作覆盖的典型小消息长度，单位为字节。
    /// </summary>
    [Params(32, 256, 4 * 1024)]
    public int PayloadSize { get; set; }

    /// <summary>
    /// 用于 SM2 操作的随机消息。
    /// </summary>
    private byte[] _payload = null!;

    /// <summary>
    /// 基准期间复用的 SM2 PEM 密钥对。
    /// </summary>
    private Sm2KeyPair _keyPair = null!;

    /// <summary>
    /// 预先生成的 C1C3C2 密文，避免解密基准包含加密成本。
    /// </summary>
    private byte[] _ciphertext = null!;

    /// <summary>
    /// 预先生成的 DER 签名，避免验签基准包含签名成本。
    /// </summary>
    private byte[] _signature = null!;

    /// <summary>
    /// 初始化每种负载长度对应的消息、密钥和预计算输入。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = SecurityRandom.GetBytes(PayloadSize);
        _keyPair = Sm2KeyGenerator.Generate();
        _ciphertext = Sm2Encryption.Encrypt(_payload, _keyPair.PublicKeyPem);
        _signature = Sm2Signature.Sign(_payload, _keyPair.PrivateKeyPem);
    }

    /// <summary>
    /// 清除基准期间保留的二进制敏感数据。
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        Array.Clear(_payload, 0, _payload.Length);
        Array.Clear(_ciphertext, 0, _ciphertext.Length);
        Array.Clear(_signature, 0, _signature.Length);
    }

    /// <summary>
    /// 测量 SM2 C1C3C2 加密。
    /// </summary>
    /// <returns>新生成的 C1C3C2 密文。</returns>
    [Benchmark]
    public byte[] Encrypt() => Sm2Encryption.Encrypt(_payload, _keyPair.PublicKeyPem);

    /// <summary>
    /// 测量 SM2 C1C3C2 解密和认证验证。
    /// </summary>
    /// <returns>认证成功后的明文。</returns>
    [Benchmark]
    public byte[] Decrypt() => Sm2Encryption.Decrypt(_ciphertext, _keyPair.PrivateKeyPem);

    /// <summary>
    /// 测量 SM2withSM3 DER 签名。
    /// </summary>
    /// <returns>新生成的 DER 签名。</returns>
    [Benchmark]
    public byte[] Sign() => Sm2Signature.Sign(_payload, _keyPair.PrivateKeyPem);

    /// <summary>
    /// 测量 SM2withSM3 DER 签名验证。
    /// </summary>
    /// <returns>签名有效时返回 <c>true</c>。</returns>
    [Benchmark]
    public bool Verify() => Sm2Signature.Verify(_payload, _signature, _keyPair.PublicKeyPem);
}